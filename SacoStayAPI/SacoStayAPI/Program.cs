using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SacoStayAPI.Data;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;
using SacoStayAPI.Service;
using SacoStayAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Amazon.S3;
using SacoStayAPI.Hubs;

namespace SacoStayAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Bí mật local / server (Neon, JWT, SMTP…) — không commit (xem appsettings.Local.json.example)
            builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")?.Trim();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Thiếu ConnectionStrings:DefaultConnection. " +
                    "Tạo file appsettings.Local.json (copy từ appsettings.Local.json.example), " +
                    "hoặc đặt biến môi trường ConnectionStrings__DefaultConnection trên hosting.");
            }

            // ================= 1. REGISTER SERVICES =================

            builder.Services.AddControllers();
            builder.Services.AddSignalR();
            builder.Services.AddMemoryCache();

            // ---- AWS S3 Configuration (Đã sửa lỗi nạp đè credentials) ----
            var awsOptions = builder.Configuration.GetAWSOptions();
            awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(
                builder.Configuration["AWS:AccessKey"],
                builder.Configuration["AWS:SecretKey"]
            );
            builder.Services.AddDefaultAWSOptions(awsOptions);
            builder.Services.AddAWSService<IAmazonS3>();

            // ---- Dependency Injection ----
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<IPhotoService, PhotoService>();
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();

            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();

            builder.Services.AddScoped<ILifestyleRepository, LifestyleRepository>();
            builder.Services.AddScoped<LifestyleService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
            builder.Services.AddScoped<IRoomPostService, RoomPostService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            // Swagger + Bearer 
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SacoStay API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Chỉ cần dán trực tiếp JWT Token của bạn vào ô dưới đây (Không cần gõ chữ Bearer)",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http, 
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Database (PostgreSQL)
            builder.Services.AddDbContext<ApplicationDBContext>(options =>
                options.UseNpgsql(connectionString));

            // Identity
            builder.Services.AddIdentity<Account, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<ApplicationDBContext>()
            .AddDefaultTokenProviders();

            // JWT Authentication
            var jwt = builder.Configuration.GetSection("Jwt");
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // Giữ claim "role" từ JWT (tránh map sang URI dài khiến [Authorize(Roles = "admin")] 403).
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwt["Issuer"],
                        ValidAudience = jwt["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"] ?? string.Empty)),
                        NameClaimType = JwtRegisteredClaimNames.Sub,
                        RoleClaimType = "role",
                        ClockSkew = TimeSpan.Zero
                    };

                    // Cấu hình cho SignalR nhận Token từ Query String
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // CORS — FE production + tùy chọn localhost khi dev
            var frontendBaseUrl = builder.Configuration["Frontend:BaseUrl"] ?? "https://sacostay.id.vn";
            var frontendSecondaryBaseUrl = builder.Configuration["Frontend:SecondaryBaseUrl"];
            var allowedOrigins = new List<string>
            {
                frontendBaseUrl.TrimEnd('/'),
                frontendSecondaryBaseUrl?.TrimEnd('/') ?? string.Empty
            };
            if (builder.Environment.IsDevelopment())
            {
                allowedOrigins.Add("http://localhost:4200");
                allowedOrigins.Add("https://localhost:4200");
            }

            var distinctOrigins = allowedOrigins
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(distinctOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // ================= 2. BUILD APP =================

            var app = builder.Build();

            // Forwarded headers để chạy tốt sau reverse proxy / cloud hosting
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                ForwardLimit = null
            });

            // Seed roles/users (chỉ khi DB kết nối được)
            try
            {
                using var scope = app.Services.CreateScope();
                await SeedData.InitializeAsync(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");
                logger.LogError(ex, "Seed data thất bại — kiểm tra ConnectionString và migration DB.");
                if (app.Environment.IsDevelopment())
                    throw;
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Sử dụng CORS trước Routing/Auth
            app.UseCors("AllowFrontend");

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Map Endpoints
            app.MapHub<ChatHub>("/chatHub");
            app.MapControllers();

            app.Run();
        }
    }
}

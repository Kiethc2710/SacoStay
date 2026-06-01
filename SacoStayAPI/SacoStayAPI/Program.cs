using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SacoStayAPI.Data;
using SacoStayAPI.Hubs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;
using SacoStayAPI.Service;
using SacoStayAPI.Services;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Amazon.S3;

namespace SacoStayAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ================= 1. REGISTER SERVICES =================

            builder.Services.AddControllers();
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();
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

            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();

            builder.Services.AddScoped<ILifestyleRepository, LifestyleRepository>();
            builder.Services.AddScoped<LifestyleService>();
             
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
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
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
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"])),
                        NameClaimType = JwtRegisteredClaimNames.Sub,
                        RoleClaimType = "role",
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };

                    // SignalR: token qua query access_token (chuẩn) hoặc header Authorization (fallback)
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var path = context.Request.Path;
                            if (!path.HasValue ||
                                path.Value?.IndexOf("/chathub", StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                return Task.CompletedTask;
                            }

                            var accessToken = context.Request.Query["access_token"].ToString();
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                context.Token = accessToken;
                                return Task.CompletedTask;
                            }

                            var authHeader = context.Request.Headers.Authorization.ToString();
                            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Token = authHeader["Bearer ".Length..].Trim();
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // CORS cho Angular
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyHeader()
                          .AllowAnyMethod()
                          .SetIsOriginAllowed(_ => true)
                          .AllowCredentials();
                });
            });

            // ================= 2. BUILD APP =================

            var app = builder.Build();

            // Seed data
            using (var scope = app.Services.CreateScope())
            {
                await SeedData.InitializeAsync(scope.ServiceProvider);
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Sử dụng CORS trước Routing/Auth
            app.UseCors("AllowAll");

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
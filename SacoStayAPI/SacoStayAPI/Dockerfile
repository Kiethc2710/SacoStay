# 1. Giai đoạn Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file project và restore các package
COPY ["SacoStayAPI.csproj", "./"]
RUN dotnet restore "SacoStayAPI.csproj"

# Copy toàn bộ code và build bản Release
COPY . .
RUN dotnet build "SacoStayAPI.csproj" -c Release -o /app/build

# 2. Giai đoạn Publish
FROM build AS publish
RUN dotnet publish "SacoStayAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Giai đoạn Chạy (Final)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render cấp cổng động, thường là 8080 hoặc 10000. Ta ép app chạy ở 8080.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SacoStayAPI.dll"]
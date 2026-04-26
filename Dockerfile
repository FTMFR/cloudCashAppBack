# ============================================
# Dockerfile for BnpCashClaudeApp Backend
# FPT_TUD_EXT: Secure Update Implementation
# ============================================
# Multi-stage build برای بهینه‌سازی حجم Image
# ============================================

# Stage 1: Base Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

# Stage 2: Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# کپی فایل‌های پروژه برای Restore
COPY ["BnpCashClaudeApp.api/BnpCashClaudeApp.api.csproj", "BnpCashClaudeApp.api/"]
COPY ["BnpCashClaudeApp.Application/BnpCashClaudeApp.Application.csproj", "BnpCashClaudeApp.Application/"]
COPY ["BnpCashClaudeApp.Domain/BnpCashClaudeApp.Domain.csproj", "BnpCashClaudeApp.Domain/"]
COPY ["BnpCashClaudeApp.Infrastructure/BnpCashClaudeApp.Infrastructure.csproj", "BnpCashClaudeApp.Infrastructure/"]
COPY ["BnpCashClaudeApp.Persistence/BnpCashClaudeApp.Persistence.csproj", "BnpCashClaudeApp.Persistence/"]

# Restore dependencies
RUN dotnet restore "BnpCashClaudeApp.api/BnpCashClaudeApp.api.csproj"

# کپی تمام فایل‌های سورس
COPY . .

# Build پروژه
WORKDIR "/src/BnpCashClaudeApp.api"
RUN dotnet build "BnpCashClaudeApp.api.csproj" -c Release -o /app/build

# Stage 3: Publish Stage
FROM build AS publish
RUN dotnet publish "BnpCashClaudeApp.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final Runtime Image
FROM base AS final
WORKDIR /app

# کپی فایل‌های Publish شده
COPY --from=publish /app/publish .

# ============================================
# FPT_TUD_EXT.1.2: Version Management Labels
# ============================================
LABEL version="1.0.0"
LABEL maintainer="BnpCash Team"
LABEL description="BnpCashClaudeApp Backend API"
LABEL build-date=""

# ============================================
# Health Check برای Docker
# ============================================
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# ============================================
# Entry Point
# ============================================
ENTRYPOINT ["dotnet", "BnpCashClaudeApp.api.dll"]


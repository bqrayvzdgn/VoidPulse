# ============================================
# VoidPulse API — Multi-stage Dockerfile
# ============================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY src/backend/*.sln ./
COPY src/backend/VoidPulse.Api/*.csproj ./VoidPulse.Api/
COPY src/backend/VoidPulse.Application/*.csproj ./VoidPulse.Application/
COPY src/backend/VoidPulse.Domain/*.csproj ./VoidPulse.Domain/
COPY src/backend/VoidPulse.Infrastructure/*.csproj ./VoidPulse.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy source code (exclude Tests)
COPY src/backend/VoidPulse.Api/ ./VoidPulse.Api/
COPY src/backend/VoidPulse.Application/ ./VoidPulse.Application/
COPY src/backend/VoidPulse.Domain/ ./VoidPulse.Domain/
COPY src/backend/VoidPulse.Infrastructure/ ./VoidPulse.Infrastructure/

# Publish Release build
RUN dotnet publish VoidPulse.Api/VoidPulse.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

# Install curl for healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser -d /app -s /sbin/nologin appuser
RUN chown -R appuser:appuser /app

COPY --from=build --chown=appuser:appuser /app/publish .

USER appuser

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/api/v1/health || exit 1

ENTRYPOINT ["dotnet", "VoidPulse.Api.dll"]

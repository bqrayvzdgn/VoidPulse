# ============================================
# VoidPulse Agent — Multi-stage Dockerfile
# Supports packet capture via libpcap
# ============================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Clear any host NuGet config leaks
ENV NUGET_FALLBACK_PACKAGES=""
RUN dotnet nuget locals all --clear 2>/dev/null || true

COPY VoidPulse.Agent/*.csproj ./VoidPulse.Agent/
RUN dotnet restore VoidPulse.Agent/VoidPulse.Agent.csproj

COPY VoidPulse.Agent/ ./VoidPulse.Agent/

RUN dotnet publish VoidPulse.Agent/VoidPulse.Agent.csproj \
    -c Release \
    -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Install libpcap for packet capture (runtime only)
RUN apt-get update && apt-get install -y --no-install-recommends libpcap0.8t64 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Agent runs as root for packet capture (NET_RAW + NET_ADMIN)
ENTRYPOINT ["dotnet", "VoidPulse.Agent.dll"]

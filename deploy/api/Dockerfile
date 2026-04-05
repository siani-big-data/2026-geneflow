# =============================================================================
# GeneFlow .NET API Dockerfile
# =============================================================================
# Multi-stage build for optimized production image
#
# Build:
#   docker build -t geneflow-api -f deploy/api/Dockerfile .
#
# Run:
#   docker run -p 5145:5145 geneflow-api
# =============================================================================

# -----------------------------------------------------------------------------
# Stage 1: Build
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and project files first (layer caching optimization)
COPY GeneFlow.ApiNet.sln ./
COPY GeneFlow.ApiNet.API/GeneFlow.ApiNet.API.csproj ./GeneFlow.ApiNet.API/
COPY GeneFlow.ApiNet.Application/GeneFlow.ApiNet.Application.csproj ./GeneFlow.ApiNet.Application/
COPY GeneFlow.ApiNet.Domain/GeneFlow.ApiNet.Domain.csproj ./GeneFlow.ApiNet.Domain/
COPY GeneFlow.ApiNet.Infrastructure/GeneFlow.ApiNet.Infrastructure.csproj ./GeneFlow.ApiNet.Infrastructure/
COPY GeneFlow.ApiNet.SharedKernel/GeneFlow.ApiNet.SharedKernel.csproj ./GeneFlow.ApiNet.SharedKernel/

# Restore dependencies (cached unless .csproj files change)
RUN dotnet restore

# Copy all source code
COPY . .

# Build and publish in Release mode
RUN dotnet publish GeneFlow.ApiNet.API -c Release -o /app/publish --no-restore

# -----------------------------------------------------------------------------
# Stage 2: Runtime
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Install required tools
RUN apk add --no-cache \
    curl \
    tzdata \
    icu-libs

# Set timezone
ENV TZ=Europe/Madrid

# Enable globalization support
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Create non-root user for security
RUN addgroup -S geneflow && adduser -S geneflow -G geneflow

# Create application directories
RUN mkdir -p /app/traces /app/logs && \
    chown -R geneflow:geneflow /app

# Copy published application from build stage
COPY --from=build --chown=geneflow:geneflow /app/publish .

# Switch to non-root user
USER geneflow

# Expose API port
EXPOSE 5145

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5145/health || exit 1

# Start application
ENTRYPOINT ["dotnet", "GeneFlow.ApiNet.API.dll"]

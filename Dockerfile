# GeneFlow Datalake Service
FROM python:3.12-slim

# Install uv and system dependencies
COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    libjpeg62-turbo \
    libpng16-16 \
    libwebp7 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy dependency files and README (required by hatchling)
COPY pyproject.toml uv.lock README.md ./

# Install dependencies (no dev, frozen from lockfile)
RUN uv sync --frozen --no-dev

# Copy source code
COPY src/ ./src/

# Create data directories
RUN mkdir -p /app/data/wal /app/data/dlq /app/data/datalake

# Create non-root user
RUN useradd --create-home --shell /bin/bash datalake && chown -R datalake:datalake /app
USER datalake

# Environment variables (override in docker-compose or k8s)
ENV DATALAKE_REDIS_URL=redis://redis:6379
ENV DATALAKE_STORAGE_PROVIDER=local
ENV DATALAKE_LOCAL_STORAGE_PATH=/app/data/datalake
ENV DATALAKE_WAL_PATH=/app/data/wal
ENV DATALAKE_DLQ_PATH=/app/data/dlq
ENV DATALAKE_API_HOST=0.0.0.0
ENV DATALAKE_API_PORT=8080

# Expose API port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run the service
CMD ["uv", "run", "python", "-m", "src.main"]

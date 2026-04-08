FROM python:3.12-slim

COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy project files
COPY pyproject.toml uv.lock README.md ./
COPY src/ ./src/

# Create venv and install dependencies
# Use CUDA index only for x86_64; ARM64 uses CPU-only torch from PyPI
RUN uv venv /app/.venv && \
    if [ "$(uname -m)" = "x86_64" ]; then \
        uv sync --no-dev --extra-index-url https://download.pytorch.org/whl/cu124; \
    else \
        uv sync --no-dev; \
    fi

RUN mkdir -p /app/data
RUN useradd --create-home --shell /bin/bash appuser && chown -R appuser:appuser /app
USER appuser

ENV PATH="/app/.venv/bin:$PATH"
ENV AI_API_HOST=0.0.0.0
ENV AI_API_PORT=8090

EXPOSE 8090

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8090/health || exit 1

CMD ["python", "-m", "src.main"]

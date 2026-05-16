# GeneFlow - Convenciones de Arquitectura y Desarrollo

Este documento define las convenciones estándar que deben seguir todos los módulos de GeneFlow para mantener consistencia en arquitectura, código y APIs.

## Tabla de Contenidos

1. [Estructura de Proyecto](#1-estructura-de-proyecto)
2. [Configuración (Settings)](#2-configuración-settings)
3. [API REST](#3-api-rest)
4. [Modelos de Datos](#4-modelos-de-datos)
5. [Patrones de Arquitectura](#5-patrones-de-arquitectura)
6. [Logging y Observabilidad](#6-logging-y-observabilidad)
7. [Testing](#7-testing)
8. [Naming Conventions](#8-naming-conventions)
9. [Docker](#9-docker)
10. [Git](#10-git)

---

## 1. Estructura de Proyecto

### Organización de Directorios

```
geneflow-{module}/
├── src/
│   ├── __init__.py
│   ├── main.py              # Punto de entrada
│   ├── api.py               # REST API (clase principal)
│   ├── config.py            # Settings (pydantic-settings)
│   ├── models.py            # Modelos de dominio (dataclasses)
│   ├── {feature}/           # Subdominios por funcionalidad
│   │   ├── __init__.py
│   │   ├── {component}.py
│   │   └── handlers/        # Event handlers si aplica
│   └── storage/             # Proveedores de almacenamiento
│       ├── storage.py       # Abstract base class
│       └── {provider}.py    # Implementaciones concretas
├── tests/
│   ├── __init__.py
│   ├── conftest.py          # Fixtures compartidas
│   └── test_{module}.py     # Tests unitarios
├── docs/
│   └── API_CONVENTIONS.md   # Documentación de API
├── pyproject.toml           # Configuración del proyecto (uv)
├── pytest.ini               # Configuración de pytest
├── Dockerfile
├── .env.example
├── .gitignore
└── README.md
```

### Convenciones de Naming para Directorios

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Directorios | `snake_case` | `src/`, `tests/`, `storage/` |
| Módulos Python | `snake_case` | `consumer.py`, `deduplication.py` |
| Subdominios | `snake_case` por categoría | `postgres/`, `qdrant/`, `handlers/` |

---

## 2. Configuración (Settings)

### Patrón Settings con pydantic-settings

```python
from typing import Literal
from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    # Sección: Redis
    redis_url: str = "redis://redis:6379"
    redis_consumer_group: str = "geneflow-{module}-consumers"

    # Sección: Storage
    storage_provider: Literal["local", "supabase", "minio"] = "local"
    local_storage_path: str = "./data/{module}"

    # Sección: API
    api_host: str = "0.0.0.0"
    api_port: int = 8080
    api_key: str = ""  # Vacío = sin autenticación

    # Sección: CORS
    cors_origins: list[str] = ["*"]
    cors_allow_credentials: bool = True
    cors_allow_methods: list[str] = ["*"]
    cors_allow_headers: list[str] = ["*"]

    # Sección: Logging
    log_level: str = "INFO"
    log_requests: bool = True

    class Config:
        env_prefix = "{MODULE}_"  # Prefijo único por módulo
        env_file = ".env"
```

### Variables de Entorno

**Formato**: `{MODULENAME}_{SECCION}_{SETTING}`

```bash
# Redis
DATALAKE_REDIS_URL=redis://localhost:6379

# Storage
DATALAKE_STORAGE_PROVIDER=local
DATALAKE_LOCAL_STORAGE_PATH=./data/datalake

# API
DATALAKE_API_HOST=0.0.0.0
DATALAKE_API_PORT=8080
DATALAKE_API_KEY=secret-key-optional

# Logging
DATALAKE_LOG_LEVEL=INFO
```

---

## 3. API REST

### Estructura de Clase API

```python
class {ModuleName}API:
    """REST API for {ModuleName}."""

    def __init__(self, storage: StorageProvider, settings: Settings):
        self.storage = storage
        self.settings = settings
        self._verify_api_key = create_api_key_dependency(settings)

        self.app = FastAPI(
            title="GeneFlow {ModuleName}",
            version="1.0.0",
            openapi_tags=TAGS_METADATA,
        )

        self._setup_middleware()
        create_exception_handlers(self.app)
        self._setup_routes()

    def _setup_middleware(self) -> None:
        # CORS
        self.app.add_middleware(
            CORSMiddleware,
            allow_origins=self.settings.cors_origins,
            allow_credentials=self.settings.cors_allow_credentials,
            allow_methods=self.settings.cors_allow_methods,
            allow_headers=self.settings.cors_allow_headers,
        )
        # Logging middleware

    def _setup_routes(self) -> None:
        # Definición de endpoints
```

### Convenciones de Rutas

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Rutas | kebab-case, plurales | `/events`, `/categories`, `/dlq/retry-all` |
| Path params | snake_case | `{event_id}`, `{category}` |
| Query params | snake_case | `event_type`, `start_date`, `limit`, `offset` |

### Endpoint Health (Obligatorio)

```python
@self.app.get(
    "/health",
    response_model=HealthResponse,
    tags=["Health"],
    summary="Service health check",
    description="Returns service health status. No authentication required."
)
async def health():
    return HealthResponse(
        status="healthy" if await self.storage.health_check() else "degraded",
        storage_healthy=await self.storage.health_check(),
    )
```

### Autenticación con API Key

```python
def create_api_key_dependency(settings: Settings):
    async def verify_api_key(request: Request) -> None:
        if not settings.api_key:
            return  # Sin autenticación configurada

        api_key = request.headers.get("X-API-Key")
        if api_key != settings.api_key:
            raise HTTPException(status_code=401, detail="Invalid or missing API key")

    return verify_api_key

# Uso en endpoint:
@self.app.get("/protected", dependencies=[Depends(self._verify_api_key)])
async def protected_endpoint():
    pass
```

### Headers Estándar

| Header | Propósito |
|--------|-----------|
| `X-API-Key` | Autenticación |
| `X-Correlation-ID` | Trazabilidad (auto-generado si no presente) |

### Response Models

```python
from pydantic import BaseModel, Field

class ErrorResponse(BaseModel):
    """Standard error response."""
    error: str = Field(..., description="Error type")
    message: str = Field(..., description="Error message")
    detail: Optional[str] = Field(None, description="Additional details")
    correlation_id: str = Field(..., description="Request correlation ID")

class HealthResponse(BaseModel):
    """Service health status."""
    status: str = Field(..., description="Service status", examples=["healthy", "degraded"])
    storage_healthy: bool = Field(..., description="Storage provider health")
```

### Error Handling

```python
error_map = {
    400: "bad_request",
    401: "unauthorized",
    403: "forbidden",
    404: "not_found",
    405: "method_not_allowed",
    409: "conflict",
    422: "validation_error",
    429: "too_many_requests",
    500: "internal_error",
    503: "service_unavailable",
}

def create_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(StarletteHTTPException)
    async def http_exception_handler(request: Request, exc: StarletteHTTPException):
        correlation_id = correlation_id_ctx.get() or "unknown"
        return JSONResponse(
            status_code=exc.status_code,
            content=ErrorResponse(
                error=error_map.get(exc.status_code, "error"),
                message=str(exc.detail),
                correlation_id=correlation_id,
            ).model_dump(),
        )
```

### OpenAPI Tags

```python
TAGS_METADATA = [
    {"name": "Health", "description": "Service health and metrics endpoints."},
    {"name": "Categories", "description": "List and inspect event categories."},
    {"name": "Events", "description": "Query events by category and filters."},
]
```

---

## 4. Modelos de Datos

### Dataclasses para Dominio

```python
from dataclasses import dataclass, field
from enum import Enum
from datetime import datetime
from typing import Optional

class EventCategory(str, Enum):
    """Event categories."""
    USERS = "users"
    STUDIES = "studies"
    TRACES = "traces"

@dataclass
class DomainEvent:
    """Evento del dominio."""
    eventId: str
    type: str
    category: str
    timestamp: datetime
    data: dict

    @classmethod
    def from_dict(cls, data: dict) -> "DomainEvent":
        """Factory method."""
        pass

    def to_dict(self) -> dict:
        """Serializar a diccionario."""
        pass
```

### Convenciones de Naming en Modelos

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Dataclass | PascalCase | `EventBusMessage`, `DatalakeEvent` |
| Response Model | PascalCase + "Response" | `HealthResponse`, `EventsResponse` |
| Enum | PascalCase | `EventCategory`, `MounterMode` |
| Atributos | camelCase | `eventId`, `correlationId`, `receivedAt` |
| Métodos | snake_case | `from_dict()`, `to_json_line()` |

---

## 5. Patrones de Arquitectura

### Strategy Pattern (Storage Provider)

```python
from abc import ABC, abstractmethod

class StorageProvider(ABC):
    @abstractmethod
    async def save(self, data: dict) -> None:
        pass

    @abstractmethod
    async def read(self, id: str) -> dict:
        pass

    @abstractmethod
    async def health_check(self) -> bool:
        pass

# Implementaciones:
class LocalStorageProvider(StorageProvider): ...
class MinIOStorageProvider(StorageProvider): ...
class SupabaseStorageProvider(StorageProvider): ...
```

### Factory Pattern

```python
def get_storage_provider(provider: str, **kwargs) -> StorageProvider:
    providers = {
        "local": LocalStorageProvider,
        "minio": MinIOStorageProvider,
        "supabase": SupabaseStorageProvider,
    }
    return providers[provider](**kwargs)
```

### Ciclo de Vida (start/stop)

```python
class Component:
    def __init__(self):
        self._running = False

    async def start(self) -> None:
        """Inicializar recursos."""
        self._running = True
        logger.info("component_started")

    async def stop(self) -> None:
        """Graceful shutdown."""
        self._running = False
        logger.info("component_stopped")

    async def health_check(self) -> bool:
        """Verificar estado."""
        return self._running

    @property
    def metrics(self) -> dict:
        """Exponer métricas."""
        return {"running": self._running}
```

### Dependency Injection

```python
# Constructor injection
def __init__(self, storage: StorageProvider, settings: Settings):
    self.storage = storage
    self.settings = settings

# FastAPI dependency
_: None = Depends(self._verify_api_key)
```

---

## 6. Logging y Observabilidad

### Configuración structlog

```python
import structlog

structlog.configure(
    processors=[
        structlog.stdlib.add_log_level,
        structlog.processors.TimeStamper(fmt="iso"),
        structlog.processors.StackInfoRenderer(),
        structlog.processors.format_exc_info,
        structlog.processors.UnicodeDecoder(),
        structlog.processors.JSONRenderer(),
    ],
    wrapper_class=structlog.BoundLogger,
    context_class=dict,
    logger_factory=structlog.PrintLoggerFactory(),
    cache_logger_on_first_use=True,
)

logger = structlog.get_logger()
```

### Correlation ID

```python
from contextvars import ContextVar

correlation_id_ctx: ContextVar[str] = ContextVar("correlation_id", default="")

# En middleware:
@self.app.middleware("http")
async def logging_middleware(request: Request, call_next):
    correlation_id = request.headers.get("X-Correlation-ID") or str(uuid.uuid4())
    correlation_id_ctx.set(correlation_id)

    logger.info(
        "request_started",
        correlation_id=correlation_id,
        method=request.method,
        path=request.url.path,
    )

    response = await call_next(request)

    logger.info(
        "request_completed",
        correlation_id=correlation_id,
        status_code=response.status_code,
    )

    return response
```

### Eventos de Log Estándar

| Evento | Nivel | Campos |
|--------|-------|--------|
| `request_started` | INFO | `correlation_id`, `method`, `path` |
| `request_completed` | INFO | `correlation_id`, `status_code`, `duration_ms` |
| `request_failed` | ERROR | `correlation_id`, `error`, `path` |
| `{module}_starting` | INFO | N/A |
| `config_loaded` | INFO | configuración relevante |
| `graceful_shutdown_starting` | INFO | N/A |

---

## 7. Testing

### Estructura de Tests

```
tests/
├── conftest.py          # Fixtures compartidas
├── test_api.py          # Tests de API
├── test_config.py       # Tests de configuración
├── test_models.py       # Tests de modelos
└── {feature}/           # Tests por feature
    └── test_{module}.py
```

### Convenciones de Naming

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Archivo | `test_{module}.py` | `test_api.py`, `test_buffer.py` |
| Clase | `Test{Entity}` | `TestHealthEndpoint` |
| Método | `test_{action}_{condition}` | `test_health_no_auth_required` |

### Fixtures Estándar

```python
import pytest
import pytest_asyncio
from pathlib import Path
import tempfile
from fastapi.testclient import TestClient

@pytest.fixture
def temp_dir() -> Path:
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)

@pytest.fixture
def settings(temp_dir: Path) -> Settings:
    return Settings(
        storage_provider="local",
        local_storage_path=str(temp_dir / "data"),
        api_key="test-api-key",
    )

@pytest_asyncio.fixture
async def storage(temp_dir: Path):
    provider = LocalStorageProvider(base_path=str(temp_dir))
    yield provider
    await provider.close()

@pytest.fixture
def api_client(settings: Settings, storage: StorageProvider) -> TestClient:
    api = ModuleAPI(storage=storage, settings=settings)
    return TestClient(api.app)
```

### Configuración pytest.ini

```ini
[pytest]
asyncio_mode = auto
testpaths = tests
python_files = test_*.py
python_classes = Test*
python_functions = test_*
addopts = -v --tb=short
```

---

## 8. Naming Conventions

### Resumen Completo

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| **Archivos** | `snake_case.py` | `consumer.py` |
| **Directorios** | `snake_case/` | `handlers/` |
| **Clases** | `PascalCase` | `DatalakeConsumer` |
| **Response Models** | `PascalCase` + Response | `HealthResponse` |
| **Funciones** | `snake_case` | `process_event()` |
| **Métodos privados** | `_snake_case` | `_setup_routes()` |
| **Variables** | `snake_case` | `event_count` |
| **Constantes** | `UPPER_SNAKE_CASE` | `MAX_RETRIES` |
| **Rutas API** | `kebab-case` | `/dead-letter-queue` |
| **Path params** | `snake_case` | `{event_id}` |
| **Query params** | `snake_case` | `start_date` |
| **Headers** | `X-Kebab-Case` | `X-Correlation-ID` |
| **Env vars** | `UPPER_SNAKE_CASE` | `DATALAKE_API_PORT` |

---

## 9. Docker

### Dockerfile Estándar

```dockerfile
FROM python:3.12-slim

COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY pyproject.toml uv.lock ./
RUN uv sync --frozen --no-dev

COPY src/ ./src/

RUN mkdir -p /app/data
RUN useradd --create-home --shell /bin/bash appuser && chown -R appuser:appuser /app
USER appuser

ENV {MODULE}_API_HOST=0.0.0.0
ENV {MODULE}_API_PORT=8080

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

CMD ["uv", "run", "python", "-m", "src.main"]
```

### Convenciones Docker

- **Base image**: `python:3.12-slim`
- **Package manager**: `uv`
- **Non-root user**: Crear usuario específico
- **Health check**: Obligatorio, usando `/health`
- **Entry point**: `uv run python -m src.main`

---

## 10. Git

### Formato de Commits

```
type(scope): description
```

**Tipos**:
- `feat`: Nueva funcionalidad
- `fix`: Corrección de bug
- `test`: Añadir/modificar tests
- `docs`: Documentación
- `refactor`: Refactorización
- `style`: Formato, sin cambios de lógica
- `deploy`: Cambios de deployment

**Scopes**: `api`, `storage`, `consumer`, `mounters`, etc.

**Ejemplos**:
```
feat(api): add pagination to events endpoint
fix(storage): handle connection timeout
test(mounters): add unit tests for postgres handler
docs(api): update endpoint documentation
```

### .gitignore Estándar

```gitignore
# Python
__pycache__/
*.py[cod]
.venv/
.Python

# Testing
.pytest_cache/
.coverage
htmlcov/

# IDEs
.idea/
.vscode/

# Project data
data/
*.jsonl

# Environment
.env
.env.local

# OS
.DS_Store
Thumbs.db
```

---

## Checklist para Nuevos Módulos

- [ ] Crear estructura de directorios según convención
- [ ] Implementar `src/config.py` con `Settings` y prefijo único
- [ ] Crear `src/api.py` con clase `{ModuleName}API`
- [ ] Configurar CORS middleware
- [ ] Configurar logging middleware con correlation ID
- [ ] Implementar exception handlers globales
- [ ] Añadir endpoint `/health` público (sin auth)
- [ ] Crear response models con ejemplos
- [ ] Usar `Depends(verify_api_key)` en endpoints protegidos
- [ ] Configurar structlog para logging JSON
- [ ] Crear `tests/conftest.py` con fixtures
- [ ] Añadir tests unitarios en `tests/test_*.py`
- [ ] Configurar `pytest.ini` con `asyncio_mode = auto`
- [ ] Crear Dockerfile con health check
- [ ] Añadir `.env.example` con variables documentadas
- [ ] Documentar API en `docs/API_CONVENTIONS.md`

---

## Stack Tecnológico

| Aspecto | Tecnología |
|---------|-----------|
| Lenguaje | Python 3.12+ |
| Package Manager | uv |
| Web Framework | FastAPI |
| Config | pydantic-settings |
| Logging | structlog (JSON) |
| Testing | pytest + pytest-asyncio |
| Linting | ruff |
| Contenedor | Docker + python:3.12-slim |
| Async | asyncio nativo |

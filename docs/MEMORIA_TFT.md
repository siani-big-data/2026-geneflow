# GeneFlow Datalake: Memoria Técnica

## Índice

1. [Introducción](#1-introducción)
2. [Objetivos](#2-objetivos)
3. [Arquitectura del Sistema](#3-arquitectura-del-sistema)
4. [Stack Tecnológico](#4-stack-tecnológico)
5. [Diseño e Implementación](#5-diseño-e-implementación)
6. [Flujo de Datos](#6-flujo-de-datos)
7. [Patrones de Diseño](#7-patrones-de-diseño)
8. [API REST](#8-api-rest)
9. [Sistema de Mounters](#9-sistema-de-mounters)
10. [CLI](#10-cli)
11. [Testing](#11-testing)
12. [Despliegue](#12-despliegue)
13. [Conclusiones](#13-conclusiones)

---

## 1. Introducción

GeneFlow Datalake es un **almacén de eventos inmutable** diseñado como fuente única de verdad (*single source of truth*) para la plataforma GeneFlow. El sistema consume todos los eventos generados por los diferentes microservicios de la plataforma a través de Redis Streams y los persiste en formato JSONL (JSON Lines), permitiendo auditoría completa, replay de eventos y análisis histórico.

### 1.1 Contexto

En arquitecturas basadas en eventos (*event-driven*), es fundamental mantener un registro inmutable de todos los eventos del sistema. Este registro permite:

- **Auditoría**: Trazabilidad completa de todas las acciones del sistema
- **Replay**: Reconstrucción del estado del sistema a partir de eventos
- **Analytics**: Análisis histórico y generación de métricas
- **Debugging**: Investigación de incidencias con contexto completo

### 1.2 Alcance

El Datalake procesa eventos de 9 dominios de negocio:

| Categoría | Descripción |
|-----------|-------------|
| `users` | Registro, login, actualizaciones de perfil |
| `studies` | Operaciones CRUD de estudios |
| `traces` | Carga, procesamiento y archivado de trazas |
| `alignments` | Creación y resultados de alineamientos |
| `subscriptions` | Ciclo de vida de suscripciones |
| `plans` | Cambios de planes y facturación |
| `ai` | Interacciones con GeneFlow AI |
| `blast` | Trabajos BLAST y resultados |
| `system` | Eventos de sistema y mantenimiento |

---

## 2. Objetivos

### 2.1 Objetivos Funcionales

1. **Consumo de eventos**: Procesar eventos de múltiples streams de Redis de forma concurrente
2. **Persistencia durable**: Garantizar que ningún evento se pierda, incluso ante fallos
3. **Deduplicación**: Evitar eventos duplicados mediante identificadores únicos
4. **Consulta de eventos**: API REST para búsqueda y replay de eventos
5. **Gestión de fallos**: Sistema de reintentos con Dead Letter Queue (DLQ)

### 2.2 Objetivos No Funcionales

1. **Alta disponibilidad**: Recuperación automática ante fallos
2. **Escalabilidad horizontal**: Soporte para múltiples consumidores
3. **Rendimiento**: Procesamiento eficiente mediante buffering y escritura por lotes
4. **Observabilidad**: Métricas y logging estructurado
5. **Flexibilidad de almacenamiento**: Soporte para múltiples backends (local, MinIO, Supabase)

### 2.3 Garantías de Entrega

| Garantía | Implementación |
|----------|----------------|
| **No data loss** | Write-Ahead Log (WAL) antes de buffering |
| **No duplicates** | Deduplicación por `eventId` con ventana de 24h |
| **Ordered within category** | Un consumidor por stream de categoría |
| **Crash recovery** | Replay de WAL al iniciar |
| **At-least-once delivery** | ACK solo después de persistencia exitosa |

---

## 3. Arquitectura del Sistema

### 3.1 Diagrama de Arquitectura

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            GENEFLOW PLATFORM                                │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐      │
│  │ Users    │  │ Studies  │  │ Traces   │  │ AI       │  │ BLAST    │      │
│  │ Service  │  │ Service  │  │ Service  │  │ Service  │  │ Service  │      │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘      │
│       │             │             │             │             │            │
│       └─────────────┴─────────────┴─────────────┴─────────────┘            │
│                                   │                                         │
│                                   ▼                                         │
│                     ┌─────────────────────────┐                            │
│                     │      REDIS STREAMS      │                            │
│                     │  (Event Bus / Broker)   │                            │
│                     └───────────┬─────────────┘                            │
└─────────────────────────────────┼───────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          GENEFLOW DATALAKE                                  │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         CONSUMER LAYER                               │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │   │
│  │  │   Redis     │  │   Event     │  │   Event     │                  │   │
│  │  │  Consumer   │──│  Validator  │──│ Transformer │                  │   │
│  │  │ (XREADGROUP)│  │             │  │             │                  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                  │   │
│  └──────────────────────────┬──────────────────────────────────────────┘   │
│                             │                                               │
│  ┌──────────────────────────▼──────────────────────────────────────────┐   │
│  │                      PROCESSING LAYER                                │   │
│  │                                                                      │   │
│  │  ┌─────────────┐      ┌─────────────┐      ┌─────────────┐          │   │
│  │  │ Deduplicator│      │   Buffer    │      │    WAL      │          │   │
│  │  │  (eventId)  │─────▶│  (Batching) │─────▶│  (Durability)│         │   │
│  │  │  24h TTL    │      │  size/time  │      │             │          │   │
│  │  └─────────────┘      └──────┬──────┘      └─────────────┘          │   │
│  │                              │                                       │   │
│  │                              ▼                                       │   │
│  │  ┌─────────────┐      ┌─────────────┐                               │   │
│  │  │   Retry     │◀─────│   Flush     │                               │   │
│  │  │  Handler    │ fail │  Callback   │                               │   │
│  │  │  (exp.back) │      │             │                               │   │
│  │  └──────┬──────┘      └──────┬──────┘                               │   │
│  │         │                    │                                       │   │
│  │         ▼                    ▼                                       │   │
│  │  ┌─────────────┐      ┌─────────────┐                               │   │
│  │  │    DLQ      │      │   XACK      │                               │   │
│  │  │ (Dead Letter│      │  (Confirm)  │                               │   │
│  │  └─────────────┘      └─────────────┘                               │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                        STORAGE LAYER                                 │   │
│  │                                                                      │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │   │
│  │  │    Local    │  │    MinIO    │  │  Supabase   │                  │   │
│  │  │ (Filesystem)│  │ (S3-compat) │  │  (Storage)  │                  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                  │   │
│  │                           │                                          │   │
│  │                           ▼                                          │   │
│  │              ┌───────────────────────┐                              │   │
│  │              │    JSONL Files        │                              │   │
│  │              │  events/{cat}/{date}  │                              │   │
│  │              └───────────────────────┘                              │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                          API LAYER                                   │   │
│  │                                                                      │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │   │
│  │  │   FastAPI   │  │   Swagger   │  │  API Key    │                  │   │
│  │  │  (REST API) │  │   (OpenAPI) │  │   (Auth)    │                  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                  │   │
│  │                                                                      │   │
│  │  Endpoints:                                                          │   │
│  │  • GET /health          - Estado del servicio                       │   │
│  │  • GET /categories      - Categorías con datos                      │   │
│  │  • GET /events/{cat}    - Consulta de eventos                       │   │
│  │  • GET /replay/{cat}    - Replay cronológico                        │   │
│  │  • GET /dlq             - Dead Letter Queue                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Componentes Principales

| Componente | Responsabilidad | Archivo |
|------------|-----------------|---------|
| **Consumer** | Consume eventos de Redis Streams | `consumer/consumer.py` |
| **MessageParser** | Parsea y valida mensajes de Redis | `consumer/message_parser.py` |
| **Deduplicator** | Filtra eventos duplicados | `consumer/deduplication.py` |
| **Buffer** | Acumula eventos para escritura por lotes | `buffer/event_buffer.py` |
| **WAL** | Write-Ahead Log para durabilidad | `buffer/wal.py` |
| **RetryHandler** | Gestiona reintentos | `retry/retry_handler.py` |
| **Backoff** | Cálculo de delays exponenciales | `retry/backoff.py` |
| **DLQ** | Dead Letter Queue | `retry/dlq.py` |
| **StorageProvider** | Abstracción de almacenamiento | `storage/` |
| **API** | Endpoints REST para consulta | `api/app.py` |
| **Lifecycle** | Orquestación de arranque/parada | `lifecycle.py` |
| **Bootstrap** | Inicialización de componentes | `bootstrap.py` |

---

## 4. Stack Tecnológico

### 4.1 Lenguaje y Runtime

| Tecnología | Versión | Justificación |
|------------|---------|---------------|
| **Python** | 3.12+ | Tipado moderno, async/await nativo, ecosistema maduro |
| **asyncio** | stdlib | Concurrencia eficiente para I/O-bound operations |

### 4.2 Frameworks y Librerías

| Librería | Versión | Uso |
|----------|---------|-----|
| **FastAPI** | 0.110+ | Framework web async con OpenAPI automático |
| **Pydantic** | 2.5+ | Validación de datos y configuración |
| **pydantic-settings** | 2.1+ | Gestión de configuración desde env vars |
| **Redis (redis-py)** | 5.0+ | Cliente Redis async para Streams |
| **aiofiles** | 23.2+ | I/O de archivos asíncrono |
| **aiobotocore** | 2.12+ | Cliente S3 async (MinIO, StorageMounter) |
| **asyncpg** | 0.29+ | Cliente PostgreSQL async (PostgresMounter) |
| **httpx** | 0.27+ | Cliente HTTP async (Supabase) |
| **qdrant-client** | 1.7+ | Cliente Qdrant async (QdrantMounter) |
| **structlog** | 24.1+ | Logging estructurado en JSON |
| **uvicorn** | 0.27+ | Servidor ASGI |

### 4.3 Herramientas de Desarrollo

| Herramienta | Uso |
|-------------|-----|
| **uv** | Package manager ultrarrápido (reemplazo de pip) |
| **pytest** | Framework de testing |
| **pytest-asyncio** | Testing de código async |
| **pytest-cov** | Cobertura de código |
| **ruff** | Linter y formatter |

### 4.4 Infraestructura

| Servicio | Uso |
|----------|-----|
| **Redis Streams** | Message broker / Event bus |
| **PostgreSQL** | Base de datos operacional (datamarts) |
| **MinIO** | Object storage S3-compatible (trazas) |
| **Qdrant** | Base de datos vectorial (embeddings) |
| **Supabase Storage** | Cloud storage (opcional) |
| **Docker** | Containerización |

---

## 5. Diseño e Implementación

### 5.1 Modelos de Datos

#### 5.1.1 EventCategory (Enum)

```python
class EventCategory(str, Enum):
    """Categorías de eventos que coinciden con los streams de Redis."""
    USERS = "users"
    STUDIES = "studies"
    TRACES = "traces"
    ALIGNMENTS = "alignments"
    SUBSCRIPTIONS = "subscriptions"
    PLANS = "plans"
    AI = "ai"
    BLAST = "blast"
    SYSTEM = "system"
```

#### 5.1.2 EventBusMessage

Representa un evento tal como llega desde Redis:

```python
@dataclass
class EventBusMessage:
    eventId: str           # UUID único del evento
    type: str              # Tipo de evento (ej: "UserRegistered")
    category: str          # Categoría (ej: "users")
    timestamp: int         # Unix timestamp en milisegundos
    data: str              # Payload serializado como JSON string
    source: str            # Servicio origen
    version: str           # Versión del esquema
    correlationId: str     # ID de correlación para trazabilidad
```

#### 5.1.3 DatalakeEvent

Evento normalizado para persistencia:

```python
@dataclass
class DatalakeEvent:
    eventId: str
    type: str
    category: str
    timestamp: datetime    # Convertido a datetime
    streamId: str          # ID del mensaje en Redis
    data: dict             # Payload deserializado
    receivedAt: datetime   # Momento de recepción
```

### 5.2 Consumer (consumer.py)

El consumidor es el componente central que orquesta todo el flujo:

```python
class DatalakeConsumer:
    def __init__(self, settings: Settings, storage: StorageProvider):
        self.deduplicator = EventDeduplicator(...)
        self.retry_handler = RetryHandler(...)
        self.buffer = EventBuffer(
            flush_callback=self._persist_batch,
            ...
        )
```

#### Flujo de procesamiento:

1. **XREADGROUP**: Lee batch de eventos de Redis
2. **Validación**: Parsea y valida estructura del evento
3. **Deduplicación**: Verifica si `eventId` ya fue procesado
4. **Buffer**: Añade al buffer con WAL
5. **Flush**: Cuando se alcanza tamaño o tiempo, persiste
6. **XACK**: Confirma eventos a Redis

### 5.3 Deduplicador (deduplication.py)

Mantiene un set en memoria de `eventId` vistos:

```python
class EventDeduplicator:
    def __init__(self, ttl_hours: int = 24, max_size: int = 100000):
        self._seen: dict[str, datetime] = {}  # eventId -> timestamp

    async def is_duplicate(self, event_id: str) -> bool:
        return event_id in self._seen

    async def mark_seen(self, event_id: str) -> None:
        self._seen[event_id] = datetime.utcnow()
```

**Características:**
- TTL de 24 horas para limitar memoria
- Límite de 100,000 eventos en memoria
- Cleanup periódico de entradas expiradas

### 5.4 Buffer y WAL (buffer.py)

El buffer implementa el patrón Write-Ahead Log:

```python
class EventBuffer:
    async def add(self, category, date, event_line, stream_name, msg_id):
        # 1. Escribir a WAL (durabilidad)
        await self._write_to_wal(category, date_str, event_line)

        # 2. Añadir al buffer en memoria
        self._buffer[key].append(event_line)
        self._pending_acks[key].append((stream_name, msg_id))

        # 3. Flush si se alcanza el tamaño máximo
        if self._count >= self.max_size:
            await self._flush_all()
```

**Triggers de flush:**
- **Por tamaño**: Cuando se acumulan N eventos (default: 1000)
- **Por tiempo**: Cada N segundos (default: 5s)
- **Por shutdown**: Flush final al detener el servicio

### 5.5 Retry Handler (retry.py)

Implementa reintentos con backoff exponencial:

```python
class RetryHandler:
    def _calculate_next_retry(self, retry_count: int) -> datetime:
        # delay = min(base * 2^count, max)
        # Ejemplo: 1s → 2s → 4s → 8s → 16s → ... → 300s max
        delay = min(self.base_delay * (2 ** retry_count), self.max_delay)
        return datetime.utcnow() + timedelta(seconds=delay)
```

**Flujo de retry:**
1. Evento falla en persistencia
2. Se añade a cola de retry con `nextRetryAt`
3. Loop periódico reintenta eventos maduros
4. Si excede `max_retries`, se mueve a DLQ

### 5.6 Storage Providers

#### 5.6.1 Interfaz Abstracta

```python
class StorageProvider(ABC):
    @abstractmethod
    async def append_events_batch(self, category: str, date: datetime,
                                   event_lines: list[str]) -> None: ...

    @abstractmethod
    async def read_events(self, category: str, date: datetime) -> list[str]: ...

    @abstractmethod
    async def list_categories(self) -> list[str]: ...

    @abstractmethod
    async def health_check(self) -> bool: ...
```

#### 5.6.2 LocalStorageProvider

Almacena en sistema de archivos local:

```
data/datalake/events/
├── users/
│   ├── 2026-03-24.jsonl
│   └── 2026-03-25.jsonl
├── traces/
│   └── 2026-03-25.jsonl
└── studies/
    └── 2026-03-25.jsonl
```

#### 5.6.3 MinIOStorageProvider

Compatible con cualquier storage S3:
- AWS S3
- MinIO
- DigitalOcean Spaces
- Cloudflare R2

```python
class MinIOStorageProvider(StorageProvider):
    async def append_events_batch(self, ...):
        # 1. GET objeto existente (si existe)
        # 2. Concatenar nuevo contenido
        # 3. PUT objeto actualizado
```

#### 5.6.4 SupabaseStorageProvider

Usa la API REST de Supabase Storage:

```python
class SupabaseStorageProvider(StorageProvider):
    async def append_events_batch(self, ...):
        # POST /object/{bucket}/{path} con x-upsert: true
```

### 5.7 Configuración (config.py)

Usa `pydantic-settings` para configuración declarativa:

```python
class Settings(BaseSettings):
    # Redis
    redis_url: str = "redis://redis:6379"
    redis_consumer_group: str = "geneflow-datalake-consumers"

    # Storage
    storage_provider: Literal["local", "minio", "supabase"] = "local"

    # Buffer
    buffer_max_size: int = 1000
    buffer_flush_interval: float = 5.0

    class Config:
        env_prefix = "DATALAKE_"
        env_file = ".env"
```

**Variables de entorno con prefijo `DATALAKE_`:**
- `DATALAKE_REDIS_URL`
- `DATALAKE_STORAGE_PROVIDER`
- `DATALAKE_API_KEY`
- etc.

---

## 6. Flujo de Datos

### 6.1 Flujo Principal (Happy Path)

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Redis   │    │ Consumer │    │  Dedup   │    │  Buffer  │    │ Storage  │
│ Streams  │    │          │    │          │    │  + WAL   │    │          │
└────┬─────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘
     │               │               │               │               │
     │  XREADGROUP   │               │               │               │
     │──────────────▶│               │               │               │
     │               │               │               │               │
     │   [events]    │  is_duplicate?│               │               │
     │◀──────────────│──────────────▶│               │               │
     │               │               │               │               │
     │               │     false     │               │               │
     │               │◀──────────────│               │               │
     │               │               │               │               │
     │               │  mark_seen    │               │               │
     │               │──────────────▶│               │               │
     │               │               │               │               │
     │               │               │   add(event)  │               │
     │               │               │──────────────▶│               │
     │               │               │               │               │
     │               │               │               │──┐ write WAL  │
     │               │               │               │  │            │
     │               │               │               │◀─┘            │
     │               │               │               │               │
     │               │               │               │ [size/time]   │
     │               │               │               │──────────────▶│
     │               │               │               │               │
     │               │               │               │   append()    │
     │               │               │               │──────────────▶│
     │               │               │               │               │
     │               │               │               │     ok        │
     │               │               │               │◀──────────────│
     │               │               │               │               │
     │               │               │               │──┐ clear WAL  │
     │               │               │               │  │            │
     │               │               │               │◀─┘            │
     │               │               │               │               │
     │     XACK      │               │               │               │
     │◀──────────────│               │               │               │
     │               │               │               │               │
```

### 6.2 Flujo de Error y Retry

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Buffer  │    │  Storage │    │  Retry   │    │   DLQ    │
│          │    │          │    │ Handler  │    │          │
└────┬─────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘
     │               │               │               │
     │   append()    │               │               │
     │──────────────▶│               │               │
     │               │               │               │
     │    ERROR      │               │               │
     │◀──────────────│               │               │
     │               │               │               │
     │  add_failed() │               │               │
     │──────────────────────────────▶│               │
     │               │               │               │
     │               │               │──┐            │
     │               │               │  │ wait       │
     │               │               │  │ backoff    │
     │               │               │◀─┘            │
     │               │               │               │
     │               │   retry()     │               │
     │               │◀──────────────│               │
     │               │               │               │
     │               │    ERROR      │               │
     │               │──────────────▶│               │
     │               │               │               │
     │               │               │ [max retries] │
     │               │               │──────────────▶│
     │               │               │               │
     │               │               │    write      │
     │               │               │    JSONL      │
     │               │               │               │
```

---

## 7. Patrones de Diseño

### 7.1 Write-Ahead Log (WAL)

**Problema**: Pérdida de datos si el proceso falla entre recibir un evento y persistirlo.

**Solución**: Escribir a un log durable antes de procesar.

```python
async def add(self, ...):
    # 1. WAL primero (durabilidad)
    await self._write_to_wal(category, date_str, event_line)

    # 2. Luego buffer en memoria (rendimiento)
    self._buffer[key].append(event_line)
```

**Recuperación al iniciar:**
```python
async def _recover_from_wal(self):
    for wal_file in self.wal_path.glob("*.wal"):
        # Parsear nombre: {category}_{date}.wal
        # Cargar líneas al buffer
        # El flush normal las persistirá
```

### 7.2 Consumer Groups

**Problema**: Escalar horizontalmente el consumo de eventos.

**Solución**: Redis Consumer Groups permiten múltiples consumidores.

```python
# Cada instancia del Datalake tiene un nombre único
await redis.xreadgroup(
    groupname="datalake-consumers",
    consumername="datalake-1",  # o datalake-2, etc.
    streams={...},
)
```

**Garantías:**
- Cada mensaje se entrega a exactamente un consumidor del grupo
- Si un consumidor falla, los mensajes pending se pueden reclamar

### 7.3 Exponential Backoff

**Problema**: Reintentar inmediatamente sobrecarga el sistema.

**Solución**: Incrementar el tiempo de espera exponencialmente.

```python
def _calculate_next_retry(self, retry_count: int) -> datetime:
    delay = min(self.base_delay * (2 ** retry_count), self.max_delay)
    # Intento 0: 1s
    # Intento 1: 2s
    # Intento 2: 4s
    # Intento 3: 8s
    # ...
    # Máximo: 300s (5 minutos)
    return datetime.utcnow() + timedelta(seconds=delay)
```

### 7.4 Dead Letter Queue (DLQ)

**Problema**: Eventos que fallan repetidamente bloquean el sistema.

**Solución**: Mover a una cola separada para inspección manual.

```python
async def _move_to_dlq(self, event: RetryableEvent, error: str):
    dlq_record = {
        "eventId": event.id,
        "category": event.category,
        "eventLine": event.eventLine,
        "retryCount": event.retryCount,
        "lastError": error,
        "movedToDlqAt": datetime.utcnow().isoformat(),
    }
    # Persistir en archivo JSONL separado
    await write_to_dlq_file(dlq_record)
```

### 7.5 Strategy Pattern (Storage)

**Problema**: Soportar múltiples backends de almacenamiento.

**Solución**: Interfaz abstracta con implementaciones intercambiables.

```python
class StorageProvider(ABC):
    @abstractmethod
    async def append_events_batch(...): ...

class LocalStorageProvider(StorageProvider): ...
class MinIOStorageProvider(StorageProvider): ...
class SupabaseStorageProvider(StorageProvider): ...

# Factory
def get_storage_provider(provider: str, **kwargs) -> StorageProvider:
    if provider == "local":
        return LocalStorageProvider(...)
    elif provider == "minio":
        return MinIOStorageProvider(...)
    elif provider == "supabase":
        return SupabaseStorageProvider(...)
```

### 7.6 Dependency Injection

**Problema**: Acoplamiento fuerte dificulta testing.

**Solución**: Inyectar dependencias en constructores.

```python
class DatalakeConsumer:
    def __init__(self, settings: Settings, storage: StorageProvider):
        self.storage = storage  # Inyectado, no creado internamente

# En tests:
mock_storage = MockStorageProvider()
consumer = DatalakeConsumer(settings, mock_storage)
```

---

## 8. API REST

### 8.1 Autenticación

API Key en header `X-API-Key`:

```python
async def verify_api_key(x_api_key: str = Header(None)):
    if settings.api_key and x_api_key != settings.api_key:
        raise HTTPException(401, "Invalid or missing API key")
```

### 8.2 Endpoints

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| GET | `/health` | Estado del servicio | No |
| GET | `/categories` | Categorías con datos | Sí |
| GET | `/categories/available` | Todas las categorías válidas | Sí |
| GET | `/categories/{cat}/stats` | Estadísticas | Sí |
| GET | `/categories/{cat}/dates` | Fechas disponibles | Sí |
| GET | `/events/{cat}` | Consulta de eventos | Sí |
| GET | `/replay/{cat}` | Replay cronológico | Sí |
| GET | `/dlq` | Eventos fallidos (hoy) | Sí |
| GET | `/dlq/all` | Todos los eventos fallidos | Sí |
| POST | `/dlq/retry/{id}` | Reintentar evento | Sí |
| POST | `/dlq/retry-all` | Reintentar todos | Sí |

### 8.3 Ejemplos de Uso

```bash
# Health check
curl http://localhost:8080/health

# Listar categorías válidas
curl -H "X-API-Key: $KEY" http://localhost:8080/categories/available

# Consultar eventos de usuarios de hoy
curl -H "X-API-Key: $KEY" http://localhost:8080/events/users

# Consultar eventos de una fecha específica
curl -H "X-API-Key: $KEY" "http://localhost:8080/events/users?date=2026-03-25"

# Filtrar por tipo de evento
curl -H "X-API-Key: $KEY" "http://localhost:8080/events/users?event_type=UserRegistered"

# Replay completo de una categoría
curl -H "X-API-Key: $KEY" http://localhost:8080/replay/traces
```

### 8.4 OpenAPI / Swagger

Documentación interactiva disponible en:
- **Swagger UI**: `http://localhost:8080/docs`
- **ReDoc**: `http://localhost:8080/redoc`
- **OpenAPI JSON**: `http://localhost:8080/openapi.json`

---

## 9. Sistema de Mounters

### 9.1 Concepto

Los **Mounters** son componentes que proyectan eventos del Datalake hacia diferentes **datamarts** especializados. Implementan el patrón CQRS (Command Query Responsibility Segregation), donde el Datalake es el almacén de escritura (eventos) y los datamarts son vistas materializadas optimizadas para consultas.

```
                    ┌─────────────────┐
                    │    Datalake     │
                    │  (JSONL files)  │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │  MounterEngine  │
                    │   (Coordinator) │
                    └────────┬────────┘
                             │
            ┌────────────────┼────────────────┐
            │                │                │
   ┌────────▼────────┐ ┌─────▼─────┐ ┌───────▼───────┐
   │ PostgresMounter │ │  Storage  │ │ QdrantMounter │
   │  (Operational)  │ │  Mounter  │ │   (Vectors)   │
   └────────┬────────┘ └─────┬─────┘ └───────┬───────┘
            │                │               │
   ┌────────▼────────┐ ┌─────▼─────┐ ┌───────▼───────┐
   │   PostgreSQL    │ │  S3/MinIO │ │    Qdrant     │
   └─────────────────┘ └───────────┘ └───────────────┘
```

### 9.2 MounterEngine

El **MounterEngine** es el coordinador central que:
- Lee eventos del datalake (archivos JSONL)
- Distribuye eventos a los mounters según categoría
- Soporta tres modos de operación

```python
from src.mounters import MounterEngine, MounterMode

engine = MounterEngine(datalake_path="./data/datalake")
engine.register(postgres_mounter)
engine.register(storage_mounter)

# Modos de operación
await engine.run(MounterMode.REPLAY)   # Reprocesar eventos históricos
await engine.run(MounterMode.LIVE)     # Observar nuevos eventos en tiempo real
await engine.run(MounterMode.REBUILD)  # Borrar y reconstruir desde cero
```

| Modo | Descripción | Uso |
|------|-------------|-----|
| `REPLAY` | Procesa eventos históricos con filtros de fecha | Sincronización inicial, recuperación |
| `LIVE` | Observa nuevos eventos en tiempo real | Producción continua |
| `REBUILD` | Elimina datos y reconstruye desde cero | Cambios de schema, corrección de bugs |

### 9.3 PostgresMounter

Proyecta eventos a PostgreSQL con schemas separados por dominio:

| Schema | Tablas | Categorías |
|--------|--------|------------|
| `identity` | users | users |
| `profiles` | profiles | users (perfil extendido) |
| `studies` | studies, members, invitations | studies |
| `traces` | traces, annotations, sequence_edits | traces |
| `alignments` | alignments, alignment_traces | alignments |
| `billing` | plans, subscriptions | plans, subscriptions |
| `payments` | payments, payment_methods | payments |

#### Handlers

Cada dominio tiene un handler que transforma eventos en operaciones SQL:

```python
class UsersHandler(BaseHandler):
    """Transforma eventos de usuarios en operaciones SQL."""

    async def insert(self, payload: dict) -> None:
        await self._connection.execute(
            "INSERT INTO identity.users (...) VALUES (...)",
            payload.get("id"),
            payload.get("email"),
            ...
        )

    async def update(self, payload: dict) -> None:
        # UPDATE dinámico según campos en payload
        ...

    async def soft_delete(self, payload: dict) -> None:
        await self._connection.execute(
            "UPDATE identity.users SET is_deleted = TRUE ...",
            ...
        )
```

#### Event Mappings

| Evento | Handler | Operación |
|--------|---------|-----------|
| `UserRegistered` | UsersHandler | insert |
| `UserUpdated` | UsersHandler | update |
| `UserDeleted` | UsersHandler | soft_delete |
| `StudyCreated` | StudiesHandler | insert_study |
| `MemberAdded` | StudiesHandler | insert_member |
| `TraceUploaded` | TracesHandler | insert_trace |
| `TraceProcessed` | TracesHandler | update_trace |
| `AlignmentCompleted` | AlignmentsHandler | update_alignment |
| `SubscriptionCreated` | BillingHandler | insert_subscription |

### 9.4 StorageMounter

Almacena archivos de trazas genéticas en S3/MinIO con **chunking** para streaming eficiente.

#### Estructura de Almacenamiento

```
traces/{trace_id}/
├── original.ab1          # Archivo original (AB1, FASTA, etc.)
├── manifest.json         # Índice de chunks
└── chunks/
    ├── chunk_0000.json   # Bases 0-999
    ├── chunk_0001.json   # Bases 1000-1999
    └── ...
```

#### Chunking Strategy

Las trazas genéticas pueden tener miles de bases. El chunking permite:
- **Streaming progresivo**: El frontend puede mostrar datos mientras carga
- **Reducción de memoria**: No es necesario cargar toda la traza
- **Caching eficiente**: Chunks individuales pueden cachearse

```python
class TraceChunker:
    def __init__(self, chunk_size: int = 1000):
        self.chunk_size = chunk_size  # bases por chunk

    def chunk_trace(self, trace_id: str, parsed_data: dict):
        # Genera manifest + chunks
        ...
```

#### Formato de Chunk

```json
{
  "index": 0,
  "start_position": 0,
  "end_position": 1000,
  "bases": "ATCGATCG...",
  "quality_scores": [30, 35, 40, ...],
  "chromatogram": {
    "A": [10, 20, 15, ...],
    "C": [5, 8, 12, ...],
    "G": [3, 6, 9, ...],
    "T": [2, 4, 7, ...]
  }
}
```

#### Manifest

```json
{
  "trace_id": "550e8400-e29b-41d4-a716-446655440000",
  "original_filename": "sample.ab1",
  "format": "AB1",
  "total_bases": 2500,
  "chunk_size": 1000,
  "chunk_count": 3,
  "has_chromatogram": true,
  "has_quality_scores": true,
  "chunks": [
    {"index": 0, "start_position": 0, "end_position": 1000, "filename": "chunk_0000.json"},
    {"index": 1, "start_position": 1000, "end_position": 2000, "filename": "chunk_0001.json"},
    {"index": 2, "start_position": 2000, "end_position": 2500, "filename": "chunk_0002.json"}
  ]
}
```

### 9.5 QdrantMounter

Proyecta embeddings vectoriales pre-calculados a **Qdrant** para búsqueda semántica de secuencias, anotaciones y trazas.

#### Colecciones

| Colección | Vector Size | Distancia | Propósito |
|-----------|-------------|-----------|-----------|
| `geneflow_sequences` | 768 | Cosine | Embeddings de secuencias ADN/ARN |
| `geneflow_annotations` | 1536 | Cosine | Embeddings de anotaciones de texto |
| `geneflow_traces` | 256 | Cosine | Embeddings de metadatos de trazas |

#### Eventos Soportados

| Evento | Categoría | Acción |
|--------|-----------|--------|
| `AISequenceEmbedded` | ai | Upsert en `geneflow_sequences` |
| `AIAnnotationEmbedded` | ai | Upsert en `geneflow_annotations` |
| `AITraceEmbedded` | ai | Upsert en `geneflow_traces` |
| `AIEmbeddingDeleted` | ai | Delete del collection indicado |
| `TraceDeleted` | traces | Delete cascada en los 3 collections |
| `AnnotationDeleted` | traces | Delete en `geneflow_annotations` |

#### Esquema de Eventos

```json
// AISequenceEmbedded
{
  "type": "AISequenceEmbedded",
  "category": "ai",
  "payload": {
    "trace_id": "550e8400-e29b-41d4-a716-446655440000",
    "embedding": [0.1, 0.2, ...],  // 768 dimensiones
    "study_id": "uuid",
    "owner_id": "uuid",
    "sequence_length": 1500,
    "format": "ab1"
  }
}

// AIAnnotationEmbedded
{
  "type": "AIAnnotationEmbedded",
  "category": "ai",
  "payload": {
    "annotation_id": "uuid",
    "embedding": [0.1, 0.2, ...],  // 1536 dimensiones
    "trace_id": "uuid",
    "study_id": "uuid",
    "owner_id": "uuid",
    "text_content": "Primer binding site identified",
    "annotation_type": "note"
  }
}
```

#### Payload en Qdrant

Cada punto almacenado incluye metadatos para filtrado:

```json
{
  "id": "trace-123",
  "vector": [0.1, 0.2, ...],
  "payload": {
    "trace_id": "trace-123",
    "study_id": "study-456",
    "owner_id": "user-789",
    "sequence_length": 1500,
    "format": "ab1"
  }
}
```

#### Cascade Delete

Cuando se elimina una traza (`TraceDeleted`), el mounter:
1. Elimina el punto de `geneflow_sequences` por ID
2. Elimina el punto de `geneflow_traces` por ID
3. Elimina todos los puntos de `geneflow_annotations` donde `trace_id` coincide

#### Configuración

```bash
DATALAKE_QDRANT_URL=http://qdrant:6333
DATALAKE_QDRANT_API_KEY=          # Opcional
DATALAKE_QDRANT_ENABLED=true
```

### 9.6 Factory Functions

```python
from src.mounters import create_engine

# Crear engine con todos los mounters configurados
engine = create_engine(
    datalake_path="./data/datalake",
    postgres_url="postgresql://user:pass@localhost/geneflow",
    storage_endpoint="http://minio:9000",
    storage_access_key="minioadmin",
    storage_secret_key="minioadmin",
)

# Replay de eventos
result = await engine.run(MounterMode.REPLAY)
print(f"Processed: {result['events_processed']}")
print(f"Failed: {result['events_failed']}")
```

---

## 10. CLI

### 10.1 Comandos Disponibles

```bash
# Iniciar servicio (modo por defecto)
datalake serve

# Ejecutar migraciones de base de datos
datalake migrate

# Replay de eventos históricos
datalake replay --from 2026-01-01 --to 2026-03-25
datalake replay --categories users,studies

# Reconstruir datamarts (destructivo)
datalake rebuild --force
datalake rebuild --force --categories traces

# Ver estado de mounters
datalake status
```

### 10.2 Implementación

```python
# src/cli.py
def create_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="datalake")
    subparsers = parser.add_subparsers(dest="command")

    # replay command
    replay = subparsers.add_parser("replay")
    replay.add_argument("--from", dest="from_date")
    replay.add_argument("--to", dest="to_date")
    replay.add_argument("--categories")

    # rebuild command
    rebuild = subparsers.add_parser("rebuild")
    rebuild.add_argument("--force", action="stoREDACTED")

    return parser
```

---

## 11. Testing

### 11.1 Estructura de Tests

```
tests/
├── conftest.py                    # Fixtures compartidos
├── test_config.py                 # Configuración
├── test_models.py                 # Modelos de datos
├── test_bootstrap.py              # Inicialización de componentes
├── test_lifecycle.py              # Orquestación de arranque/parada
├── test_lifecycle_branches.py     # Branch coverage de lifecycle
├── test_storage.py                # StorageProvider local
├── test_storage_minio.py          # MinIOStorageProvider
├── test_storage_supabase.py       # SupabaseStorageProvider
├── test_buffer.py                 # EventBuffer
├── test_wal.py                    # Write-Ahead Log
├── test_deduplication.py          # Deduplicador
├── test_consumer.py               # Redis consumer
├── test_consumer_branches.py      # Branch coverage de consumer
├── test_message_parser.py         # MessageParser
├── test_retry.py                  # RetryHandler
├── test_retry_branches.py         # Branch coverage de retry
├── test_backoff.py                # Cálculo de delays
├── test_api.py                    # API REST
├── test_api_auth.py               # Autenticación API key
├── test_api_middleware.py         # Middleware (correlación, logging)
├── test_api_services.py           # Servicios de API
├── test_utils.py                  # Utilidades comunes
├── test_utils_logging.py          # log_execution decorator
└── mounters/                      # Sistema de mounters
    ├── __init__.py
    ├── test_engine.py             # MounterEngine
    ├── test_chunking.py           # TraceChunker, TraceManifest
    ├── test_chunking_more.py      # Branch coverage de chunking
    ├── test_handlers.py           # Handlers SQL (legacy suite)
    ├── test_storage_mounter.py    # StorageMounter
    ├── test_storage_connection.py # StorageConnection (S3 client)
    ├── test_trace_handler.py      # TraceHandler
    ├── test_profile_photo_handler.py
    ├── test_thumbnail_service.py
    ├── test_metadata_service.py
    ├── test_photo_validator.py
    ├── test_qdrant_mounter.py     # QdrantMounter
    └── postgres/                  # Postgres mounter (granular)
        ├── __init__.py
        ├── test_connection.py
        ├── test_mounter.py
        ├── test_payload_transformer.py
        ├── test_repositories.py
        ├── test_handlers_base.py
        ├── test_handlers_users.py
        ├── test_handlers_profiles.py
        ├── test_handlers_payments.py
        ├── test_handlers_billing.py
        ├── test_handlers_studies.py
        ├── test_handlers_traces.py
        └── test_handlers_alignments.py
```

### 11.2 Fixtures Principales

```python
@pytest.fixture
def temp_dir() -> Path:
    """Directorio temporal para tests."""
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)

@pytest_asyncio.fixture
async def buffer(temp_dir: Path) -> EventBuffer:
    """Buffer configurado para tests."""
    buf = EventBuffer(
        flush_callback=mock_callback,
        max_size=3,  # Bajo para testing
        flush_interval=0.5,
        wal_path=str(temp_dir / "wal"),
    )
    await buf.start()
    yield buf
    await buf.stop()
```

### 11.3 Cobertura de Tests

La cobertura se mide de forma separada para **líneas** y **ramas** (branches). El gate
interno exige `>=90%` líneas y `>=85%` ramas; ambos se aplican en CI mediante
`scripts/coverage_summary.py`.

**Resultado actual:**

| Métrica | Cobertura | Objetivo | Estado |
|---------|-----------|----------|--------|
| Líneas | 95.84% (2693/2810) | ≥90% | OK |
| Ramas | 91.77% (457/498) | ≥85% | OK |
| Combinada | 95.22% | — | OK |
| Tests | 634 passed, 1 skipped | — | OK |

**Distribución por dominio:**

| Área | Archivos de test | Foco |
|------|------------------|------|
| Configuración / modelos | `test_config`, `test_models` | Defaults, env override, serialización |
| Storage backends | `test_storage`, `test_storage_minio`, `test_storage_supabase` | CRUD, listing, stats, health, error paths |
| Buffer / WAL | `test_buffer`, `test_wal` | Add, flush, recovery |
| Consumer / dedup | `test_consumer`, `test_consumer_branches`, `test_deduplication`, `test_message_parser` | Parsing, dedup, branches |
| Retry / DLQ | `test_retry`, `test_retry_branches`, `test_backoff` | Backoff, max retries, DLQ |
| Lifecycle / bootstrap | `test_lifecycle`, `test_lifecycle_branches`, `test_bootstrap` | Arranque/parada ordenados |
| API | `test_api`, `test_api_auth`, `test_api_middleware`, `test_api_services` | Endpoints, auth, middleware |
| Utils | `test_utils`, `test_utils_logging` | Decorators, helpers |
| Mounters / engine | `test_engine` | REPLAY, LIVE, REBUILD |
| Mounters / chunking | `test_chunking`, `test_chunking_more` | TraceChunker, manifest |
| Mounters / postgres | `mounters/postgres/test_*` (13) | Connection, mounter, handlers, repos, transformer |
| Mounters / storage | `test_storage_mounter`, `test_storage_connection`, `test_trace_handler`, `test_profile_photo_handler`, `test_thumbnail_service`, `test_metadata_service`, `test_photo_validator` | S3 ops, handlers, servicios |
| Mounters / qdrant | `test_qdrant_mounter` | Upsert, delete, cascade, rebuild |

### 11.4 Ejecución

```bash
# Ejecutar todos los tests
uv run pytest

# Con cobertura de líneas y ramas
uv run pytest --cov=src --cov-branch --cov-report=term-missing

# Generar coverage.json (consumido por coverage_summary.py)
uv run pytest --cov=src --cov-branch --cov-report=json

# Aplicar gates locales
uv run python scripts/coverage_summary.py --min-line 90 --min-branch 85

# Tests por dominio
uv run pytest tests/test_api.py -v
uv run pytest tests/mounters/ -v
uv run pytest tests/mounters/postgres/ -v
```

---

## 11.5 Pipeline de Calidad (CI/CD)

El workflow `.github/workflows/ci.yml` ejecuta cinco jobs:

| Job | Comando | Bloqueante |
|-----|---------|------------|
| `lint` | `ruff check src/ tests/` | Sí |
| `format` | `ruff format --check src/ tests/` | Sí |
| `typecheck` | `mypy src/` | No (baseline laxo, `continue-on-error: true`) |
| `test` | `pytest --cov=src --cov-branch --cov-report=xml --cov-report=json` + `coverage_summary.py --min-line 80 --min-branch 70` | Sí (gates CI) |
| `security` | `pip-audit` | Sí |

`lint`, `format` y `typecheck` corren en paralelo; `test` y `security` dependen de los
tres anteriores. Tras `test` se publican como artefactos `coverage.xml`, `coverage.json`
y se sube a Codecov.

### Coverage gates

El script `scripts/coverage_summary.py`:

1. Lee `coverage.json` y calcula líneas, ramas y combinada.
2. Genera una tabla Markdown en `$GITHUB_STEP_SUMMARY`.
3. Aplica `--min-line` y `--min-branch`; sale con código 1 si alguno falla.

Esto separa la métrica CI (más laxa, 80/70) del objetivo interno (90/85), permitiendo
endurecer progresivamente el gate sin romper releases en caliente.

### Convención de commits

- Conventional Commits con scope por dominio: `feat(api)`, `refactor(postgres)`,
  `test(mounters/storage)`, `chore(ci)`, etc.
- Cuerpo multi-línea con `-m "..." -m "..."` para describir el "porqué".
- Sin co-author.
- Cada commit cubre un único dominio coherente para facilitar `git blame` y revert
  selectivo.

### Política de comentarios

El código tiene que ser autoexplicativo. Solo se conservan:

- **Docstrings** en módulos, clases y funciones públicas.
- Comentarios que explican el **"porqué"** (decisiones de diseño, restricciones
  externas, edge cases no obvios). Ejemplo: `# Chromatogram typically has 10x more
  data points than bases` o `# Required for S3-compatible storage`.
- Pragmas (`# pragma: no cover`, `# type: ignore`, etc.).

Se eliminan todos los comentarios "qué" que repiten lo que la línea siguiente ya dice.

---

## 12. Despliegue

### 12.1 Local (Desarrollo)

```bash
# Requisitos: Redis corriendo en localhost:6379

# Instalar dependencias
uv sync

# Configurar
cp .env.example .env
# Editar .env con valores

# Ejecutar
uv run datalake
```

### 12.2 Docker

```dockerfile
FROM python:3.12-slim

WORKDIR /app

COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv

COPY pyproject.toml uv.lock ./
RUN uv sync --frozen --no-dev

COPY src/ ./src/

EXPOSE 8080

CMD ["uv", "run", "datalake"]
```

```bash
docker build -t geneflow-datalake .
docker run -p 8080:8080 -e DATALAKE_REDIS_URL=redis://host:6379 geneflow-datalake
```

### 12.3 Docker Compose

```yaml
version: '3.8'

services:
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 5

  qdrant:
    image: qdrant/qdrant:latest
    ports:
      - "6333:6333"
    volumes:
      - qdrant-data:/qdrant/storage
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:6333/health"]
      interval: 10s
      timeout: 5s
      retries: 5

  datalake:
    build: .
    ports:
      - "8080:8080"
    environment:
      DATALAKE_REDIS_URL: redis://redis:6379
      DATALAKE_STORAGE_PROVIDER: local
      DATALAKE_API_KEY: ${DATALAKE_API_KEY}
      DATALAKE_QDRANT_URL: http://qdrant:6333
      DATALAKE_QDRANT_ENABLED: "true"
    volumes:
      - datalake-data:/app/data
    depends_on:
      redis:
        condition: service_healthy
      qdrant:
        condition: service_healthy

volumes:
  datalake-data:
  qdrant-data:
```

### 12.4 Variables de Entorno

| Variable | Descripción | Default |
|----------|-------------|---------|
| `DATALAKE_REDIS_URL` | URL de Redis | `redis://redis:6379` |
| `DATALAKE_STORAGE_PROVIDER` | `local`, `minio`, `supabase` | `local` |
| `DATALAKE_API_KEY` | API key para autenticación | (vacío) |
| `DATALAKE_BUFFER_MAX_SIZE` | Eventos antes de flush | `1000` |
| `DATALAKE_MINIO_ENDPOINT` | Endpoint MinIO | - |
| `DATALAKE_MINIO_ACCESS_KEY` | Access key MinIO | - |
| `DATALAKE_MINIO_SECRET_KEY` | Secret key MinIO | - |
| `DATALAKE_SUPABASE_URL` | URL de Supabase | - |
| `DATALAKE_SUPABASE_KEY` | Service key Supabase | - |
| `DATALAKE_QDRANT_URL` | URL de Qdrant | `http://localhost:6333` |
| `DATALAKE_QDRANT_API_KEY` | API key Qdrant | (vacío) |
| `DATALAKE_QDRANT_ENABLED` | Activar QdrantMounter | `false` |

---

## 13. Conclusiones

### 13.1 Objetivos Alcanzados

✅ **Consumo de eventos**: Sistema robusto que procesa eventos de 9 streams de Redis

✅ **Persistencia durable**: WAL garantiza no pérdida de datos ante fallos

✅ **Deduplicación**: Filtrado efectivo de duplicados con ventana de 24h

✅ **API REST**: Endpoints completos para consulta, replay y gestión de DLQ

✅ **Multi-storage**: Soporte para filesystem local, MinIO y Supabase

✅ **Observabilidad**: Logging estructurado y métricas de health

✅ **Sistema de Mounters**: Proyección de eventos a datamarts especializados

✅ **PostgresMounter**: 5 schemas con handlers para todos los dominios

✅ **StorageMounter**: Chunking de trazas para streaming eficiente

✅ **QdrantMounter**: Embeddings vectoriales para búsqueda semántica

✅ **CLI**: Comandos para replay, rebuild, migrate y status

✅ **Testing**: Suite de 634 tests automatizados (95.84% líneas / 91.77% ramas)

✅ **CI/CD**: Pipeline con jobs paralelos (lint, format, typecheck, test, security) y gates de cobertura aplicados automáticamente

### 13.2 Decisiones Técnicas Clave

| Decisión | Justificación |
|----------|---------------|
| **Python + asyncio** | I/O-bound workload, ecosistema maduro |
| **JSONL format** | Append-only, línea por evento, fácil de procesar |
| **WAL pattern** | Durabilidad sin sacrificar rendimiento |
| **Consumer Groups** | Escalabilidad horizontal nativa |
| **Strategy pattern** | Flexibilidad de storage sin cambiar código |

### 13.3 Limitaciones Conocidas

- Deduplicación en memoria: limitada a ~100k eventos (24h window)
- Ordenamiento solo dentro de categoría
- Replay carga todos los eventos en memoria

### 13.4 Trabajo Futuro

- [x] ~~Sistema de Mounters para proyección a datamarts~~
- [x] ~~PostgresMounter con schemas por dominio~~
- [x] ~~StorageMounter con chunking de trazas~~
- [x] ~~QdrantMounter para embeddings vectoriales~~
- [x] ~~CLI para operaciones de mounters~~
- [ ] Test de integración con servicios .NET
- [ ] Compresión de archivos JSONL antiguos
- [ ] Métricas Prometheus
- [ ] Sharding por fecha para replay eficiente

---

## Anexo A: Estructura del Proyecto

```
geneflow-datalake/
├── src/
│   ├── __init__.py
│   ├── main.py                       # Entry point
│   ├── bootstrap.py                  # Inicialización de componentes
│   ├── lifecycle.py                  # Orquestación arranque/parada
│   ├── constants.py
│   ├── config/
│   │   ├── settings.py               # pydantic-settings
│   │   └── constants.py
│   ├── models/
│   │   ├── events.py                 # EventBusMessage, DatalakeEvent
│   │   └── retry.py
│   ├── consumer/
│   │   ├── consumer.py               # DatalakeConsumer
│   │   ├── message_parser.py
│   │   └── deduplication.py
│   ├── buffer/
│   │   ├── event_buffer.py           # Buffer principal
│   │   └── wal.py                    # Write-Ahead Log
│   ├── retry/
│   │   ├── retry_handler.py
│   │   ├── backoff.py
│   │   └── dlq.py
│   ├── storage/
│   │   ├── storage.py                # Interfaz abstracta
│   │   ├── local.py
│   │   ├── minio.py
│   │   └── supabase.py
│   ├── api/
│   │   ├── app.py                    # Factory FastAPI
│   │   ├── api.py
│   │   ├── auth/                     # api_key, dependencies
│   │   ├── middleware/               # correlation, logging
│   │   ├── exceptions/               # handlers
│   │   ├── routes/                   # categories, dlq, events, health, replay
│   │   ├── services/                 # category_stats, dlq, events_query
│   │   └── responses/                # DTOs por endpoint
│   ├── utils/
│   │   ├── logging.py                # log_execution decorator
│   │   ├── dates.py
│   │   └── files.py
│   └── mounters/
│       ├── base.py
│       ├── engine.py
│       ├── setup.py
│       ├── postgres/
│       │   ├── connection.py
│       │   ├── mounter.py
│       │   ├── schemas/              # users, profiles, studies, traces,
│       │   │                         # alignments, billing, payments
│       │   ├── handlers/             # idem, todos + base.py
│       │   ├── repositories/         # base, user_repository
│       │   └── transformers/
│       │       └── payload_transformer.py
│       ├── storage/
│       │   ├── connection.py         # StorageConnection (S3)
│       │   ├── chunking.py           # TraceChunker
│       │   ├── mounter.py
│       │   ├── handlers/             # trace_handler, profile_photo_handler
│       │   ├── services/             # thumbnail, metadata
│       │   └── validators/           # photo_validator
│       └── qdrant/
│           ├── connection.py
│           ├── collections.py
│           └── mounter.py
├── tests/
│   ├── conftest.py
│   ├── test_*.py                     # ~25 archivos de tests root
│   └── mounters/
│       ├── test_*.py                 # Engine, chunking, storage, qdrant
│       └── postgres/                 # 13 archivos granulares
├── scripts/
│   └── coverage_summary.py           # Gates de cobertura
├── docs/
│   ├── MEMORIA_TFT.md
│   └── MOUNTERS.md
├── .github/
│   └── workflows/
│       └── ci.yml                    # 5 jobs paralelos
├── data/                             # Datos (gitignored)
│   ├── datalake/
│   ├── wal/
│   └── dlq/
├── .env / .env.example
├── .gitignore
├── Dockerfile / docker-compose.yml
├── pyproject.toml                    # Dependencias + coverage + mypy
├── uv.lock
└── README.md
```

---

## Anexo B: Formato de Eventos

### Evento en Redis Stream

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "type": "UserRegistered",
  "category": "users",
  "timestamp": 1711357800000,
  "data": "{\"userId\":\"user-123\",\"email\":\"scientist@lab.org\"}",
  "source": "users-service",
  "version": "1.0",
  "correlationId": "req-abc-123"
}
```

### Evento en JSONL (persistido)

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "type": "UserRegistered",
  "category": "users",
  "timestamp": "2026-03-25T10:30:00.000Z",
  "streamId": "1711357800000-0",
  "data": {
    "userId": "user-123",
    "email": "scientist@lab.org"
  },
  "receivedAt": "2026-03-25T10:30:00.150Z"
}
```

### Evento en DLQ

```json
{
  "eventId": "failed-event-123",
  "category": "users",
  "date": "2026-03-25",
  "eventLine": "{...}",
  "retryCount": 5,
  "lastError": "Storage timeout after 30s",
  "createdAt": "2026-03-25T10:30:00.000Z",
  "movedToDlqAt": "2026-03-25T10:35:00.000Z"
}
```

---

*Documento generado para la memoria del Trabajo de Fin de Título*
*GeneFlow Platform - 2026*

# Contexto del Ecosistema GeneFlow

Documento de contexto para el desarrollo de módulos en la plataforma GeneFlow.

---

## 1. Arquitectura General

GeneFlow es una plataforma de análisis bioinformático compuesta por **4 módulos** que se comunican mediante **Redis Streams** (Event-Driven Architecture):

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           GENEFLOW PLATFORM                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────┐ │
│   │   Backend    │    │   Analysis   │    │   Datalake   │    │    AI    │ │
│   │   (API)      │    │   (Workers)  │    │  (Storage)   │    │ (Models) │ │
│   └──────┬───────┘    └──────┬───────┘    └──────┬───────┘    └────┬─────┘ │
│          │                   │                   │                  │       │
│          └───────────────────┴───────────────────┴──────────────────┘       │
│                                      │                                       │
│                                      ▼                                       │
│                          ┌───────────────────────┐                          │
│                          │     REDIS STREAMS     │                          │
│                          │     (Event Bus)       │                          │
│                          └───────────────────────┘                          │
│                                      │                                       │
│                    ┌─────────────────┼─────────────────┐                    │
│                    ▼                 ▼                 ▼                    │
│              ┌──────────┐      ┌──────────┐      ┌──────────┐              │
│              │ PostgreSQL│      │  Qdrant  │      │  MinIO   │              │
│              │ (OLTP)   │      │ (Vector) │      │  (Files) │              │
│              └──────────┘      └──────────┘      └──────────┘              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Los Módulos del Sistema

### 2.1 geneflow-backend

**Responsabilidad**: API REST principal, autenticación, gestión de usuarios/estudios/trazas.

**Interacción con el bus**:
- **Emite eventos** cuando ocurren acciones de usuario (crear estudio, subir traza, etc.)
- **Encola jobs** en streams de trabajo para los workers

**Streams que usa**:
```
geneflow:jobs:traces       → Encola trabajos de procesamiento de trazas
geneflow:jobs:alignments   → Encola trabajos de alineamiento
geneflow:jobs:analysis     → Encola trabajos de análisis adicionales
```

---

### 2.2 geneflow-analysis

**Responsabilidad**: Workers que procesan trazas de secuenciación, ejecutan análisis bioinformáticos y alineamientos.

**Componentes principales**:
- **TraceWorker**: Parsea archivos AB1/SCF/FASTQ/FASTA, calcula métricas de calidad
- **AlignmentWorker**: Alineamiento pairwise/múltiple, genera consenso, detecta variantes
- **AnalysisWorker**: Análisis avanzados (trimming, heterocigotos, ORFs, restricción)

**Interacción con el bus**:

| Consume de | Evento procesado | Emite a | Evento emitido |
|------------|------------------|---------|----------------|
| `geneflow:jobs:traces` | Job de traza | `geneflow:events:traces` | `TraceProcessedEvent` / `TraceFailedEvent` |
| `geneflow:jobs:alignments` | Job de alineamiento | `geneflow:events:alignments` | `AlignmentCompletedEvent` / `AlignmentFailedEvent` |
| `geneflow:jobs:analysis` | Job de análisis | `geneflow:events:analysis` | `AnalysisCompletedEvent` / `AnalysisFailedEvent` |

**Payload de `TraceProcessedEvent`** (evento clave para AI):
```json
{
  "trace_id": "tr-123",
  "study_id": "st-456",
  "owner_id": "user-789",
  "sequence": "ATGCATGC...",
  "quality": [35, 38, 40, ...],
  "qualityMetrics": {
    "meanQuality": 35.5,
    "q20Percentage": 92.3,
    "q30Percentage": 78.1,
    "gcContent": 48.5,
    "length": 850
  },
  "format": "ab1",
  "fileName": "sample.ab1"
}
```

---

### 2.3 geneflow-datalake

**Responsabilidad**: Event sourcing, persistencia de eventos, proyección a bases de datos externas.

**Componentes principales**:
- **Consumer**: Consume eventos de todas las categorías, persiste en JSONL
- **Buffer + WAL**: Acumula eventos con write-ahead log para durabilidad
- **Deduplicator**: Evita duplicados (24h TTL)
- **Mounters**: Proyectan eventos a bases de datos externas

**Mounters disponibles**:

| Mounter | Destino | Categorías que procesa |
|---------|---------|------------------------|
| `QdrantMounter` | BD Vectorial | `ai`, `traces` |
| `PostgresMounter` | BD Relacional | `users`, `studies`, `traces`, `alignments`, `billing` |
| `StorageMounter` | S3/MinIO | `traces` (archivos raw) |

**API REST del Datalake**:
```
GET  /health                        # Health check (sin auth)
GET  /events/{category}             # Query eventos
GET  /replay/{category}             # Replay para reconstrucción
GET  /categories/{category}/stats   # Estadísticas
POST /dlq/retry/{event_id}          # Reintentar evento fallido
```

---

### 2.4 geneflow-ai (en desarrollo)

**Responsabilidad**: Análisis con modelos de IA, generación de embeddings, búsqueda semántica.

**Estructura actual**:
```
src/
├── main.py              # Entry point, AIService
├── config.py            # Settings (prefijo AI_)
├── api.py               # REST API
├── eventbus/
│   ├── consumer.py      # Consume eventos de traces/alignments
│   ├── publisher.py     # Publica eventos de AI
│   └── events.py        # Definición de eventos AI
├── services/
│   ├── sequence_analyzer.py
│   ├── quality_predictor.py
│   ├── mutation_detector.py
│   └── sequence_annotator.py
└── models/
    ├── requests.py
    └── predictions.py
```

**Interacción actual con el bus**:
- **Consume**: `geneflow:events:traces`, `geneflow:events:alignments`
- **Escucha**: `TraceProcessedEvent` para trigger automático
- **Emite a**: `geneflow:events:ai`

---

## 3. Sistema de Event Bus (Redis Streams)

### 3.1 Convención de Streams

```
JOBS (entrada para workers):
geneflow:jobs:{domain}        → geneflow:jobs:traces
                              → geneflow:jobs:alignments
                              → geneflow:jobs:analysis

EVENTS (salida de todos los servicios):
geneflow:events:{category}    → geneflow:events:users
                              → geneflow:events:studies
                              → geneflow:events:traces
                              → geneflow:events:alignments
                              → geneflow:events:ai
                              → geneflow:events:system
```

### 3.2 Categorías de Eventos

| Categoría | Descripción | Productor principal |
|-----------|-------------|---------------------|
| `users` | Registro, login, actualización de usuarios | Backend |
| `studies` | Creación, actualización de estudios | Backend |
| `traces` | Procesamiento de trazas | Analysis Workers |
| `alignments` | Resultados de alineamiento | Analysis Workers |
| `subscriptions` | Cambios de suscripción | Backend |
| `plans` | Cambios de plan | Backend |
| `ai` | Resultados de análisis AI | AI Service |
| `blast` | Resultados de búsquedas BLAST | (futuro) |
| `system` | Eventos de sistema (health, startup) | Todos |

### 3.3 Estructura de Mensajes

Todos los eventos siguen esta estructura base:

```python
{
    "eventId": "uuid-v4",           # ID único
    "type": "TraceProcessedEvent",  # Tipo del evento
    "category": "traces",           # Categoría
    "timestamp": 1711357800000,     # Epoch ms
    "data": "{...}",                # JSON con payload
    "source": "geneflow-analysis",  # Servicio origen
    "version": "1.0",               # Versión del schema
    "correlationId": "uuid"         # Para trazabilidad (opcional)
}
```

### 3.4 Consumer Groups

Cada servicio usa su propio consumer group para escalado horizontal:

```
geneflow-analysis  → consumer group: "geneflow-workers"
geneflow-datalake  → consumer group: "datalake-consumers"
geneflow-ai        → consumer group: "ai-consumers"
```

---

## 4. Mounter de BD Vectorial (Qdrant)

El QdrantMounter proyecta embeddings desde eventos a Qdrant para búsqueda semántica.

### 4.1 Colecciones

| Colección | Vector Size | Uso |
|-----------|-------------|-----|
| `geneflow_sequences` | 768 | Embeddings de secuencias genéticas |
| `geneflow_annotations` | 1536 | Embeddings de anotaciones textuales |
| `geneflow_traces` | 256 | Embeddings de metadata de traces |

### 4.2 Eventos que Consume

| Evento | Categoría | Acción |
|--------|-----------|--------|
| `AISequenceEmbedded` | `ai` | Upsert en `geneflow_sequences` |
| `AIAnnotationEmbedded` | `ai` | Upsert en `geneflow_annotations` |
| `AITraceEmbedded` | `ai` | Upsert en `geneflow_traces` |
| `AIEmbeddingDeleted` | `ai` | Delete por ID |
| `TraceDeleted` | `traces` | Cascade delete en todas las colecciones |

### 4.3 Payloads Esperados

**`AISequenceEmbedded`**:
```json
{
  "trace_id": "tr-456",
  "embedding": [0.1, 0.2, ...],
  "study_id": "st-789",
  "owner_id": "user-001",
  "sequence_length": 1500,
  "format": "ab1"
}
```

**`AIAnnotationEmbedded`**:
```json
{
  "annotation_id": "ann-123",
  "embedding": [0.1, 0.2, ...],
  "trace_id": "tr-456",
  "text_content": "Gene XYZ coding region"
}
```

### 4.4 Cómo Emitir Eventos para Qdrant

Para que el Datalake persista embeddings en Qdrant, se debe emitir un evento al stream `geneflow:events:ai`:

```python
import json
import time
from uuid import uuid4

await redis.xadd(
    "geneflow:events:ai",  # Nota: sin "datalake" en el prefijo
    {
        "eventId": str(uuid4()),
        "type": "AISequenceEmbedded",
        "category": "ai",
        "timestamp": int(time.time() * 1000),
        "data": json.dumps({
            "trace_id": "tr-456",
            "embedding": embedding_vector,  # list[float]
            "study_id": "st-789",
            "owner_id": "user-001",
            "sequence_length": 850,
            "format": "ab1",
        }),
        "source": "geneflow-ai",
        "version": "1.0",
    }
)
```

---

## 5. Convenciones de Desarrollo

### 5.1 Naming Conventions

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Archivos | `snake_case.py` | `sequence_analyzer.py` |
| Clases | `PascalCase` | `SequenceAnalyzer` |
| Funciones | `snake_case` | `analyze_sequence()` |
| Métodos privados | `_snake_case` | `_process_batch()` |
| Constantes | `UPPER_SNAKE_CASE` | `MAX_SEQUENCE_LENGTH` |
| Variables de entorno | `PREFIX_UPPER_SNAKE` | `AI_REDIS_URL` |
| Streams Redis | `kebab:case:lower` | `geneflow:events:ai` |
| Rutas API | `kebab-case` | `/analyze-sequence` |
| **Atributos de payload** | `camelCase` | `traceId`, `studyId` |

### 5.2 Prefijos de Variables de Entorno

| Módulo | Prefijo |
|--------|---------|
| geneflow-backend | `BACKEND_` |
| geneflow-analysis | `WORKER_` |
| geneflow-datalake | `DATALAKE_` |
| geneflow-ai | `AI_` |

### 5.3 Patrones de Arquitectura

**Ciclo de vida de componentes**:
```python
class Component:
    async def start(self) -> None:
        """Inicializar recursos"""

    async def stop(self) -> None:
        """Graceful shutdown"""

    async def health_check(self) -> bool:
        """Verificar salud"""

    @property
    def metrics(self) -> dict:
        """Exponer métricas"""
```

**Dependency Injection**:
```python
class AIService:
    def __init__(
        self,
        settings: Settings,
        redis_client: redis.Redis,      # Inyectado
        analyzer: SequenceAnalyzer,     # Inyectado
    ):
        ...
```

**Factory Pattern**:
```python
def get_embedding_model(model_type: str) -> EmbeddingModel:
    models = {
        "sequence": SequenceEmbedder,
        "annotation": AnnotationEmbedder,
    }
    return models[model_type]()
```

### 5.4 Logging Estructurado

Usar `structlog` con JSON output:

```python
import structlog

logger = structlog.get_logger()

logger.info(
    "embedding_generated",
    trace_id="tr-123",
    vector_size=768,
    duration_ms=150,
)
```

### 5.5 Configuración con Pydantic Settings

```python
from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    redis_url: str = "redis://localhost:6379"
    api_port: int = 8090

    class Config:
        env_prefix = "AI_"  # Variables: AI_REDIS_URL, AI_API_PORT
```

### 5.6 API REST

- Endpoint `/health` siempre público (sin auth)
- Autenticación via header `X-API-Key`
- Response models con Pydantic

```python
@app.get("/health")
async def health():
    return {"status": "healthy"}

@app.post("/analyze", dependencies=[Depends(verify_api_key)])
async def analyze(request: AnalyzeRequest) -> AnalyzeResponse:
    ...
```

### 5.7 Testing

- Framework: `pytest` + `pytest-asyncio`
- Fixtures en `tests/conftest.py`
- Naming: `class TestAnalyzer`, `async def test_analyze_sequence()`

### 5.8 Git Commits

```
feat(embeddings): add sequence embedding generation
fix(consumer): handle connection timeout
test(analyzer): add unit tests for quality prediction
docs(api): update endpoint documentation
```

### 5.9 Docker

```dockerfile
FROM python:3.12-slim

WORKDIR /app

RUN pip install uv
COPY pyproject.toml uv.lock ./
RUN uv sync --frozen --no-dev

COPY src/ ./src/

RUN useradd -m appuser
USER appuser

HEALTHCHECK CMD curl -f http://localhost:8090/health || exit 1

CMD ["uv", "run", "python", "-m", "src.main"]
```

---

## 6. Flujo de Datos Típico

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Backend   │     │  Analysis   │     │     AI      │     │  Datalake   │
│             │     │   Workers   │     │   Service   │     │             │
└──────┬──────┘     └──────┬──────┘     └──────┬──────┘     └──────┬──────┘
       │                   │                   │                   │
       │ 1. Encola job     │                   │                   │
       │──────────────────►│                   │                   │
       │                   │                   │                   │
       │                   │ 2. Procesa traza  │                   │
       │                   │ y emite evento    │                   │
       │                   │───────────────────┼──────────────────►│
       │                   │                   │                   │
       │                   │                   │◄──────────────────│
       │                   │                   │ 3. Recibe         │
       │                   │                   │ TraceProcessedEvent
       │                   │                   │                   │
       │                   │                   │ 4. Genera         │
       │                   │                   │ embeddings        │
       │                   │                   │                   │
       │                   │                   │ 5. Emite          │
       │                   │                   │ AISequenceEmbedded│
       │                   │                   │──────────────────►│
       │                   │                   │                   │
       │                   │                   │                   │ 6. Persiste
       │                   │                   │                   │ en Qdrant
       │                   │                   │                   │
```

---

## 7. Bases de Datos Compartidas

| BD | Puerto | Usuarios |
|----|--------|----------|
| Redis | 6379 | Todos (event bus) |
| PostgreSQL | 5432 | Backend, Datalake (mounter) |
| Qdrant | 6333 | AI (búsqueda), Datalake (mounter) |
| MinIO | 9000 | Backend (upload), Analysis (download), Datalake (mounter) |

---

## 8. Resumen de Integraciones

Para que un nuevo módulo se integre correctamente:

1. **Consumir eventos**: Usar `XREADGROUP` con consumer group propio
2. **Emitir eventos**: Usar `XADD` al stream correspondiente con estructura estándar
3. **Health check**: Endpoint `/health` público
4. **Configuración**: Pydantic Settings con prefijo de env único
5. **Logging**: structlog con JSON
6. **Ciclo de vida**: Implementar `start()`, `stop()`, `health_check()`

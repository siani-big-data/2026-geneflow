# Plan de Desarrollo: GeneFlow AI Module

## Resumen Ejecutivo

Este documento detalla el plan completo para desarrollar el módulo GeneFlow AI desde cero, siguiendo las convenciones establecidas del ecosistema GeneFlow.

---

## Estructura Final del Proyecto

```
geneflow-ai/
├── src/
│   ├── __init__.py
│   ├── main.py                    # Entry point, AIService
│   ├── api.py                     # REST API principal
│   ├── config.py                  # Settings (prefijo AI_)
│   ├── models.py                  # Modelos de dominio (dataclasses)
│   │
│   ├── eventbus/                  # Integración Event Bus
│   │   ├── __init__.py
│   │   ├── consumer.py            # Consume eventos de traces/alignments
│   │   ├── publisher.py           # Publica eventos AI
│   │   └── events.py              # Definición de eventos AI
│   │
│   ├── orchestrator/              # AI Orchestrator
│   │   ├── __init__.py
│   │   ├── orchestrator.py        # Coordinador principal
│   │   ├── pipeline.py            # Pipeline de análisis
│   │   └── context.py             # Contexto de análisis
│   │
│   ├── copilot/                   # Copiloto Conversacional (Claude API)
│   │   ├── __init__.py
│   │   ├── client.py              # Cliente Claude API
│   │   ├── chat.py                # Manejo de conversaciones
│   │   ├── prompts.py             # Plantillas de prompts
│   │   └── reports.py             # Generación de reportes
│   │
│   ├── blast/                     # Integración NCBI BLAST
│   │   ├── __init__.py
│   │   ├── client.py              # Cliente NCBI BLAST API
│   │   ├── parser.py              # Parser de resultados
│   │   └── models.py              # Modelos BLAST
│   │
│   ├── external/                  # APIs Externas (Fase 2)
│   │   ├── __init__.py
│   │   ├── base.py                # Cliente base abstracto
│   │   ├── ensembl.py             # Ensembl REST API
│   │   ├── clinvar.py             # ClinVar/dbSNP API
│   │   ├── interpro.py            # InterPro/Pfam API
│   │   └── viennarna.py           # ViennaRNA wrapper
│   │
│   ├── analysis/                  # Servicios de Análisis AI
│   │   ├── __init__.py
│   │   ├── quality.py             # Predicción de calidad
│   │   ├── variants.py            # Detección de variantes
│   │   ├── annotations.py         # Anotación automática
│   │   └── clustering.py          # Clustering y filogenética (Fase 4)
│   │
│   ├── ml/                        # Modelos ML Custom (Fase 3)
│   │   ├── __init__.py
│   │   ├── base.py                # Interfaz base para modelos
│   │   ├── quality_predictor.py   # XGBoost para calidad
│   │   ├── artifact_detector.py   # CNN para artefactos
│   │   └── auto_trimmer.py        # Auto-trim inteligente
│   │
│   └── storage/                   # Cache y almacenamiento
│       ├── __init__.py
│       ├── cache.py               # Cache de resultados
│       └── embeddings.py          # Gestión de embeddings (Qdrant)
│
├── tests/
│   ├── __init__.py
│   ├── conftest.py
│   ├── test_api.py
│   ├── test_config.py
│   ├── copilot/
│   │   └── test_chat.py
│   ├── blast/
│   │   └── test_client.py
│   └── orchestrator/
│       └── test_pipeline.py
│
├── docs/
│   ├── geneflow-ecosystem-context.md
│   ├── MODULO_AI.md
│   ├── CONVENTIONS.md
│   └── API.md
│
├── pyproject.toml
├── pytest.ini
├── Dockerfile
├── .env.example
├── .gitignore
└── README.md
```

---

# FASE 0: Preparación y Estructura Base

## Objetivo
Establecer la estructura del proyecto, configuración y scaffolding básico siguiendo las convenciones de GeneFlow.

## Commits

### Commit 0.1: Limpiar proyecto y crear estructura base
```
feat(setup): initialize project structure

- Remove existing mock implementation
- Create directory structure following conventions
- Add .gitignore with standard patterns
- Add .env.example with documented variables
```

**Archivos:**
```
geneflow-ai/
├── src/
│   ├── __init__.py
│   ├── eventbus/
│   │   └── __init__.py
│   ├── orchestrator/
│   │   └── __init__.py
│   ├── copilot/
│   │   └── __init__.py
│   ├── blast/
│   │   └── __init__.py
│   ├── analysis/
│   │   └── __init__.py
│   └── storage/
│       └── __init__.py
├── tests/
│   └── __init__.py
├── docs/
├── .gitignore
└── .env.example
```

**.env.example:**
```bash
# Redis
AI_REDIS_URL=redis://localhost:6379
AI_REDIS_CONSUMER_GROUP=ai-consumers
AI_REDIS_CONSUMER_NAME=ai-1

# API
AI_API_HOST=0.0.0.0
AI_API_PORT=8090
AI_API_KEY=

# CORS
AI_CORS_ORIGINS=["*"]

# Logging
AI_LOG_LEVEL=INFO
AI_LOG_REQUESTS=true

# Claude API (Copilot)
AI_CLAUDE_API_KEY=
AI_CLAUDE_MODEL=claude-sonnet-4-20250514

# NCBI BLAST
AI_BLAST_EMAIL=
AI_BLAST_API_KEY=

# Event Bus
AI_EVENTBUS_STREAM_PREFIX=geneflow:events
AI_EVENTBUS_ENABLED=true
AI_SUBSCRIBED_CATEGORIES=["traces", "alignments"]
```

---

### Commit 0.2: Configuración con pydantic-settings
```
feat(config): add Settings class with pydantic-settings

- Implement Settings with AI_ prefix
- Add sections: Redis, API, CORS, Logging, Claude, BLAST
- Add validation for required fields
```

**src/config.py:**
```python
from typing import Literal
from pydantic import Field
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """GeneFlow AI service configuration."""

    # Redis
    redis_url: str = "redis://localhost:6379"
    redis_consumer_group: str = "ai-consumers"
    redis_consumer_name: str = "ai-1"
    redis_block_ms: int = 5000

    # API
    api_host: str = "0.0.0.0"
    api_port: int = 8090
    api_key: str = ""

    # CORS
    cors_origins: list[str] = ["*"]
    cors_allow_credentials: bool = True
    cors_allow_methods: list[str] = ["*"]
    cors_allow_headers: list[str] = ["*"]

    # Logging
    log_level: str = "INFO"
    log_requests: bool = True

    # Claude API (Copilot)
    claude_api_key: str = ""
    claude_model: str = "claude-sonnet-4-20250514"
    claude_max_tokens: int = 4096

    # NCBI BLAST
    blast_email: str = ""
    blast_api_key: str = ""
    blast_base_url: str = "https://blast.ncbi.nlm.nih.gov/Blast.cgi"

    # Event Bus
    eventbus_stream_prefix: str = "geneflow:events"
    eventbus_enabled: bool = True
    subscribed_categories: list[str] = ["traces", "alignments"]

    # Analysis
    analysis_timeout_seconds: int = 300
    max_sequence_length: int = 100000

    class Config:
        env_prefix = "AI_"
        env_file = ".env"
```

---

### Commit 0.3: Modelos de dominio base
```
feat(models): add domain models with dataclasses

- Add base event models
- Add analysis result models
- Add request/response models
- Follow camelCase convention for attributes
```

**src/models.py:**
```python
from dataclasses import dataclass, field
from datetime import datetime
from enum import Enum
from typing import Optional, Any
from uuid import uuid4


class AnalysisType(str, Enum):
    """Types of AI analysis."""
    QUALITY = "quality_enhanced"
    VARIANTS = "variants"
    ANNOTATION = "annotation"
    BLAST = "blast"
    COPILOT = "copilot"
    CLUSTERING = "clustering"


class AnalysisStatus(str, Enum):
    """Analysis job status."""
    PENDING = "pending"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"


@dataclass
class AnalysisRequest:
    """Request for AI analysis."""
    traceId: str
    studyId: str
    ownerId: str
    sequence: str
    qualityScores: Optional[list[int]] = None
    analysisTypes: list[AnalysisType] = field(default_factory=lambda: [AnalysisType.QUALITY])
    referenceSequence: Optional[str] = None
    organism: Optional[str] = None

    @classmethod
    def from_dict(cls, data: dict) -> "AnalysisRequest":
        return cls(
            traceId=data.get("traceId", ""),
            studyId=data.get("studyId", ""),
            ownerId=data.get("ownerId", ""),
            sequence=data.get("sequence", ""),
            qualityScores=data.get("qualityScores"),
            analysisTypes=[AnalysisType(t) for t in data.get("analysisTypes", ["quality_enhanced"])],
            referenceSequence=data.get("referenceSequence"),
            organism=data.get("organism"),
        )


@dataclass
class QualityResult:
    """Quality prediction result."""
    predictedAccuracy: float
    errorProbability: float
    lowQualityRegions: list[tuple[int, int]]
    suggestedTrimStart: int
    suggestedTrimEnd: int
    confidence: float

    def to_dict(self) -> dict:
        return {
            "predictedAccuracy": self.predictedAccuracy,
            "errorProbability": self.errorProbability,
            "lowQualityRegions": self.lowQualityRegions,
            "suggestedTrimStart": self.suggestedTrimStart,
            "suggestedTrimEnd": self.suggestedTrimEnd,
            "confidence": self.confidence,
        }


@dataclass
class VariantResult:
    """Variant detection result."""
    position: int
    referenceBase: str
    alternateBase: str
    variantType: str  # SNP, insertion, deletion
    clinicalSignificance: Optional[str] = None
    confidence: float = 0.0
    annotation: Optional[str] = None

    def to_dict(self) -> dict:
        return {
            "position": self.position,
            "referenceBase": self.referenceBase,
            "alternateBase": self.alternateBase,
            "variantType": self.variantType,
            "clinicalSignificance": self.clinicalSignificance,
            "confidence": self.confidence,
            "annotation": self.annotation,
        }


@dataclass
class AnnotationResult:
    """Sequence annotation result."""
    start: int
    end: int
    featureType: str
    strand: str
    name: Optional[str] = None
    description: Optional[str] = None
    confidence: float = 0.0
    source: str = "ai"  # ai, blast, ensembl

    def to_dict(self) -> dict:
        return {
            "start": self.start,
            "end": self.end,
            "featureType": self.featureType,
            "strand": self.strand,
            "name": self.name,
            "description": self.description,
            "confidence": self.confidence,
            "source": self.source,
        }


@dataclass
class BlastHit:
    """BLAST search hit."""
    accession: str
    description: str
    score: float
    eValue: float
    identity: float
    queryStart: int
    queryEnd: int
    subjectStart: int
    subjectEnd: int
    organism: Optional[str] = None

    def to_dict(self) -> dict:
        return {
            "accession": self.accession,
            "description": self.description,
            "score": self.score,
            "eValue": self.eValue,
            "identity": self.identity,
            "queryStart": self.queryStart,
            "queryEnd": self.queryEnd,
            "subjectStart": self.subjectStart,
            "subjectEnd": self.subjectEnd,
            "organism": self.organism,
        }


@dataclass
class AnalysisResult:
    """Complete AI analysis result."""
    analysisId: str = field(default_factory=lambda: str(uuid4()))
    traceId: str = ""
    studyId: str = ""
    status: AnalysisStatus = AnalysisStatus.PENDING
    createdAt: datetime = field(default_factory=datetime.utcnow)
    completedAt: Optional[datetime] = None
    processingTimeMs: int = 0

    # Results by type
    quality: Optional[QualityResult] = None
    variants: list[VariantResult] = field(default_factory=list)
    annotations: list[AnnotationResult] = field(default_factory=list)
    blastHits: list[BlastHit] = field(default_factory=list)

    # Copilot
    summary: Optional[str] = None
    recommendations: list[str] = field(default_factory=list)

    # Metadata
    overallConfidence: float = 0.0
    warnings: list[str] = field(default_factory=list)
    errors: list[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        data = {
            "analysisId": self.analysisId,
            "traceId": self.traceId,
            "studyId": self.studyId,
            "status": self.status.value,
            "createdAt": self.createdAt.isoformat(),
            "processingTimeMs": self.processingTimeMs,
            "overallConfidence": self.overallConfidence,
            "warnings": self.warnings,
            "errors": self.errors,
        }

        if self.completedAt:
            data["completedAt"] = self.completedAt.isoformat()
        if self.quality:
            data["quality_enhanced"] = self.quality.to_dict()
        if self.variants:
            data["variants"] = [v.to_dict() for v in self.variants]
        if self.annotations:
            data["annotations"] = [a.to_dict() for a in self.annotations]
        if self.blastHits:
            data["blastHits"] = [h.to_dict() for h in self.blastHits]
        if self.summary:
            data["summary"] = self.summary
        if self.recommendations:
            data["recommendations"] = self.recommendations

        return data
```

---

### Commit 0.4: API REST base con FastAPI
```
feat(api): add base REST API with health endpoint

- Implement AIAPI class following conventions
- Add CORS middleware
- Add logging middleware with correlation ID
- Add exception handlers
- Add /health endpoint (public)
```

**src/api.py** (estructura base, ~200 líneas)

---

### Commit 0.5: Entry point y ciclo de vida
```
feat(main): add service entry point with lifecycle

- Implement AIService class with start/stop
- Configure structlog for JSON logging
- Add graceful shutdown handling
- Add signal handlers
```

**src/main.py**

---

### Commit 0.6: Configuración de tests
```
test(setup): add pytest configuration and fixtures

- Add pytest.ini with asyncio_mode=auto
- Add conftest.py with standard fixtures
- Add test_config.py for settings validation
- Add test_api.py for health endpoint
```

---

### Commit 0.7: pyproject.toml y Dockerfile
```
deploy(docker): add pyproject.toml and Dockerfile

- Configure pyproject.toml with uv
- Add dependencies: fastapi, redis, structlog, httpx, anthropic
- Create Dockerfile following conventions
- Add health check configuration
```

**pyproject.toml:**
```toml
[project]
name = "geneflow-ai"
version = "1.0.0"
description = "GeneFlow AI - Intelligent sequence analysis module"
requires-python = ">=3.12"
dependencies = [
    "fastapi>=0.115.0",
    "uvicorn[standard]>=0.32.0",
    "pydantic>=2.0.0",
    "pydantic-settings>=2.0.0",
    "redis>=5.0.0",
    "structlog>=24.0.0",
    "httpx>=0.27.0",
    "anthropic>=0.40.0",
    "biopython>=1.84",
]

[project.optional-dependencies]
dev = [
    "pytest>=8.0.0",
    "pytest-asyncio>=0.24.0",
    "pytest-cov>=5.0.0",
    "ruff>=0.7.0",
]
ml = [
    "scikit-learn>=1.5.0",
    "xgboost>=2.1.0",
    "torch>=2.4.0",
    "scipy>=1.14.0",
]

[build-system]
requires = ["hatchling"]
build-backend = "hatchling.build"

[tool.pytest.ini_options]
asyncio_mode = "auto"
testpaths = ["tests"]

[tool.ruff]
line-length = 100
target-version = "py312"
```

---

# FASE 1: MVP - Copilot + BLAST + Orchestrator

## Objetivo
Implementar las funcionalidades core del MVP: integración con Claude API para el copiloto conversacional, cliente NCBI BLAST, y el orquestador que coordina los análisis.

---

## 1.1 Event Bus (Consumer/Publisher)

### Commit 1.1.1: Eventos AI
```
feat(eventbus): add AI event definitions

- Add AIEvent base class
- Add AIAnalysisStartedEvent
- Add AIAnalysisCompletedEvent
- Add AIAnalysisFailedEvent
- Add AIEmbeddingGeneratedEvent
```

**src/eventbus/events.py:**
```python
from dataclasses import dataclass, field
from datetime import datetime
from typing import Optional, Any
from uuid import uuid4


@dataclass
class AIEvent:
    """Base class for AI events."""
    eventId: str = field(default_factory=lambda: str(uuid4()))
    occurredAt: datetime = field(default_factory=datetime.utcnow)
    source: str = "geneflow-ai"
    version: str = "1.0"
    correlationId: Optional[str] = None

    @property
    def eventType(self) -> str:
        return self.__class__.__name__

    @property
    def category(self) -> str:
        return "ai"

    def to_dict(self) -> dict[str, Any]:
        return {
            "eventId": self.eventId,
            "occurredAt": self.occurredAt.isoformat(),
            "source": self.source,
            "version": self.version,
            "correlationId": self.correlationId,
        }


@dataclass
class AIAnalysisStartedEvent(AIEvent):
    """Published when AI analysis begins."""
    traceId: str = ""
    studyId: str = ""
    analysisTypes: list[str] = field(default_factory=list)


@dataclass
class AIAnalysisCompletedEvent(AIEvent):
    """Published when AI analysis completes."""
    traceId: str = ""
    studyId: str = ""
    analysisId: str = ""
    overallConfidence: float = 0.0
    processingTimeMs: int = 0
    summary: Optional[str] = None


@dataclass
class AIAnalysisFailedEvent(AIEvent):
    """Published when AI analysis fails."""
    traceId: str = ""
    studyId: str = ""
    errorMessage: str = ""
    errorType: str = ""


@dataclass
class AIBlastCompletedEvent(AIEvent):
    """Published when BLAST search completes."""
    traceId: str = ""
    studyId: str = ""
    hitCount: int = 0
    topHitAccession: Optional[str] = None
    topHitOrganism: Optional[str] = None
```

---

### Commit 1.1.2: Publisher
```
feat(eventbus): add event publisher for Redis Streams

- Implement EventBusPublisher class
- Add publish method with retry logic
- Add metrics tracking
- Follow event structure convention
```

**src/eventbus/publisher.py**

---

### Commit 1.1.3: Consumer
```
feat(eventbus): add event consumer for traces

- Implement EventBusConsumer class
- Add consumer group management
- Handle TraceProcessedEvent trigger
- Add graceful shutdown
```

**src/eventbus/consumer.py**

---

### Commit 1.1.4: Tests Event Bus
```
test(eventbus): add unit tests for publisher and consumer

- Test event serialization
- Test publish with mock Redis
- Test consumer group creation
- Test event handling
```

---

## 1.2 Copilot (Claude API)

### Commit 1.2.1: Cliente Claude API
```
feat(copilot): add Claude API client

- Implement ClaudeClient class with httpx
- Add async message creation
- Add retry logic with exponential backoff
- Add rate limiting handling
- Add token counting utilities
```

**src/copilot/client.py:**
```python
from typing import Optional
import httpx
import structlog
from anthropic import AsyncAnthropic

from ..config import Settings

logger = structlog.get_logger()


class ClaudeClient:
    """Async client for Claude API."""

    def __init__(self, settings: Settings):
        self.settings = settings
        self.client = AsyncAnthropic(api_key=settings.claude_api_key)
        self._requests_made = 0
        self._tokens_used = 0

    @property
    def metrics(self) -> dict:
        return {
            "requestsMade": self._requests_made,
            "tokensUsed": self._tokens_used,
        }

    async def create_message(
        self,
        system_prompt: str,
        user_message: str,
        max_tokens: Optional[int] = None,
    ) -> str:
        """Create a message using Claude API."""
        try:
            response = await self.client.messages.create(
                model=self.settings.claude_model,
                max_tokens=max_tokens or self.settings.claude_max_tokens,
                system=system_prompt,
                messages=[{"role": "user", "content": user_message}],
            )

            self._requests_made += 1
            self._tokens_used += response.usage.input_tokens + response.usage.output_tokens

            return response.content[0].text

        except Exception as e:
            logger.error("claude_api_error", error=str(e))
            raise

    async def health_check(self) -> bool:
        """Check if Claude API is accessible."""
        return bool(self.settings.claude_api_key)
```

---

### Commit 1.2.2: Plantillas de prompts
```
feat(copilot): add prompt templates for analysis

- Add SYSTEM_ANALYST prompt
- Add REPORT_GENERATOR prompt
- Add QA_RESPONDER prompt
- Add template rendering utilities
```

**src/copilot/prompts.py:**
```python
"""Prompt templates for Copilot."""

SYSTEM_ANALYST = """Eres un experto en bioinformática y análisis de secuencias de ADN.
Tu rol es analizar secuencias de Sanger y proporcionar interpretaciones claras.

Contexto del análisis:
- Organismo: {organism}
- Longitud de secuencia: {sequence_length} bp
- Calidad promedio: {avg_quality}

Responde siempre en español, de forma clara y concisa.
Incluye recomendaciones prácticas cuando sea apropiado."""


REPORT_GENERATOR = """Genera un reporte técnico del análisis de secuencia.

Datos del análisis:
{analysis_data}

El reporte debe incluir:
1. Resumen ejecutivo (2-3 oraciones)
2. Métricas de calidad
3. Hallazgos relevantes
4. Recomendaciones

Formato: Markdown estructurado."""


QA_RESPONDER = """Responde la pregunta del usuario sobre el análisis de secuencia.

Contexto del análisis:
{context}

Pregunta del usuario:
{question}

Responde de forma precisa y técnica, pero accesible."""


VARIANT_INTERPRETER = """Interpreta las variantes detectadas en la secuencia.

Variantes encontradas:
{variants}

Secuencia de referencia: {reference}
Organismo: {organism}

Proporciona:
1. Descripción de cada variante
2. Posible impacto funcional
3. Relevancia clínica si aplica"""


def render_prompt(template: str, **kwargs) -> str:
    """Render a prompt template with variables."""
    return template.format(**kwargs)
```

---

### Commit 1.2.3: Chat handler
```
feat(copilot): add chat handler for conversations

- Implement ChatHandler class
- Add conversation context management
- Add history truncation for token limits
- Add specialized analysis methods
```

**src/copilot/chat.py:**
```python
from dataclasses import dataclass, field
from typing import Optional
import structlog

from .client import ClaudeClient
from .prompts import SYSTEM_ANALYST, QA_RESPONDER, render_prompt
from ..models import AnalysisResult
from ..config import Settings

logger = structlog.get_logger()


@dataclass
class Message:
    """Chat message."""
    role: str  # "user" or "assistant"
    content: str


@dataclass
class Conversation:
    """Conversation context."""
    conversationId: str
    traceId: Optional[str] = None
    analysisContext: Optional[dict] = None
    messages: list[Message] = field(default_factory=list)


class ChatHandler:
    """Handles conversational interactions with Copilot."""

    def __init__(self, client: ClaudeClient, settings: Settings):
        self.client = client
        self.settings = settings
        self._conversations: dict[str, Conversation] = {}

    async def ask(
        self,
        conversation_id: str,
        question: str,
        analysis_result: Optional[AnalysisResult] = None,
    ) -> str:
        """Ask a question about an analysis."""
        conversation = self._get_or_create_conversation(conversation_id)

        # Build context
        context = self._build_context(conversation, analysis_result)

        # Render prompt
        prompt = render_prompt(
            QA_RESPONDER,
            context=context,
            question=question,
        )

        # Get response
        response = await self.client.create_message(
            system_prompt=SYSTEM_ANALYST.format(
                organism=analysis_result.organism if analysis_result else "Unknown",
                sequence_length=len(analysis_result.sequence) if analysis_result else 0,
                avg_quality="N/A",
            ),
            user_message=prompt,
        )

        # Update conversation
        conversation.messages.append(Message(role="user", content=question))
        conversation.messages.append(Message(role="assistant", content=response))

        return response

    async def interpret_analysis(self, result: AnalysisResult) -> str:
        """Generate natural language interpretation of analysis."""
        # Implementation
        pass

    def _get_or_create_conversation(self, conversation_id: str) -> Conversation:
        if conversation_id not in self._conversations:
            self._conversations[conversation_id] = Conversation(
                conversationId=conversation_id
            )
        return self._conversations[conversation_id]

    def _build_context(
        self,
        conversation: Conversation,
        analysis_result: Optional[AnalysisResult],
    ) -> str:
        # Build context string from analysis result
        pass
```

---

### Commit 1.2.4: Generador de reportes
```
feat(copilot): add report generator

- Implement ReportGenerator class
- Add markdown report generation
- Add PDF export support (optional)
- Add template customization
```

**src/copilot/reports.py**

---

### Commit 1.2.5: Tests Copilot
```
test(copilot): add unit tests for copilot module

- Test ClaudeClient with mocked API
- Test prompt rendering
- Test ChatHandler conversation flow
- Test ReportGenerator output
```

---

## 1.3 BLAST Integration

### Commit 1.3.1: Cliente NCBI BLAST
```
feat(blast): add NCBI BLAST API client

- Implement BlastClient class
- Add async job submission (PUT)
- Add job status polling (GET)
- Add result retrieval
- Add timeout handling
```

**src/blast/client.py:**
```python
import asyncio
from typing import Optional
import httpx
import structlog

from ..config import Settings
from .models import BlastJob, BlastResult

logger = structlog.get_logger()


class BlastClient:
    """Async client for NCBI BLAST API."""

    def __init__(self, settings: Settings):
        self.settings = settings
        self.base_url = settings.blast_base_url
        self._client = httpx.AsyncClient(timeout=30.0)

    async def submit_search(
        self,
        sequence: str,
        program: str = "blastn",
        database: str = "nt",
    ) -> BlastJob:
        """Submit a BLAST search job."""
        params = {
            "CMD": "Put",
            "PROGRAM": program,
            "DATABASE": database,
            "QUERY": sequence,
            "FORMAT_TYPE": "JSON2",
            "EMAIL": self.settings.blast_email,
        }

        if self.settings.blast_api_key:
            params["API_KEY"] = self.settings.blast_api_key

        response = await self._client.post(self.base_url, data=params)
        response.raise_for_status()

        # Parse RID from response
        rid = self._parse_rid(response.text)

        logger.info("blast_job_submitted", rid=rid)

        return BlastJob(rid=rid, status="WAITING")

    async def check_status(self, rid: str) -> str:
        """Check status of a BLAST job."""
        params = {
            "CMD": "Get",
            "RID": rid,
            "FORMAT_OBJECT": "SearchInfo",
        }

        response = await self._client.get(self.base_url, params=params)
        response.raise_for_status()

        return self._parse_status(response.text)

    async def get_results(self, rid: str) -> BlastResult:
        """Get results of a completed BLAST job."""
        params = {
            "CMD": "Get",
            "RID": rid,
            "FORMAT_TYPE": "JSON2",
        }

        response = await self._client.get(self.base_url, params=params)
        response.raise_for_status()

        return BlastResult.from_json(response.json())

    async def search_and_wait(
        self,
        sequence: str,
        program: str = "blastn",
        database: str = "nt",
        timeout_seconds: int = 300,
        poll_interval: int = 10,
    ) -> BlastResult:
        """Submit search and wait for results."""
        job = await self.submit_search(sequence, program, database)

        elapsed = 0
        while elapsed < timeout_seconds:
            await asyncio.sleep(poll_interval)
            elapsed += poll_interval

            status = await self.check_status(job.rid)

            if status == "READY":
                return await self.get_results(job.rid)
            elif status == "FAILED":
                raise BlastError(f"BLAST job {job.rid} failed")

            logger.debug("blast_job_polling", rid=job.rid, status=status)

        raise BlastTimeoutError(f"BLAST job {job.rid} timed out")

    def _parse_rid(self, response_text: str) -> str:
        """Parse RID from BLAST response."""
        for line in response_text.split("\n"):
            if line.startswith("RID = "):
                return line.split("=")[1].strip()
        raise ValueError("Could not parse RID from response")

    def _parse_status(self, response_text: str) -> str:
        """Parse status from BLAST response."""
        for line in response_text.split("\n"):
            if line.startswith("Status="):
                return line.split("=")[1].strip()
        return "UNKNOWN"

    async def close(self) -> None:
        await self._client.aclose()


class BlastError(Exception):
    """BLAST operation error."""
    pass


class BlastTimeoutError(BlastError):
    """BLAST job timed out."""
    pass
```

---

### Commit 1.3.2: Modelos BLAST
```
feat(blast): add BLAST data models

- Add BlastJob model
- Add BlastResult model
- Add BlastHit model
- Add HSP (High-scoring Segment Pair) model
- Add JSON parsing utilities
```

**src/blast/models.py**

---

### Commit 1.3.3: Parser de resultados
```
feat(blast): add BLAST result parser

- Parse JSON2 format responses
- Extract top hits with scores
- Calculate coverage and identity
- Handle edge cases (no hits, errors)
```

**src/blast/parser.py**

---

### Commit 1.3.4: Tests BLAST
```
test(blast): add unit tests for BLAST client

- Test job submission with mock
- Test status polling
- Test result parsing
- Test timeout handling
```

---

## 1.4 AI Orchestrator

### Commit 1.4.1: Contexto de análisis
```
feat(orchestrator): add analysis context

- Implement AnalysisContext class
- Store trace data and metadata
- Track analysis progress
- Manage intermediate results
```

**src/orchestrator/context.py:**
```python
from dataclasses import dataclass, field
from datetime import datetime
from typing import Optional, Any
from uuid import uuid4

from ..models import AnalysisRequest, AnalysisResult, AnalysisStatus


@dataclass
class AnalysisContext:
    """Context for an AI analysis pipeline."""

    contextId: str = field(default_factory=lambda: str(uuid4()))
    request: Optional[AnalysisRequest] = None
    result: Optional[AnalysisResult] = None
    startedAt: datetime = field(default_factory=datetime.utcnow)

    # Intermediate data
    trimmedSequence: Optional[str] = None
    blastRid: Optional[str] = None

    # Progress tracking
    completedSteps: list[str] = field(default_factory=list)
    currentStep: Optional[str] = None
    errors: list[str] = field(default_factory=list)

    @property
    def is_failed(self) -> bool:
        return len(self.errors) > 0

    def mark_step_completed(self, step: str) -> None:
        self.completedSteps.append(step)
        self.currentStep = None

    def mark_step_started(self, step: str) -> None:
        self.currentStep = step

    def add_error(self, error: str) -> None:
        self.errors.append(error)
```

---

### Commit 1.4.2: Pipeline de análisis
```
feat(orchestrator): add analysis pipeline

- Implement Pipeline class with steps
- Add step execution with error handling
- Add parallel step execution
- Add conditional step logic
```

**src/orchestrator/pipeline.py:**
```python
from abc import ABC, abstractmethod
from typing import Callable, Awaitable
import asyncio
import structlog

from .context import AnalysisContext

logger = structlog.get_logger()


class PipelineStep(ABC):
    """Base class for pipeline steps."""

    @property
    @abstractmethod
    def name(self) -> str:
        pass

    @abstractmethod
    async def execute(self, context: AnalysisContext) -> None:
        pass

    def should_run(self, context: AnalysisContext) -> bool:
        """Override to add conditional logic."""
        return True


class Pipeline:
    """Analysis pipeline that executes steps in order."""

    def __init__(self, steps: list[PipelineStep]):
        self.steps = steps

    async def execute(self, context: AnalysisContext) -> AnalysisContext:
        """Execute all pipeline steps."""
        for step in self.steps:
            if not step.should_run(context):
                logger.debug("pipeline_step_skipped", step=step.name)
                continue

            try:
                context.mark_step_started(step.name)
                logger.info("pipeline_step_started", step=step.name)

                await step.execute(context)

                context.mark_step_completed(step.name)
                logger.info("pipeline_step_completed", step=step.name)

            except Exception as e:
                context.add_error(f"{step.name}: {str(e)}")
                logger.error("pipeline_step_failed", step=step.name, error=str(e))

                # Continue or abort based on step criticality
                if self._is_critical_step(step):
                    break

        return context

    def _is_critical_step(self, step: PipelineStep) -> bool:
        """Determine if step failure should abort pipeline."""
        # Quality and BLAST are non-critical
        critical_steps = ["validate_sequence"]
        return step.name in critical_steps
```

---

### Commit 1.4.3: Orquestador principal
```
feat(orchestrator): add main orchestrator

- Implement AIOrchestrator class
- Coordinate analysis components
- Manage analysis lifecycle
- Emit events on completion/failure
```

**src/orchestrator/orchestrator.py:**
```python
import time
from typing import Optional
import structlog

from .context import AnalysisContext
from .pipeline import Pipeline, PipelineStep
from ..models import AnalysisRequest, AnalysisResult, AnalysisStatus, AnalysisType
from ..copilot import ChatHandler
from ..blast import BlastClient
from ..config import Settings
from ..eventbus import EventBusPublisher, AIAnalysisCompletedEvent, AIAnalysisFailedEvent

logger = structlog.get_logger()


class AIOrchestrator:
    """Orchestrates AI analysis across multiple services."""

    def __init__(
        self,
        settings: Settings,
        blast_client: BlastClient,
        chat_handler: ChatHandler,
        publisher: EventBusPublisher,
    ):
        self.settings = settings
        self.blast_client = blast_client
        self.chat_handler = chat_handler
        self.publisher = publisher

        self._pipeline = self._build_pipeline()
        self._active_analyses: dict[str, AnalysisContext] = {}

    async def analyze(self, request: AnalysisRequest) -> AnalysisResult:
        """Run full AI analysis on a sequence."""
        start_time = time.time()

        # Create context
        context = AnalysisContext(request=request)
        context.result = AnalysisResult(
            traceId=request.traceId,
            studyId=request.studyId,
            status=AnalysisStatus.RUNNING,
        )

        self._active_analyses[context.contextId] = context

        try:
            # Execute pipeline
            context = await self._pipeline.execute(context)

            # Generate summary with Copilot
            if AnalysisType.COPILOT in request.analysisTypes:
                summary = await self.chat_handler.interpret_analysis(context.result)
                context.result.summary = summary

            # Calculate metrics
            context.result.processingTimeMs = int((time.time() - start_time) * 1000)
            context.result.status = AnalysisStatus.COMPLETED if not context.is_failed else AnalysisStatus.FAILED

            # Emit completion event
            await self._emit_completion_event(context)

            return context.result

        except Exception as e:
            logger.error("analysis_failed", trace_id=request.traceId, error=str(e))
            context.result.status = AnalysisStatus.FAILED
            context.result.errors.append(str(e))

            await self._emit_failuREDACTED(context, str(e))

            return context.result

        finally:
            del self._active_analyses[context.contextId]

    def _build_pipeline(self) -> Pipeline:
        """Build the analysis pipeline."""
        steps = [
            ValidateSequenceStep(),
            QualityAnalysisStep(self.settings),
            BlastSearchStep(self.blast_client),
            VariantDetectionStep(),
            AnnotationStep(),
        ]
        return Pipeline(steps)

    async def _emit_completion_event(self, context: AnalysisContext) -> None:
        await self.publisher.publish(AIAnalysisCompletedEvent(
            traceId=context.request.traceId,
            studyId=context.request.studyId,
            analysisId=context.result.analysisId,
            overallConfidence=context.result.overallConfidence,
            processingTimeMs=context.result.processingTimeMs,
            summary=context.result.summary,
        ))

    async def _emit_failuREDACTED(self, context: AnalysisContext, error: str) -> None:
        await self.publisher.publish(AIAnalysisFailedEvent(
            traceId=context.request.traceId,
            studyId=context.request.studyId,
            errorMessage=error,
            errorType="AnalysisError",
        ))

    @property
    def metrics(self) -> dict:
        return {
            "activeAnalyses": len(self._active_analyses),
        }
```

---

### Commit 1.4.4: Steps del pipeline
```
feat(orchestrator): add pipeline steps implementation

- Add ValidateSequenceStep
- Add QualityAnalysisStep (mock for now)
- Add BlastSearchStep
- Add VariantDetectionStep (mock for now)
- Add AnnotationStep (mock for now)
```

---

### Commit 1.4.5: Tests Orchestrator
```
test(orchestrator): add unit tests for orchestrator

- Test pipeline execution
- Test context management
- Test error handling
- Test event emission
```

---

## 1.5 API Endpoints MVP

### Commit 1.5.1: Endpoints de análisis
```
feat(api): add analysis endpoints

- POST /analyze - Full analysis
- GET /analyze/{analysis_id} - Get result
- POST /analyze/blast - BLAST search only
- GET /analyze/{analysis_id}/status - Check status
```

---

### Commit 1.5.2: Endpoints de Copilot
```
feat(api): add copilot endpoints

- POST /copilot/ask - Ask question about analysis
- POST /copilot/report - Generate report
- GET /copilot/conversations/{id} - Get conversation
- DELETE /copilot/conversations/{id} - Clear conversation
```

---

### Commit 1.5.3: Tests API completos
```
test(api): add integration tests for API

- Test /analyze endpoint flow
- Test /copilot endpoints
- Test authentication
- Test error responses
```

---

## 1.6 Integración Final Fase 1

### Commit 1.6.1: Documentación API
```
docs(api): add API documentation

- Add docs/API.md with endpoint specs
- Add OpenAPI examples
- Add authentication guide
- Add rate limits documentation
```

---

### Commit 1.6.2: README y guías
```
docs: update README with setup instructions

- Add installation guide
- Add configuration reference
- Add development guide
- Add Docker deployment guide
```

---

# FASE 2: APIs Externas

## Objetivo
Integrar APIs externas adicionales: Ensembl, ClinVar/dbSNP, InterPro/Pfam, ViennaRNA.

---

## 2.1 Cliente Base

### Commit 2.1.1: Cliente HTTP base
```
feat(external): add base HTTP client with retry

- Implement BaseExternalClient
- Add retry with exponential backoff
- Add rate limiting
- Add response caching
```

**src/external/base.py**

---

## 2.2 Ensembl REST

### Commit 2.2.1: Cliente Ensembl
```
feat(external): add Ensembl REST client

- Implement EnsemblClient
- Add sequence lookup
- Add gene annotation retrieval
- Add variant effect predictor
```

**src/external/ensembl.py**

---

### Commit 2.2.2: Tests Ensembl
```
test(external): add Ensembl client tests
```

---

## 2.3 ClinVar/dbSNP

### Commit 2.3.1: Cliente ClinVar
```
feat(external): add ClinVar/dbSNP client

- Implement ClinVarClient
- Add variant lookup by position
- Add clinical significance retrieval
- Add rsID lookup
```

**src/external/clinvar.py**

---

### Commit 2.3.2: Tests ClinVar
```
test(external): add ClinVar client tests
```

---

## 2.4 InterPro/Pfam

### Commit 2.4.1: Cliente InterPro
```
feat(external): add InterPro client

- Implement InterProClient
- Add protein domain search
- Add family classification
- Add GO term retrieval
```

**src/external/interpro.py**

---

### Commit 2.4.2: Tests InterPro
```
test(external): add InterPro client tests
```

---

## 2.5 ViennaRNA

### Commit 2.5.1: Wrapper ViennaRNA
```
feat(external): add ViennaRNA wrapper

- Implement ViennaRNAWrapper
- Add secondary structure prediction
- Add minimum free energy calculation
- Add structure visualization data
```

**src/external/viennarna.py**

---

### Commit 2.5.2: Tests ViennaRNA
```
test(external): add ViennaRNA tests
```

---

## 2.6 Integración en Orchestrator

### Commit 2.6.1: Steps externos
```
feat(orchestrator): add external API pipeline steps

- Add EnsemblAnnotationStep
- Add ClinVarLookupStep
- Add InterProSearchStep
- Add RNAStructureStep
```

---

### Commit 2.6.2: Configuración de features
```
feat(config): add feature flags for external APIs

- Add toggles for each external service
- Add fallback behavior
- Add timeout configuration
```

---

# FASE 3: Modelos ML Custom

## Objetivo
Implementar modelos de Machine Learning propios para análisis de calidad, detección de artefactos y auto-trimming.

---

## 3.1 Infraestructura ML

### Commit 3.1.1: Base de modelos
```
feat(ml): add base ML model interface

- Implement BaseModel abstract class
- Add model loading/saving utilities
- Add prediction interface
- Add model registry
```

**src/ml/base.py:**
```python
from abc import ABC, abstractmethod
from pathlib import Path
from typing import Any, Optional
import structlog

logger = structlog.get_logger()


class BaseModel(ABC):
    """Base class for ML models."""

    @property
    @abstractmethod
    def name(self) -> str:
        pass

    @property
    @abstractmethod
    def version(self) -> str:
        pass

    @abstractmethod
    def predict(self, input_data: Any) -> Any:
        pass

    @abstractmethod
    def load(self, path: Path) -> None:
        pass

    @abstractmethod
    def save(self, path: Path) -> None:
        pass

    def health_check(self) -> bool:
        return True


class ModelRegistry:
    """Registry for ML models."""

    def __init__(self):
        self._models: dict[str, BaseModel] = {}

    def register(self, model: BaseModel) -> None:
        self._models[model.name] = model
        logger.info("model_registered", name=model.name, version=model.version)

    def get(self, name: str) -> Optional[BaseModel]:
        return self._models.get(name)

    def list_models(self) -> list[dict]:
        return [
            {"name": m.name, "version": m.version}
            for m in self._models.values()
        ]
```

---

## 3.2 Quality Predictor

### Commit 3.2.1: Predictor de calidad XGBoost
```
feat(ml): add quality predictor model

- Implement QualityPredictor with XGBoost
- Add feature extraction from sequences
- Add Q20/Q30 prediction
- Add accuracy estimation
```

**src/ml/quality_predictor.py**

---

### Commit 3.2.2: Training pipeline calidad
```
feat(ml): add quality predictor training

- Add data preprocessing
- Add feature engineering
- Add model training script
- Add evaluation metrics
```

---

### Commit 3.2.3: Tests Quality Predictor
```
test(ml): add quality predictor tests
```

---

## 3.3 Artifact Detector

### Commit 3.3.1: Detector de artefactos CNN
```
feat(ml): add artifact detector model

- Implement ArtifactDetector with PyTorch CNN
- Process chromatogram signals
- Detect dye blobs and pull-ups
- Return artifact positions
```

**src/ml/artifact_detector.py**

---

### Commit 3.3.2: Training pipeline artefactos
```
feat(ml): add artifact detector training

- Add signal preprocessing
- Add data augmentation
- Add model training script
- Add evaluation metrics
```

---

### Commit 3.3.3: Tests Artifact Detector
```
test(ml): add artifact detector tests
```

---

## 3.4 Auto-Trimmer

### Commit 3.4.1: Auto-trimmer inteligente
```
feat(ml): add intelligent auto-trimmer

- Implement AutoTrimmer model
- Combine quality + artifact signals
- Predict optimal trim points
- Handle edge cases
```

**src/ml/auto_trimmer.py**

---

### Commit 3.4.2: Tests Auto-Trimmer
```
test(ml): add auto-trimmer tests
```

---

## 3.5 Integración ML

### Commit 3.5.1: Steps ML en pipeline
```
feat(orchestrator): add ML pipeline steps

- Add MLQualityStep
- Add MLArtifactStep
- Add MLTrimStep
- Add fallback to basic analysis
```

---

### Commit 3.5.2: Endpoints ML
```
feat(api): add ML model endpoints

- GET /models - List available models
- GET /models/{name}/info - Model details
- POST /models/{name}/predict - Direct prediction
```

---

# FASE 4: Clustering y Filogenética

## Objetivo
Implementar análisis de clustering y generación de árboles filogenéticos.

---

## 4.1 Clustering

### Commit 4.1.1: Servicio de clustering
```
feat(analysis): add clustering service

- Implement ClusteringService
- Add hierarchical clustering
- Add k-means clustering
- Add DBSCAN for outlier detection
```

**src/analysis/clustering.py**

---

### Commit 4.1.2: Tests Clustering
```
test(analysis): add clustering tests
```

---

## 4.2 Filogenética

### Commit 4.2.1: Generador de árboles
```
feat(analysis): add phylogenetic tree generator

- Implement PhylogeneticService
- Add neighbor-joining tree
- Add UPGMA tree
- Export Newick format
```

---

### Commit 4.2.2: Métricas de diversidad
```
feat(analysis): add diversity metrics

- Add nucleotide diversity (π)
- Add haplotype diversity
- Add Tajima's D
- Add pairwise distance matrix
```

---

### Commit 4.2.3: Tests Filogenética
```
test(analysis): add phylogenetic tests
```

---

## 4.3 Integración

### Commit 4.3.1: Endpoints clustering/phylo
```
feat(api): add clustering and phylogenetic endpoints

- POST /clustering/sequences - Cluster sequences
- POST /phylogenetics/tree - Generate tree
- POST /phylogenetics/diversity - Calculate metrics
```

---

# FASE 5: Embeddings y Búsqueda Semántica

## Objetivo
Implementar generación de embeddings y búsqueda semántica en Qdrant.

---

## 5.1 Generación de Embeddings

### Commit 5.1.1: Servicio de embeddings
```
feat(storage): add embedding generation service

- Implement EmbeddingService
- Generate sequence embeddings (k-mer based)
- Generate annotation embeddings (text)
- Batch processing support
```

**src/storage/embeddings.py**

---

### Commit 5.1.2: Cliente Qdrant
```
feat(storage): add Qdrant client

- Implement QdrantClient wrapper
- Add collection management
- Add upsert/search operations
- Add filtering support
```

---

### Commit 5.1.3: Tests Embeddings
```
test(storage): add embedding tests
```

---

## 5.2 Búsqueda Semántica

### Commit 5.2.1: Servicio de búsqueda
```
feat(storage): add semantic search service

- Implement SemanticSearchService
- Search similar sequences
- Search by annotation text
- Combine with metadata filters
```

---

### Commit 5.2.2: Endpoints de búsqueda
```
feat(api): add semantic search endpoints

- POST /search/sequences - Find similar sequences
- POST /search/annotations - Search by text
- GET /search/similar/{trace_id} - Find similar to trace
```

---

### Commit 5.2.3: Tests Búsqueda
```
test(storage): add search tests
```

---

# FASE 6: Optimización y Producción

## Objetivo
Preparar el módulo para producción con optimizaciones, monitoreo y documentación completa.

---

## 6.1 Cache y Optimización

### Commit 6.1.1: Sistema de cache
```
feat(storage): add result caching

- Implement CacheService with Redis
- Cache BLAST results (TTL: 24h)
- Cache embeddings
- Cache Copilot responses
```

**src/storage/cache.py**

---

### Commit 6.1.2: Rate limiting
```
feat(api): add rate limiting

- Implement rate limiter middleware
- Per-user limits
- Per-endpoint limits
- Graceful degradation
```

---

## 6.2 Métricas y Monitoreo

### Commit 6.2.1: Métricas Prometheus
```
feat(api): add Prometheus metrics

- Add request latency histograms
- Add analysis counters
- Add model inference metrics
- Add external API call metrics
```

---

### Commit 6.2.2: Health check mejorado
```
feat(api): enhance health check endpoint

- Add component-level health
- Add dependency checks
- Add degraded state detection
- Add readiness/liveness separation
```

---

## 6.3 Documentación Final

### Commit 6.3.1: Documentación completa
```
docs: add comprehensive documentation

- Complete API reference
- Architecture documentation
- Deployment guide
- Troubleshooting guide
```

---

### Commit 6.3.2: Ejemplos y tutoriales
```
docs: add examples and tutorials

- Quick start guide
- Integration examples
- Python SDK examples
- Common workflows
```

---

# Resumen de Commits por Fase

| Fase | Commits | Descripción |
|------|---------|-------------|
| **Fase 0** | 7 | Preparación y estructura base |
| **Fase 1** | 20 | MVP: Copilot + BLAST + Orchestrator |
| **Fase 2** | 12 | APIs externas |
| **Fase 3** | 12 | Modelos ML custom |
| **Fase 4** | 7 | Clustering y filogenética |
| **Fase 5** | 6 | Embeddings y búsqueda |
| **Fase 6** | 5 | Optimización y producción |
| **Total** | **69** | Commits totales |

---

# Dependencias por Fase

## Fase 0-1 (MVP)
```toml
dependencies = [
    "fastapi>=0.115.0",
    "uvicorn[standard]>=0.32.0",
    "pydantic>=2.0.0",
    "pydantic-settings>=2.0.0",
    "redis>=5.0.0",
    "structlog>=24.0.0",
    "httpx>=0.27.0",
    "anthropic>=0.40.0",
]
```

## Fase 2
```toml
# Añadir:
"biopython>=1.84",
```

## Fase 3
```toml
# Añadir (opcional):
"scikit-learn>=1.5.0",
"xgboost>=2.1.0",
"torch>=2.4.0",
```

## Fase 4
```toml
# Añadir:
"scipy>=1.14.0",
```

## Fase 5
```toml
# Añadir:
"qdrant-client>=1.12.0",
```

---

# Notas de Implementación

## Buenas Ideas del Código Existente

1. **Estructura de eventos** (`eventbus/events.py`): El patrón de dataclasses con `to_dict()` y propiedades `eventType`/`category` es limpio.

2. **Metrics tracking**: El patrón de exponer métricas como property `metrics` en cada componente.

3. **Consumer groups**: La gestión de consumer groups con `BUSYGROUP` handling.

4. **Pipeline patterns**: La separación en `QualityPredictor`, `MutationDetector`, `SequenceAnnotator` es buena base conceptual.

## Cambios Respecto al Código Actual

1. **Naming**: Cambiar de `snake_case` a `camelCase` en atributos de modelos (convención).

2. **Estructura**: Reorganizar en subdominios más claros (`copilot/`, `blast/`, `orchestrator/`).

3. **Implementación real**: Reemplazar mocks por integraciones reales (Claude API, NCBI BLAST).

4. **Orchestrator**: Añadir el componente central que falta para coordinar análisis.

5. **Pipeline**: Implementar sistema de steps con error handling robusto.

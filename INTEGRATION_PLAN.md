# GeneFlow - Plan de Integración Completa

## Fecha: 2026-04-20
## Estado: ANÁLISIS COMPLETADO - PENDIENTE IMPLEMENTACIÓN

---

## Resumen Ejecutivo

Este documento describe la integración de extremo a extremo entre los tres componentes principales de GeneFlow:

1. **Backend .NET** - Orquestador y API Gateway
2. **Analysis (Python)** - Motor de análisis bioinformático
3. **Frontend (Next.js)** - Interfaz de usuario

---

## 1. Arquitectura Actual

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              ARQUITECTURA OBJETIVO                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────┐       ┌──────────────┐       ┌──────────────────────┐   │
│  │   Frontend   │◄─────►│  Backend     │       │  Analysis (Python)   │   │
│  │   (Next.js)  │  REST │  (.NET 8)    │       │  - 7 Analyzers       │   │
│  └──────────────┘       └──────┬───────┘       │  - 4 Parsers         │   │
│                                │               │  - Alignments        │   │
│                                │               └──────────┬───────────┘   │
│                                │                          │               │
│                                ▼                          ▼               │
│                    ┌─────────────────────────────────────────┐            │
│                    │              Redis Streams              │            │
│                    ├─────────────────────────────────────────┤            │
│                    │  Jobs (Input):                          │            │
│                    │    geneflow:jobs:traces                 │◄─── .NET   │
│                    │    geneflow:jobs:alignments             │     publica│
│                    │    geneflow:jobs:analysis               │            │
│                    ├─────────────────────────────────────────┤            │
│                    │  Events (Output):                       │            │
│                    │    geneflow:events:traces      ─────────┼─►Analysis  │
│                    │    geneflow:events:alignments           │   publica  │
│                    │    geneflow:events:analysis             │            │
│                    │    geneflow:events:studies     ◄────────┼─.NET       │
│                    │    geneflow:events:users                │  publica   │
│                    └─────────────────────────────────────────┘            │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Problemas Identificados

### 2.1 CRÍTICO: Backend No Envía Jobs a Analysis

**Problema:** El backend cambia estados pero no envía trabajos reales.

| Acción | Backend Actual | Debería |
|--------|----------------|---------|
| Upload Trace | Guarda en DB, levanta evento | + Publicar job a `geneflow:jobs:traces` |
| Start Processing | Cambia estado a Processing | + Publicar job a `geneflow:jobs:traces` |
| Request Analysis | No existe | Publicar job a `geneflow:jobs:analysis` |
| Create Alignment | No existe | Publicar job a `geneflow:jobs:alignments` |

**Archivos afectados:**
- `UploadTraceCommandHandler.cs` - No publica job
- `StartTraceProcessingCommandHandler.cs` - No publica job
- No existen handlers para análisis avanzados

### 2.2 CRÍTICO: Backend No Consume Eventos de Analysis

**Problema:** Analysis publica eventos pero backend no los procesa.

| Evento de Analysis | Consumido por Backend? |
|-------------------|------------------------|
| `TraceProcessed` | ❌ No (nombre diferente a `TraceProcessedEvent`) |
| `TraceProcessingFailed` | ❌ No |
| `AlignmentCompleted` | ❌ No (nombre diferente) |
| `TrimmingCompleted` | ❌ No |
| `HeterozygoteDetectionCompleted` | ❌ No |
| `MotifSearchCompleted` | ❌ No |
| `TranslationCompleted` | ❌ No |
| `ORFDetectionCompleted` | ❌ No |
| `RestrictionAnalysisCompleted` | ❌ No |

**Discrepancia de nombres:**
- Analysis publica: `TraceProcessed`
- Backend espera: `TraceProcessedEvent`

### 2.3 CRÍTICO: Frontend No Expone Capacidades de Analysis

**Capacidades de Analysis disponibles:**

| Capacidad | Backend Endpoint | Frontend UI |
|-----------|------------------|-------------|
| Quality Analysis | ❌ No | ❌ No |
| Trimming (3 algoritmos) | ⚠️ Parcial | ❌ No |
| Heterozygote Detection | ❌ No | ❌ No |
| Motif Search | ❌ No | ❌ No |
| Translation (6 frames) | ❌ No | ❌ No |
| ORF Detection | ❌ No | ❌ No |
| Restriction Analysis | ❌ No | ❌ No |
| Pairwise Alignment | ❌ No | ❌ No |
| Multiple Alignment | ❌ No | ❌ No |
| Consensus Building | ❌ No | ❌ No |

### 2.4 MODERADO: Eventos No Se Publican Correctamente

**Eventos definidos vs publicados:**

| Dominio | Definidos | Publicados | % |
|---------|-----------|------------|---|
| Traces | 11 | 6 | 55% |
| Studies | 12 | 2 | 17% |
| Identity | 13 | 0 | 0% |
| Profiles | 5 | 2 | 40% |
| Subscriptions | 5 | 1 | 20% |
| **Total** | **48** | **11** | **23%** |

### 2.5 MENOR: Traces Service Usa Mocks

El frontend usa mocks en lugar de API real para traces.

---

## 3. Plan de Implementación

### Fase 1: Infraestructura de Jobs (Backend)

#### 1.1 Crear Job Publisher

**Nuevo archivo:** `GeneFlow.ApiNet2.Infrastructure/Jobs/RedisJobPublisher.cs`

```csharp
public interface IJobPublisher
{
    Task PublishTraceJobAsync(TraceProcessingJob job, CancellationToken ct = default);
    Task PublishAlignmentJobAsync(AlignmentJob job, CancellationToken ct = default);
    Task PublishAnalysisJobAsync(AnalysisJob job, CancellationToken ct = default);
}

public sealed class RedisJobPublisher : IJobPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private const string TraceJobStream = "geneflow:jobs:traces";
    private const string AlignmentJobStream = "geneflow:jobs:alignments";
    private const string AnalysisJobStream = "geneflow:jobs:analysis";

    public async Task PublishTraceJobAsync(TraceProcessingJob job, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var entries = new NameValueEntry[]
        {
            new("data", JsonSerializer.Serialize(job, JsonOptions))
        };
        await db.StreamAddAsync(TraceJobStream, entries);
    }
    // ... similar para alignment y analysis
}
```

#### 1.2 Definir Modelos de Jobs

**Nuevo archivo:** `GeneFlow.ApiNet2.Application/Jobs/Models/`

```csharp
// TraceProcessingJob.cs
public sealed record TraceProcessingJob(
    string TraceId,
    string StudyId,
    string FileName,
    string StoragePath,
    string Format,
    Dictionary<string, object>? Options = null);

// AlignmentJob.cs
public sealed record AlignmentJob(
    string AlignmentId,
    string Type,  // "pairwise" | "multiple"
    List<string> TraceIds,
    List<string>? Sequences = null,
    AlignmentOptions? Options = null);

// AnalysisJob.cs
public sealed record AnalysisJob(
    string TraceId,
    string AnalysisType,  // trimming, heterozygote, motif, translation, orf, restriction
    string? Sequence = null,
    int[]? Quality = null,
    Dictionary<string, object>? Options = null);
```

#### 1.3 Actualizar Command Handlers

**Modificar:** `UploadTraceCommandHandler.cs`

```csharp
public sealed class UploadTraceCommandHandler
{
    private readonly ITraceUnitOfWork _unitOfWork;
    private readonly IJobPublisher _jobPublisher;  // NUEVO

    public async Task<Result<TraceDto>> Handle(...)
    {
        // ... código existente ...

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // NUEVO: Publicar job a Analysis
        await _jobPublisher.PublishTraceJobAsync(new TraceProcessingJob(
            TraceId: trace.Id.ToString(),
            StudyId: trace.StudyId.ToString(),
            FileName: request.FileName,
            StoragePath: request.StoragePath,
            Format: format.Name
        ), cancellationToken);

        return Result.Success(trace.ToDto());
    }
}
```

---

### Fase 2: Consumo de Eventos de Analysis (Backend)

#### 2.1 Crear Event Processor para Analysis

**Nuevo archivo:** `GeneFlow.ApiNet2.Infrastructure/Analysis/AnalysisEventProcessor.cs`

```csharp
public sealed class AnalysisEventProcessor : BackgroundService
{
    // Consume de: geneflow:events:traces, geneflow:events:alignments, geneflow:events:analysis

    // Mapeo de eventos de Analysis a handlers
    private static readonly Dictionary<string, Func<...>> EventHandlers = new()
    {
        ["TraceProcessed"] = HandleTraceProcessed,
        ["TraceProcessingFailed"] = HandleTraceProcessingFailed,
        ["AlignmentCompleted"] = HandleAlignmentCompleted,
        ["AlignmentFailed"] = HandleAlignmentFailed,
        ["TrimmingCompleted"] = HandleTrimmingCompleted,
        ["HeterozygoteDetectionCompleted"] = HandleHeterozygoteDetected,
        ["MotifSearchCompleted"] = HandleMotifSearchCompleted,
        ["TranslationCompleted"] = HandleTranslationCompleted,
        ["ORFDetectionCompleted"] = HandleORFDetected,
        ["RestrictionAnalysisCompleted"] = HandleRestrictionAnalysisCompleted,
    };

    private async Task HandleTraceProcessed(JsonDocument data, IServiceScope scope)
    {
        var traceId = data.RootElement.GetProperty("traceId").GetString();
        var sequenceLength = data.RootElement.GetProperty("sequenceLength").GetInt32();
        var meanQuality = data.RootElement.GetProperty("meanQuality").GetDecimal();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<ITraceUnitOfWork>();
        var trace = await unitOfWork.Traces.GetByIdAsync(TraceId.Parse(traceId));

        if (trace is not null)
        {
            trace.CompleteProcessing(
                QualityMetrics.Create(sequenceLength, meanQuality, ...),
                hasChromatogram: data.RootElement.GetProperty("hasChromatogram").GetBoolean()
            );
            await unitOfWork.SaveChangesAsync();
        }
    }
}
```

---

### Fase 3: Endpoints de Analysis (Backend API)

#### 3.1 Crear AnalysisEndpoints

**Nuevo archivo:** `GeneFlow.ApiNet2.API/Endpoints/Analysis/AnalysisEndpoints.cs`

```csharp
public sealed class AnalysisEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/traces/{traceId}/analysis")
            .WithTags("Analysis")
            .RequireAuthorization();

        // Trimming
        group.MapPost("/trim", RequestTrimming);
        group.MapGet("/trim/preview", PreviewTrimming);

        // Quality
        group.MapGet("/quality", GetQualityMetrics);

        // Heterozygote Detection
        group.MapPost("/heterozygotes", DetectHeterozygotes);

        // Motif Search
        group.MapPost("/motifs/search", SearchMotifs);

        // Translation
        group.MapPost("/translate", TranslateSequence);

        // ORF Detection
        group.MapPost("/orfs", DetectORFs);

        // Restriction Analysis
        group.MapPost("/restriction", AnalyzeRestrictionSites);
    }
}
```

#### 3.2 Crear AlignmentEndpoints

**Nuevo archivo:** `GeneFlow.ApiNet2.API/Endpoints/Alignments/AlignmentEndpoints.cs`

```csharp
public sealed class AlignmentEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/alignments")
            .WithTags("Alignments")
            .RequireAuthorization();

        // Pairwise Alignment
        group.MapPost("/pairwise", CreatePairwiseAlignment);

        // Multiple Alignment
        group.MapPost("/multiple", CreateMultipleAlignment);

        // Get Alignment Results
        group.MapGet("/{alignmentId}", GetAlignment);
        group.MapGet("/{alignmentId}/consensus", GetConsensus);
        group.MapGet("/{alignmentId}/variants", GetVariants);
    }
}
```

---

### Fase 4: Dominio de Analysis (Backend)

#### 4.1 Crear Entidades de Analysis

**Nuevos archivos en:** `GeneFlow.ApiNet2.Domain/Analysis/`

```
Analysis/
├── AnalysisResult.cs          # Aggregate root
├── AnalysisResultId.cs        # Strongly-typed ID
├── Entities/
│   ├── TrimmingResult.cs
│   ├── HeterozygoteResult.cs
│   ├── MotifSearchResult.cs
│   ├── TranslationResult.cs
│   ├── ORFResult.cs
│   └── RestrictionResult.cs
├── Enumerations/
│   ├── AnalysisType.cs        # Enum: Trimming, Heterozygote, etc.
│   ├── AnalysisStatus.cs      # Pending, Processing, Completed, Failed
│   └── TrimmingAlgorithm.cs   # ModifiedMott, SlidingWindow, QualityThreshold
└── Events/
    ├── AnalysisRequestedEvent.cs
    ├── AnalysisCompletedEvent.cs
    └── AnalysisFailedEvent.cs
```

#### 4.2 Crear Entidades de Alignment

**Nuevos archivos en:** `GeneFlow.ApiNet2.Domain/Alignments/`

```
Alignments/
├── Alignment.cs               # Aggregate root
├── AlignmentId.cs
├── ValueObjects/
│   ├── AlignmentScore.cs
│   ├── ConsensusSequence.cs
│   └── AlignedSequence.cs
├── Enumerations/
│   ├── AlignmentType.cs       # Pairwise, Multiple
│   └── AlignmentStatus.cs     # Pending, Processing, Completed, Failed
└── Events/
    ├── AlignmentRequestedEvent.cs
    ├── AlignmentCompletedEvent.cs
    └── AlignmentFailedEvent.cs
```

---

### Fase 5: Frontend - Servicios y Tipos

#### 5.1 Actualizar Tipos

**`app/types/analysis.ts`** (NUEVO)

```typescript
export type AnalysisType =
  | "trimming"
  | "heterozygote"
  | "motif"
  | "translation"
  | "orf"
  | "restriction";

export type TrimmingAlgorithm = "modified_mott" | "sliding_window" | "quality_threshold";

export interface TrimmingOptions {
  algorithm: TrimmingAlgorithm;
  qualityThreshold?: number;
  windowSize?: number;
  minLength?: number;
}

export interface TrimmingResult {
  traceId: string;
  algorithm: TrimmingAlgorithm;
  originalLength: number;
  trimmedLength: number;
  trimStart: number;
  trimEnd: number;
  trimmedSequence: string;
}

export interface HeterozygoteCall {
  position: number;
  base1: string;
  base2: string;
  iupacCode: string;
  ratio: number;
  confidence: number;
}

export interface HeterozygoteResult {
  traceId: string;
  calls: HeterozygoteCall[];
  totalCalls: number;
}

export interface MotifMatch {
  pattern: string;
  start: number;
  end: number;
  matchedSequence: string;
}

export interface MotifSearchResult {
  traceId: string;
  pattern: string;
  matches: MotifMatch[];
  totalMatches: number;
}

export interface ORF {
  start: number;
  end: number;
  frame: number;
  strand: "+" | "-";
  length: number;
  proteinSequence: string;
}

export interface ORFResult {
  traceId: string;
  orfs: ORF[];
  longestOrf: ORF | null;
}

export interface RestrictionSite {
  enzyme: string;
  position: number;
  cutPosition: number;
  recognitionSequence: string;
}

export interface RestrictionResult {
  traceId: string;
  sites: RestrictionSite[];
  enzymeCount: number;
  totalSites: number;
}

export interface TranslationResult {
  traceId: string;
  frame: number;
  proteinSequence: string;
  length: number;
}
```

#### 5.2 Crear Analysis Service

**`app/services/analysis.service.ts`** (NUEVO)

```typescript
import { api } from "@/lib/api-client";
import type {
  TrimmingOptions,
  TrimmingResult,
  HeterozygoteResult,
  MotifSearchResult,
  ORFResult,
  RestrictionResult,
  TranslationResult,
} from "@/types";

export const analysisService = {
  // Trimming
  async requestTrimming(traceId: string, options: TrimmingOptions): Promise<{ jobId: string }> {
    return api.post(`/api/v1/traces/${traceId}/analysis/trim`, options);
  },

  async previewTrimming(traceId: string, options: TrimmingOptions): Promise<TrimmingResult> {
    const params = new URLSearchParams(options as Record<string, string>);
    return api.get(`/api/v1/traces/${traceId}/analysis/trim/preview?${params}`);
  },

  // Heterozygote Detection
  async detectHeterozygotes(traceId: string, options?: {
    minRatio?: number;
    minConfidence?: number;
  }): Promise<{ jobId: string }> {
    return api.post(`/api/v1/traces/${traceId}/analysis/heterozygotes`, options);
  },

  // Motif Search
  async searchMotifs(traceId: string, pattern: string, options?: {
    type?: "exact" | "iupac" | "regex";
  }): Promise<{ jobId: string }> {
    return api.post(`/api/v1/traces/${traceId}/analysis/motifs/search`, { pattern, ...options });
  },

  // Translation
  async translate(traceId: string, options?: {
    frame?: number;
    geneticCode?: number;
  }): Promise<{ jobId: string }> {
    return api.post(`/api/v1/traces/${traceId}/analysis/translate`, options);
  },

  // ORF Detection
  async detectORFs(traceId: string, options?: {
    minLength?: number;
    startCodons?: string[];
    stopCodons?: string[];
  }): Promise<{ jobId: string }> {
    return api.post(`/api/v1/traces/${traceId}/analysis/orfs`, options);
  },

  // Restriction Analysis
  async analyzeRestriction(traceId: string, enzymes: string[]): Promise<{ jobId: string }> {
    return api.post(`/api/v1/traces/${traceId}/analysis/restriction`, { enzymes });
  },

  // Get Results (polling or WebSocket)
  async getResult<T>(traceId: string, analysisType: string): Promise<T> {
    return api.get(`/api/v1/traces/${traceId}/analysis/${analysisType}/result`);
  },
};
```

#### 5.3 Crear Alignment Service

**`app/services/alignment.service.ts`** (NUEVO)

```typescript
import { api } from "@/lib/api-client";
import type { Alignment, AlignmentResult, ConsensusResult } from "@/types";

export const alignmentService = {
  async createPairwise(traceIds: [string, string], options?: {
    matchScore?: number;
    mismatchPenalty?: number;
    gapPenalty?: number;
  }): Promise<{ alignmentId: string }> {
    return api.post("/api/v1/alignments/pairwise", { traceIds, options });
  },

  async createMultiple(traceIds: string[], options?: {
    buildConsensus?: boolean;
    consensusMethod?: "majority" | "threshold";
  }): Promise<{ alignmentId: string }> {
    return api.post("/api/v1/alignments/multiple", { traceIds, options });
  },

  async getAlignment(alignmentId: string): Promise<AlignmentResult> {
    return api.get(`/api/v1/alignments/${alignmentId}`);
  },

  async getConsensus(alignmentId: string): Promise<ConsensusResult> {
    return api.get(`/api/v1/alignments/${alignmentId}/consensus`);
  },

  async getVariants(alignmentId: string): Promise<VariantResult[]> {
    return api.get(`/api/v1/alignments/${alignmentId}/variants`);
  },
};
```

---

### Fase 6: Frontend - UI Components

#### 6.1 Analysis Panel Component

**`app/components/features/analysis/AnalysisPanel.tsx`** (NUEVO)

Panel lateral que muestra todas las opciones de análisis disponibles:
- Trimming con selector de algoritmo
- Heterozygote detection
- Motif search con input de patrón
- Translation con selector de frame
- ORF detection
- Restriction analysis con selector de enzimas

#### 6.2 Analysis Results Components

**`app/components/features/analysis/`**

```
analysis/
├── AnalysisPanel.tsx           # Panel principal
├── TrimmingPanel.tsx           # UI para trimming
├── TrimmingResult.tsx          # Visualización de resultado
├── HeterozygotePanel.tsx
├── HeterozygoteResult.tsx
├── MotifSearchPanel.tsx
├── MotifSearchResult.tsx
├── TranslationPanel.tsx
├── TranslationResult.tsx
├── ORFPanel.tsx
├── ORFResult.tsx
├── RestrictionPanel.tsx
└── RestrictionResult.tsx
```

#### 6.3 Alignment Components

**`app/components/features/alignment/`**

```
alignment/
├── AlignmentPanel.tsx          # Panel de alineamiento
├── PairwiseAlignment.tsx       # UI para pairwise
├── MultipleAlignment.tsx       # UI para multiple
├── AlignmentViewer.tsx         # Visualización de alineamiento
├── ConsensusViewer.tsx         # Visualización de consenso
└── VariantTable.tsx            # Tabla de variantes
```

---

### Fase 7: Corrección de Eventos

#### 7.1 Eventos que Deben Levantarse

**Agregar a aggregates existentes:**

```csharp
// User.cs - Agregar en Register()
RaiseDomainEvent(new UserRegisteredEvent(Id, Email, Username, DateTime.UtcNow));

// Study.cs - Agregar en AddMember()
RaiseDomainEvent(new StudyMemberAddedEvent(Id, memberId, roleId, memberCount));

// Study.cs - Agregar en ChangeStatus()
RaiseDomainEvent(new StudyStatusChangedEvent(Id, oldStatus, newStatus));

// Profile.cs - Agregar en Create()
RaiseDomainEvent(new ProfileCreatedEvent(Id, UserId));
```

#### 7.2 Eventos que Deben Eliminarse

**Eventos definidos pero que no tienen sentido:**
- `TracesUploadedEvent` - Redundante, usar múltiples `TraceUploadedEvent`
- Revisar si hay otros eventos innecesarios

#### 7.3 Normalizar Nombres de Eventos

**Opción A (Recomendada):** Backend normaliza nombres al consumir

```csharp
// AnalysisEventProcessor.cs
private static string NormalizeEventType(string eventType)
{
    return eventType switch
    {
        "TraceProcessed" => "TraceProcessedEvent",
        "AlignmentCompleted" => "AlignmentCompletedEvent",
        _ => eventType
    };
}
```

**Opción B:** Analysis publica con nombres que terminan en "Event"

---

### Fase 8: Integración Traces Service Frontend

#### 8.1 Reescribir traces.service.ts

```typescript
import { api } from "@/lib/api-client";
import type { Trace, TraceFilters, PaginatedResponse } from "@/types";

export const tracesService = {
  // Lista global de traces del usuario
  async getAll(filters?: TraceFilters, page = 1, limit = 20): Promise<PaginatedResponse<Trace>> {
    const params = new URLSearchParams({
      pageNumber: page.toString(),
      pageSize: limit.toString(),
    });
    if (filters?.status) params.set("statusId", getStatusId(filters.status).toString());
    if (filters?.search) params.set("searchTerm", filters.search);
    if (filters?.studyId) params.set("studyId", filters.studyId);

    return api.get<PaginatedResponse<Trace>>(`/api/v1/traces?${params}`);
  },

  async getByStudyId(studyId: string, page = 1, limit = 20): Promise<PaginatedResponse<Trace>> {
    const params = new URLSearchParams({
      pageNumber: page.toString(),
      pageSize: limit.toString(),
    });
    return api.get<PaginatedResponse<Trace>>(`/api/v1/studies/${studyId}/traces?${params}`);
  },

  async getById(traceId: string): Promise<Trace> {
    return api.get<Trace>(`/api/v1/traces/${traceId}`);
  },

  async upload(studyId: string, file: File, name: string, description?: string): Promise<Trace> {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("name", name);
    if (description) formData.append("description", description);

    return api.post<Trace>(`/api/v1/studies/${studyId}/traces/upload`, formData);
  },

  async delete(traceId: string): Promise<void> {
    return api.delete(`/api/v1/traces/${traceId}`);
  },

  async archive(traceId: string): Promise<void> {
    return api.post(`/api/v1/traces/${traceId}/archive`);
  },

  async getStats(): Promise<TraceStats> {
    return api.get<TraceStats>(`/api/v1/traces/stats`);
  },
};
```

---

## 4. Archivos a Crear/Modificar

### Backend - Nuevos Archivos

```
GeneFlow.ApiNet2.Infrastructure/
├── Jobs/
│   ├── IJobPublisher.cs
│   ├── RedisJobPublisher.cs
│   └── Models/
│       ├── TraceProcessingJob.cs
│       ├── AlignmentJob.cs
│       └── AnalysisJob.cs
├── Analysis/
│   └── AnalysisEventProcessor.cs

GeneFlow.ApiNet2.Domain/
├── Analysis/
│   ├── AnalysisResult.cs
│   ├── AnalysisResultId.cs
│   ├── IAnalysisResultRepository.cs
│   └── Enumerations/
│       ├── AnalysisType.cs
│       └── AnalysisStatus.cs
├── Alignments/
│   ├── Alignment.cs
│   ├── AlignmentId.cs
│   ├── IAlignmentRepository.cs
│   └── Enumerations/
│       ├── AlignmentType.cs
│       └── AlignmentStatus.cs

GeneFlow.ApiNet2.Application/
├── Analysis/
│   ├── Commands/
│   │   ├── RequestTrimming/
│   │   ├── RequestHeterozygoteDetection/
│   │   ├── RequestMotifSearch/
│   │   ├── RequestTranslation/
│   │   ├── RequestORFDetection/
│   │   └── RequestRestrictionAnalysis/
│   └── Queries/
│       └── GetAnalysisResult/
├── Alignments/
│   ├── Commands/
│   │   ├── CreatePairwiseAlignment/
│   │   └── CreateMultipleAlignment/
│   └── Queries/
│       ├── GetAlignment/
│       ├── GetConsensus/
│       └── GetVariants/

GeneFlow.ApiNet2.API/
├── Endpoints/
│   ├── Analysis/
│   │   └── AnalysisEndpoints.cs
│   └── Alignments/
│       └── AlignmentEndpoints.cs
├── Contracts/
│   ├── Analysis/
│   │   ├── Requests/
│   │   └── Responses/
│   └── Alignments/
│       ├── Requests/
│       └── Responses/
```

### Backend - Archivos a Modificar

```
GeneFlow.ApiNet2.Application/Traces/Commands/UploadTrace/UploadTraceCommandHandler.cs
GeneFlow.ApiNet2.Application/Traces/Commands/StartTraceProcessing/StartTraceProcessingCommandHandler.cs
GeneFlow.ApiNet2.Infrastructure/DependencyInjection.cs
GeneFlow.ApiNet2.Domain/Studies/Study.cs (agregar eventos faltantes)
GeneFlow.ApiNet2.Domain/Identity/User.cs (agregar eventos faltantes)
GeneFlow.ApiNet2.Domain/Profiles/Profile.cs (agregar eventos faltantes)
```

### Frontend - Nuevos Archivos

```
app/
├── types/
│   ├── analysis.ts
│   └── alignment.ts
├── services/
│   ├── analysis.service.ts
│   └── alignment.service.ts
├── hooks/
│   ├── use-analysis.ts
│   └── use-alignment.ts
├── components/features/
│   ├── analysis/
│   │   ├── AnalysisPanel.tsx
│   │   ├── TrimmingPanel.tsx
│   │   ├── HeterozygotePanel.tsx
│   │   ├── MotifSearchPanel.tsx
│   │   ├── TranslationPanel.tsx
│   │   ├── ORFPanel.tsx
│   │   └── RestrictionPanel.tsx
│   └── alignment/
│       ├── AlignmentPanel.tsx
│       ├── PairwiseAlignment.tsx
│       ├── MultipleAlignment.tsx
│       └── AlignmentViewer.tsx
```

### Frontend - Archivos a Modificar

```
app/services/traces.service.ts (reescribir)
app/hooks/use-traces.ts (agregar mutations)
app/types/trace.ts (actualizar tipos)
app/[locale]/(platform)/traces/[traceId]/page.tsx (agregar AnalysisPanel)
```

---

## 5. Orden de Implementación

### Semana 1: Infraestructura de Jobs
1. ✅ Crear `IJobPublisher` y `RedisJobPublisher`
2. ✅ Definir modelos de jobs
3. ✅ Modificar `UploadTraceCommandHandler` para publicar jobs
4. ✅ Modificar `StartTraceProcessingCommandHandler`
5. ✅ Registrar en DI
6. ✅ Testing con Redis local

### Semana 2: Consumo de Eventos de Analysis
1. ✅ Crear `AnalysisEventProcessor`
2. ✅ Implementar handlers para cada tipo de evento
3. ✅ Actualizar estado de traces con resultados
4. ✅ Testing E2E: Upload → Analysis → Result

### Semana 3: Endpoints de Analysis y Alignment
1. ✅ Crear dominio de Analysis
2. ✅ Crear dominio de Alignment
3. ✅ Crear endpoints de Analysis
4. ✅ Crear endpoints de Alignment
5. ✅ Testing de API

### Semana 4: Frontend Integration
1. ✅ Crear tipos de Analysis y Alignment
2. ✅ Crear servicios de Analysis y Alignment
3. ✅ Crear hooks
4. ✅ Reescribir `traces.service.ts`
5. ✅ Crear componentes de UI

### Semana 5: Corrección de Eventos + Polish
1. ✅ Agregar eventos faltantes en aggregates
2. ✅ Eliminar eventos innecesarios
3. ✅ Testing E2E completo
4. ✅ Documentación

---

## 6. Criterios de Aceptación

### Funcionales
- [ ] Upload de trace inicia procesamiento automático en Analysis
- [ ] Resultados de Analysis actualizan estado del trace
- [ ] UI muestra todas las capacidades de Analysis
- [ ] Alignments funcionan de extremo a extremo
- [ ] Dashboard muestra métricas reales

### No Funcionales
- [ ] Tiempo de respuesta < 500ms para APIs síncronas
- [ ] Jobs procesan en < 30s para traces típicos
- [ ] Eventos se entregan con latencia < 100ms
- [ ] Frontend usa API real (0 mocks)

### Eventos
- [ ] > 90% de eventos definidos se publican correctamente
- [ ] UsageStatsEventProcessor procesa todos los eventos
- [ ] No hay eventos huérfanos

---

## 7. Riesgos y Mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| Incompatibilidad de formatos de eventos | Media | Alto | Normalizar en capa de consumo |
| Pérdida de jobs en Redis | Baja | Alto | Consumer groups + ACK manual |
| Timeout en análisis largos | Media | Medio | Polling con WebSocket fallback |
| Tipos frontend/backend desalineados | Alta | Medio | Generar tipos desde OpenAPI |

---

*Documento actualizado: 2026-04-20*
*Versión: 2.0*

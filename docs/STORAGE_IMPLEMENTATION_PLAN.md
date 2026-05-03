# Plan de Implementación: Almacenamiento en Datalake

## Fecha: 2026-04-22
## Estado: PENDIENTE IMPLEMENTACIÓN

---

## Resumen Ejecutivo

Este plan implementa el almacenamiento de trazas y resultados de análisis en el Datalake con chunking para respuestas paginadas al frontend.

### Objetivos

1. **Trazas chunkeadas** - Secuencias divididas en chunks de 10,000 bases
2. **Respuestas paginadas** - Frontend solicita chunks individuales, no toda la traza
3. **Análisis en Datalake** - Resultados de análisis almacenados junto a las trazas
4. **Single Source of Truth** - Datalake es la fuente de verdad para datos binarios

---

## Arquitectura Objetivo

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              FLUJO DE DATOS                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  1. UPLOAD                                                                   │
│     Frontend ──► .NET API ──► File Storage (temporal)                       │
│                      │                                                       │
│                      ▼                                                       │
│              TraceUploaded Event ──► Redis Streams                          │
│                                            │                                 │
│  2. PROCESAMIENTO                          ▼                                 │
│     geneflow:jobs:traces ──► Python Analysis Worker                         │
│                                     │                                        │
│                                     ├─► Parse AB1/SCF                        │
│                                     ├─► Extract sequence + quality           │
│                                     └─► Publish TraceProcessed Event         │
│                                              │                               │
│  3. ALMACENAMIENTO                           ▼                               │
│     geneflow:events:traces ──► Datalake StorageMounter                      │
│                                     │                                        │
│                                     ├─► Store original file                  │
│                                     ├─► Chunk sequence (10K bases)           │
│                                     ├─► Store chunks as JSON                 │
│                                     └─► Create manifest.json                 │
│                                                                              │
│  4. LECTURA PAGINADA                                                         │
│     Frontend ──► .NET API ──► Datalake Storage Client                       │
│                      │              │                                        │
│                      │              ├─► GET manifest.json                    │
│                      │              └─► GET chunk_{page}.json                │
│                      │                                                       │
│                      ▼                                                       │
│              Response paginada al frontend                                   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Estructura de Almacenamiento

```
bucket: geneflow-traces/
└── traces/{trace_id}/
    ├── original.ab1              # Archivo original
    ├── manifest.json             # Índice de chunks
    ├── chunks/
    │   ├── chunk_0000.json       # Bases 0-9999
    │   ├── chunk_0001.json       # Bases 10000-19999
    │   └── ...
    └── analysis/
        ├── trimming.json         # Resultado de trimming
        ├── heterozygotes.json    # Detección de heterocigotos
        ├── motifs.json           # Búsqueda de motifs
        ├── translation.json      # Traducción
        ├── orfs.json             # ORFs detectados
        └── restriction.json      # Sitios de restricción
```

---

## Fase 1: Modificar Analysis Worker (Python)

### 1.1 Actualizar TraceProcessed Event

**Archivo:** `geneflow-analysis/src/events/events.py`

El evento `TraceProcessed` debe incluir los datos parseados completos para que el StorageMounter pueda chunkearlos.

```python
@dataclass
class TraceProcessed(BaseEvent):
    """Event emitted when a trace is successfully processed."""

    traceId: str = ""
    studyId: str = ""
    format: str = ""
    sequenceLength: int = 0
    meanQuality: Optional[float] = None
    hasChromatogram: bool = False

    # NUEVO: Datos parseados para StorageMounter
    parsedData: Optional[dict] = None  # {sequence, quality_scores, chromatogram}

    @property
    def category(self) -> str:
        return "traces"

    def _get_data(self) -> dict[str, Any]:
        data = {
            "traceId": self.traceId,
            "studyId": self.studyId,
            "format": self.format,
            "sequenceLength": self.sequenceLength,
            "meanQuality": self.meanQuality,
            "hasChromatogram": self.hasChromatogram,
        }
        if self.parsedData:
            data["parsedData"] = self.parsedData
        return data
```

### 1.2 Actualizar TraceWorker

**Archivo:** `geneflow-analysis/src/workers/trace.py`

```python
async def process_job(self, job_id: str, job_data: dict[str, Any]) -> None:
    """Process a trace file parsing job."""
    try:
        job = TraceProcessingJob.from_dict(job_data)
        trace_data = await self._fetch_trace_data(job)
        parser = self._parser_factory.get_parser(job.format)
        parsed = parser.parse(trace_data, trace_id=job.traceId)

        # NUEVO: Incluir datos parseados en el evento
        parsed_data = {
            "sequence": parsed.sequence,
            "quality_scores": parsed.qualityScores or [],
            "chromatogram": parsed.chromatogram.to_dict() if parsed.chromatogram else None,
            "filename": job.fileName,
            "format": job.format.value,
        }

        event = TraceProcessed(
            traceId=job.traceId,
            studyId=job.studyId,
            format=job.format.value,
            sequenceLength=len(parsed.sequence),
            meanQuality=parsed.qualityMetrics.meanQuality if parsed.qualityMetrics else None,
            hasChromatogram=parsed.chromatogram is not None,
            parsedData=parsed_data,  # NUEVO
        )
        await self._publisher.publish(event)

    except Exception as e:
        # ... error handling
```

### 1.3 Crear Evento para Resultados de Análisis

**Archivo:** `geneflow-analysis/src/events/events.py`

```python
@dataclass
class AnalysisResultStored(BaseEvent):
    """Event emitted when an analysis result is stored."""

    traceId: str = ""
    analysisType: str = ""  # trimming, heterozygote, motif, translation, orf, restriction
    resultData: dict = field(default_factory=dict)

    @property
    def category(self) -> str:
        return "analysis"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "analysisType": self.analysisType,
            "resultData": self.resultData,
        }
```

---

## Fase 2: Actualizar StorageMounter (Datalake)

### 2.1 Modificar TraceHandler para Análisis

**Archivo:** `geneflow-datalake/src/mounters/storage/handlers/trace_handler.py`

```python
class TraceHandler:
    """Handles trace file storage operations."""

    # ... existing code ...

    async def handle_processed(self, payload: dict) -> None:
        """Handle TraceProcessed event - store chunked data."""
        trace_id = payload.get("traceId")
        parsed_data = payload.get("parsedData")

        if not parsed_data:
            logger.warning("trace_processed_no_parsed_data", trace_id=trace_id)
            return

        await self._stoREDACTED(trace_id, parsed_data)
        logger.info("storage_trace_processed", trace_id=trace_id)

    async def handle_analysis_result(self, payload: dict) -> None:
        """Handle AnalysisResultStored event."""
        trace_id = payload.get("traceId")
        analysis_type = payload.get("analysisType")
        result_data = payload.get("resultData", {})

        result_key = f"traces/{trace_id}/analysis/{analysis_type}.json"
        await self._connection.put_object(
            self._bucket,
            result_key,
            json.dumps(result_data, indent=2).encode(),
        )
        logger.info("storage_analysis_stored", trace_id=trace_id, type=analysis_type)

    async def get_analysis_result(self, trace_id: str, analysis_type: str) -> dict | None:
        """Get analysis result for a trace."""
        result_key = f"traces/{trace_id}/analysis/{analysis_type}.json"

        if not await self._connection.object_exists(self._bucket, result_key):
            return None

        data = await self._connection.get_object(self._bucket, result_key)
        return json.loads(data.decode())

    async def list_analysis_results(self, trace_id: str) -> list[str]:
        """List available analysis results for a trace."""
        prefix = f"traces/{trace_id}/analysis/"
        objects = await self._connection.list_objects(self._bucket, prefix)
        return [
            obj["key"].split("/")[-1].replace(".json", "")
            for obj in objects
        ]
```

### 2.2 Actualizar StorageMounter para nuevos eventos

**Archivo:** `geneflow-datalake/src/mounters/storage/mounter.py`

```python
async def handle_event(self, event: dict) -> None:
    """Handle an incoming event."""
    event_type = event.get("type", "") or event.get("event_type", "")
    payload = event.get("payload", {}) or event.get("data", {})

    if event_type == "TraceUploaded":
        await self._trace_handler.handle_uploaded(payload)
    elif event_type == "TraceProcessed":
        await self._trace_handler.handle_processed(payload)  # NUEVO
    elif event_type == "TraceDeleted":
        await self._trace_handler.handle_deleted(payload)
    elif event_type == "AnalysisResultStored":
        await self._trace_handler.handle_analysis_result(payload)  # NUEVO
    elif event_type == "ProfilePhotoUploadedEvent":
        await self._photo_handler.handle_uploaded(payload)
    elif event_type == "ProfilePhotoDeletedEvent":
        await self._photo_handler.handle_deleted(payload)

    self._metrics["events_processed"] += 1
```

---

## Fase 3: Cliente de Storage en .NET Backend

### 3.1 Crear Interfaz IDatalakeStorageClient

**Archivo:** `GeneFlow.ApiNet2.Application/Traces/Interfaces/IDatalakeStorageClient.cs`

```csharp
namespace GeneFlow.ApiNet2.Application.Traces.Interfaces;

/// <summary>
/// Client for reading trace data from the Datalake storage.
/// </summary>
public interface IDatalakeStorageClient
{
    /// <summary>
    /// Gets the manifest for a trace.
    /// </summary>
    Task<TraceManifestDto?> GetManifestAsync(string traceId, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific chunk of trace data.
    /// </summary>
    Task<TraceChunkDto?> GetChunkAsync(string traceId, int chunkIndex, CancellationToken ct = default);

    /// <summary>
    /// Gets the original trace file.
    /// </summary>
    Task<(byte[] Data, string Extension)?> GetOriginalFileAsync(string traceId, CancellationToken ct = default);

    /// <summary>
    /// Gets an analysis result for a trace.
    /// </summary>
    Task<T?> GetAnalysisResultAsync<T>(string traceId, string analysisType, CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Lists available analysis results for a trace.
    /// </summary>
    Task<IReadOnlyList<string>> ListAnalysisResultsAsync(string traceId, CancellationToken ct = default);

    /// <summary>
    /// Checks if chunked data exists for a trace.
    /// </summary>
    Task<bool> HasChunkedDataAsync(string traceId, CancellationToken ct = default);
}
```

### 3.2 DTOs para datos chunkeados

**Archivo:** `GeneFlow.ApiNet2.Application/Traces/DTOs/TraceStorageDtos.cs`

```csharp
namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// Manifest containing metadata about stored trace chunks.
/// </summary>
public sealed record TraceManifestDto(
    string TraceId,
    string OriginalFilename,
    string Format,
    int TotalBases,
    int ChunkSize,
    int ChunkCount,
    bool HasChromatogram,
    bool HasQualityScores,
    DateTime CreatedAt,
    IReadOnlyList<ChunkMetadataDto> Chunks);

/// <summary>
/// Metadata for a single chunk.
/// </summary>
public sealed record ChunkMetadataDto(
    int Index,
    int StartPosition,
    int EndPosition,
    int BaseCount,
    string Filename);

/// <summary>
/// A chunk of trace sequence data.
/// </summary>
public sealed record TraceChunkDto(
    int Index,
    int StartPosition,
    int EndPosition,
    string Bases,
    IReadOnlyList<int>? QualityScores,
    ChromatogramChunkDto? Chromatogram);

/// <summary>
/// Chromatogram data for a chunk.
/// </summary>
public sealed record ChromatogramChunkDto(
    IReadOnlyList<int>? A,
    IReadOnlyList<int>? C,
    IReadOnlyList<int>? G,
    IReadOnlyList<int>? T);

/// <summary>
/// Paginated response for sequence data.
/// </summary>
public sealed record SequencePageDto(
    string TraceId,
    int Page,
    int PageSize,
    int TotalBases,
    int TotalPages,
    string Bases,
    IReadOnlyList<int>? QualityScores,
    ChromatogramChunkDto? Chromatogram);
```

### 3.3 Implementación con MinIO/S3

**Archivo:** `GeneFlow.ApiNet2.Infrastructure/Storage/MinIODatalakeStorageClient.cs`

```csharp
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeneFlow.ApiNet2.Infrastructure.Storage;

/// <summary>
/// MinIO/S3 implementation of IDatalakeStorageClient.
/// </summary>
public sealed class MinIODatalakeStorageClient : IDatalakeStorageClient
{
    private readonly IAmazonS3 _s3Client;
    private readonly DatalakeStorageSettings _settings;
    private readonly ILogger<MinIODatalakeStorageClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public MinIODatalakeStorageClient(
        IAmazonS3 s3Client,
        IOptions<DatalakeStorageSettings> settings,
        ILogger<MinIODatalakeStorageClient> logger)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TraceManifestDto?> GetManifestAsync(string traceId, CancellationToken ct = default)
    {
        var key = $"traces/{traceId}/manifest.json";

        try
        {
            var response = await _s3Client.GetObjectAsync(_settings.Bucket, key, ct);
            using var reader = new StreamReader(response.ResponseStream);
            var json = await reader.ReadToEndAsync(ct);
            return JsonSerializer.Deserialize<TraceManifestDto>(json, JsonOptions);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<TraceChunkDto?> GetChunkAsync(string traceId, int chunkIndex, CancellationToken ct = default)
    {
        var key = $"traces/{traceId}/chunks/chunk_{chunkIndex:D4}.json";

        try
        {
            var response = await _s3Client.GetObjectAsync(_settings.Bucket, key, ct);
            using var reader = new StreamReader(response.ResponseStream);
            var json = await reader.ReadToEndAsync(ct);
            return JsonSerializer.Deserialize<TraceChunkDto>(json, JsonOptions);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<(byte[] Data, string Extension)?> GetOriginalFileAsync(
        string traceId, CancellationToken ct = default)
    {
        var prefix = $"traces/{traceId}/original.";

        try
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _settings.Bucket,
                Prefix = prefix,
                MaxKeys = 1
            };

            var listResponse = await _s3Client.ListObjectsV2Async(listRequest, ct);
            if (listResponse.S3Objects.Count == 0)
                return null;

            var key = listResponse.S3Objects[0].Key;
            var extension = Path.GetExtension(key).TrimStart('.');

            var response = await _s3Client.GetObjectAsync(_settings.Bucket, key, ct);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, ct);

            return (ms.ToArray(), extension);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<T?> GetAnalysisResultAsync<T>(
        string traceId, string analysisType, CancellationToken ct = default) where T : class
    {
        var key = $"traces/{traceId}/analysis/{analysisType}.json";

        try
        {
            var response = await _s3Client.GetObjectAsync(_settings.Bucket, key, ct);
            using var reader = new StreamReader(response.ResponseStream);
            var json = await reader.ReadToEndAsync(ct);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<string>> ListAnalysisResultsAsync(
        string traceId, CancellationToken ct = default)
    {
        var prefix = $"traces/{traceId}/analysis/";

        var request = new ListObjectsV2Request
        {
            BucketName = _settings.Bucket,
            Prefix = prefix
        };

        var response = await _s3Client.ListObjectsV2Async(request, ct);

        return response.S3Objects
            .Select(o => Path.GetFileNameWithoutExtension(o.Key))
            .ToList();
    }

    public async Task<bool> HasChunkedDataAsync(string traceId, CancellationToken ct = default)
    {
        var manifest = await GetManifestAsync(traceId, ct);
        return manifest is not null;
    }
}
```

### 3.4 Configuración

**Archivo:** `GeneFlow.ApiNet2.Infrastructure/Storage/DatalakeStorageSettings.cs`

```csharp
namespace GeneFlow.ApiNet2.Infrastructure.Storage;

/// <summary>
/// Settings for Datalake storage connection.
/// </summary>
public sealed class DatalakeStorageSettings
{
    public const string SectionName = "DatalakeStorage";

    /// <summary>MinIO/S3 endpoint URL.</summary>
    public string EndpointUrl { get; set; } = "http://localhost:9000";

    /// <summary>Access key.</summary>
    public string AccessKey { get; set; } = "minioadmin";

    /// <summary>Secret key.</summary>
    public string SecretKey { get; set; } = "minioadmin";

    /// <summary>Bucket name.</summary>
    public string Bucket { get; set; } = "geneflow-traces";

    /// <summary>Use HTTPS.</summary>
    public bool UseSSL { get; set; } = false;
}
```

### 3.5 Registrar en DI

**Archivo:** `GeneFlow.ApiNet2.Infrastructure/DependencyInjection.cs`

```csharp
// Add to AddInfrastructure method:

// Datalake Storage Client
services.Configure<DatalakeStorageSettings>(
    configuration.GetSection(DatalakeStorageSettings.SectionName));

services.AddSingleton<IAmazonS3>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<DatalakeStorageSettings>>().Value;
    var config = new AmazonS3Config
    {
        ServiceURL = settings.EndpointUrl,
        ForcePathStyle = true,
        UseHttp = !settings.UseSSL
    };
    return new AmazonS3Client(settings.AccessKey, settings.SecretKey, config);
});

services.AddScoped<IDatalakeStorageClient, MinIODatalakeStorageClient>();
```

---

## Fase 4: Endpoints Paginados en .NET

### 4.1 Query para Secuencia Paginada

**Archivo:** `GeneFlow.ApiNet2.Application/Traces/Queries/GetSequencePage/GetSequencePageQuery.cs`

```csharp
using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetSequencePage;

/// <summary>
/// Query to get a paginated chunk of trace sequence data.
/// </summary>
public sealed record GetSequencePageQuery(
    string TraceId,
    int Page,
    int? PageSize = null,
    bool IncludeQuality = true,
    bool IncludeChromatogram = false
) : IQuery<Result<SequencePageDto>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
    StudyRole? IRequireTraceAccess.MinimumRole => null; // Allow all members
}
```

**Archivo:** `GeneFlow.ApiNet2.Application/Traces/Queries/GetSequencePage/GetSequencePageQueryHandler.cs`

```csharp
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetSequencePage;

public sealed class GetSequencePageQueryHandler
    : IQueryHandler<GetSequencePageQuery, Result<SequencePageDto>>
{
    private readonly IDatalakeStorageClient _storageClient;
    private const int DefaultPageSize = 10000;

    public GetSequencePageQueryHandler(IDatalakeStorageClient storageClient)
    {
        _storageClient = storageClient;
    }

    public async Task<Result<SequencePageDto>> Handle(
        GetSequencePageQuery request,
        CancellationToken cancellationToken)
    {
        // Get manifest to know total chunks
        var manifest = await _storageClient.GetManifestAsync(request.TraceId, cancellationToken);
        if (manifest is null)
            return Result.Failure<SequencePageDto>(TraceErrors.ChunkedDataNotAvailable);

        var pageSize = request.PageSize ?? manifest.ChunkSize;
        var totalPages = manifest.ChunkCount;

        if (request.Page < 0 || request.Page >= totalPages)
            return Result.Failure<SequencePageDto>(TraceErrors.InvalidPageNumber);

        // Get the requested chunk
        var chunk = await _storageClient.GetChunkAsync(request.TraceId, request.Page, cancellationToken);
        if (chunk is null)
            return Result.Failure<SequencePageDto>(TraceErrors.ChunkNotFound);

        return Result.Success(new SequencePageDto(
            TraceId: request.TraceId,
            Page: request.Page,
            PageSize: pageSize,
            TotalBases: manifest.TotalBases,
            TotalPages: totalPages,
            Bases: chunk.Bases,
            QualityScores: request.IncludeQuality ? chunk.QualityScores : null,
            Chromatogram: request.IncludeChromatogram ? chunk.Chromatogram : null
        ));
    }
}
```

### 4.2 Query para Resultados de Análisis

**Archivo:** `GeneFlow.ApiNet2.Application/Traces/Queries/GetAnalysisResult/GetAnalysisResultQuery.cs`

```csharp
using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetAnalysisResult;

/// <summary>
/// Query to get analysis results for a trace.
/// </summary>
public sealed record GetAnalysisResultQuery(
    string TraceId,
    string AnalysisType
) : IQuery<Result<object>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
    StudyRole? IRequireTraceAccess.MinimumRole => null;
}
```

### 4.3 Endpoints

**Archivo:** `GeneFlow.ApiNet2.API/Endpoints/Traces/TraceSequenceEndpoints.cs`

```csharp
using GeneFlow.ApiNet2.Application.Traces.Queries.GetSequencePage;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetAnalysisResult;
using GeneFlow.ApiNet2.API.Extensions;
using MediatR;

namespace GeneFlow.ApiNet2.API.Endpoints.Traces;

public static class TraceSequenceEndpoints
{
    public static void MapTraceSequenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/traces/{traceId}")
            .WithTags("Trace Sequence")
            .RequireAuthorization();

        // GET /api/v1/traces/{traceId}/sequence?page=0&includeQuality=true&includeChromatogram=false
        group.MapGet("/sequence", async (
            string traceId,
            int page,
            int? pageSize,
            bool includeQuality,
            bool includeChromatogram,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetSequencePageQuery(
                traceId, page, pageSize, includeQuality, includeChromatogram);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        })
        .WithName("GetSequencePage")
        .WithSummary("Get a paginated chunk of trace sequence data")
        .Produces<SequencePageDto>(200)
        .Produces(404);

        // GET /api/v1/traces/{traceId}/manifest
        group.MapGet("/manifest", async (
            string traceId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetTraceManifestQuery(traceId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        })
        .WithName("GetTraceManifest")
        .WithSummary("Get trace manifest with chunk information")
        .Produces<TraceManifestDto>(200)
        .Produces(404);

        // GET /api/v1/traces/{traceId}/analysis/{analysisType}
        group.MapGet("/analysis/{analysisType}", async (
            string traceId,
            string analysisType,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetAnalysisResultQuery(traceId, analysisType);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        })
        .WithName("GetAnalysisResult")
        .WithSummary("Get stored analysis result for a trace")
        .Produces<object>(200)
        .Produces(404);

        // GET /api/v1/traces/{traceId}/analysis
        group.MapGet("/analysis", async (
            string traceId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new ListAnalysisResultsQuery(traceId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        })
        .WithName("ListAnalysisResults")
        .WithSummary("List available analysis results for a trace")
        .Produces<IReadOnlyList<string>>(200);
    }
}
```

---

## Fase 5: Almacenar Resultados de Análisis

### 5.1 Modificar Analysis Worker para publicar resultados

**Archivo:** `geneflow-analysis/src/workers/analysis.py`

Cada analyzer debe publicar un evento `AnalysisResultStored` después de completar:

```python
async def _handle_trimming(self, job: AnalysisJob) -> None:
    """Handle trimming analysis."""
    trimmer = TrimmingAnalyzer(
        algorithm=job.options.get("algorithm", "modified_mott"),
        quality_threshold=job.options.get("quality_threshold", 20),
        window_size=job.options.get("window_size", 10),
    )

    result = trimmer.analyze(job.sequence, job.quality)

    # Publicar evento de resultado completado
    await self._publisher.publish(TrimmingCompleted(
        traceId=job.traceId,
        algorithm=result.algorithm,
        originalLength=result.original_length,
        trimmedLength=result.trimmed_length,
        trimStart=result.trim_start,
        trimEnd=result.trim_end,
    ))

    # NUEVO: Publicar evento para almacenar en datalake
    await self._publisher.publish(AnalysisResultStored(
        traceId=job.traceId,
        analysisType="trimming",
        resultData={
            "algorithm": result.algorithm,
            "original_length": result.original_length,
            "trimmed_length": result.trimmed_length,
            "trim_start": result.trim_start,
            "trim_end": result.trim_end,
            "trimmed_sequence": result.trimmed_sequence,
            "quality_before": result.quality_before,
            "quality_after": result.quality_after,
        }
    ))
```

---

## Fase 6: Configuración

### 6.1 appsettings.json

```json
{
  "DatalakeStorage": {
    "EndpointUrl": "http://localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "Bucket": "geneflow-traces",
    "UseSSL": false
  }
}
```

### 6.2 docker-compose.yml

```yaml
services:
  minio:
    image: minio/minio:latest
    ports:
      - "9000:9000"
      - "9001:9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
    command: server /data --console-address ":9001"
    volumes:
      - minio-data:/data
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]
      interval: 30s
      timeout: 20s
      retries: 3

volumes:
  minio-data:
```

---

## Orden de Implementación

### Sprint 1: Infraestructura (3 días)

| # | Tarea | Archivos | Estimación |
|---|-------|----------|------------|
| 1.1 | Crear DTOs de storage | `TraceStorageDtos.cs` | 2h |
| 1.2 | Crear interfaz `IDatalakeStorageClient` | Interface + settings | 2h |
| 1.3 | Implementar `MinIODatalakeStorageClient` | Implementation | 4h |
| 1.4 | Registrar en DI + configuración | `DependencyInjection.cs`, appsettings | 1h |
| 1.5 | Agregar AWSSDK.S3 a Infrastructure | `.csproj` | 0.5h |

### Sprint 2: Queries y Endpoints (2 días)

| # | Tarea | Archivos | Estimación |
|---|-------|----------|------------|
| 2.1 | `GetSequencePageQuery` + Handler | Query + Handler | 3h |
| 2.2 | `GetTraceManifestQuery` + Handler | Query + Handler | 2h |
| 2.3 | `GetAnalysisResultQuery` + Handler | Query + Handler | 2h |
| 2.4 | `ListAnalysisResultsQuery` + Handler | Query + Handler | 1h |
| 2.5 | `TraceSequenceEndpoints` | Endpoints | 2h |
| 2.6 | Agregar errores de dominio | `TraceErrors.cs` | 0.5h |

### Sprint 3: Analysis Worker (2 días)

| # | Tarea | Archivos | Estimación |
|---|-------|----------|------------|
| 3.1 | Actualizar `TraceProcessed` event | `events.py` | 1h |
| 3.2 | Crear `AnalysisResultStored` event | `events.py` | 1h |
| 3.3 | Modificar `TraceWorker` | `trace.py` | 2h |
| 3.4 | Modificar `AnalysisWorker` (7 analyzers) | `analysis.py` | 4h |

### Sprint 4: StorageMounter (1 día)

| # | Tarea | Archivos | Estimación |
|---|-------|----------|------------|
| 4.1 | Agregar `handle_processed` | `trace_handler.py` | 2h |
| 4.2 | Agregar `handle_analysis_result` | `trace_handler.py` | 2h |
| 4.3 | Actualizar `StorageMounter` | `mounter.py` | 1h |

### Sprint 5: Testing E2E (2 días)

| # | Tarea | Descripción | Estimación |
|---|-------|-------------|------------|
| 5.1 | Setup MinIO local | Docker compose | 1h |
| 5.2 | Test upload → process → chunk | E2E flow | 3h |
| 5.3 | Test paginated reads | API tests | 2h |
| 5.4 | Test analysis results | API tests | 2h |

---

## Criterios de Aceptación

### Funcionales

- [ ] Upload de traza genera chunks en datalake storage
- [ ] `GET /traces/{id}/manifest` devuelve metadata de chunks
- [ ] `GET /traces/{id}/sequence?page=N` devuelve chunk específico
- [ ] Análisis genera resultados almacenados en datalake
- [ ] `GET /traces/{id}/analysis/{type}` devuelve resultado guardado
- [ ] Frontend puede cargar trazas grandes sin problemas de memoria

### No Funcionales

- [ ] Chunk size configurable (default: 10,000 bases)
- [ ] Latencia de lectura de chunk < 100ms
- [ ] Soporte para trazas de hasta 100,000 bases
- [ ] Compresión opcional para chromatogram

---

## Dependencias de Paquetes

### .NET Backend

```xml
<!-- GeneFlow.ApiNet2.Infrastructure.csproj -->
<PackageReference Include="AWSSDK.S3" Version="3.7.*" />
```

### Python (ya instaladas)

- `aiobotocore` - Ya presente en datalake
- `aiofiles` - Ya presente

---

*Documento creado: 2026-04-22*
*Versión: 1.0*

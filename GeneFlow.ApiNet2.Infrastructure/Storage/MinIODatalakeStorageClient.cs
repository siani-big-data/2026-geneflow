using System.Text.Json;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace GeneFlow.ApiNet2.Infrastructure.Storage;

/// <summary>
/// MinIO implementation of IDatalakeStorageClient.
/// Reads trace data (chunks, manifests, analysis results) from object storage.
/// </summary>
public sealed class MinIODatalakeStorageClient : IDatalakeStorageClient
{
    private readonly IMinioClient _minioClient;
    private readonly DatalakeStorageSettings _settings;
    private readonly ILogger<MinIODatalakeStorageClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public MinIODatalakeStorageClient(
        IMinioClient minioClient,
        IOptions<DatalakeStorageSettings> settings,
        ILogger<MinIODatalakeStorageClient> logger)
    {
        _minioClient = minioClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TraceManifestDto?> GetManifestAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        var key = $"traces/{traceId}/manifest.json";

        try
        {
            var json = await GetObjectAsStringAsync(key, cancellationToken);
            if (json is null) return null;

            return JsonSerializer.Deserialize<TraceManifestDto>(json, JsonOptions);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO error reading manifest for trace {TraceId}", traceId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid manifest JSON for trace {TraceId}", traceId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TraceChunkDto?> GetChunkAsync(
        string traceId,
        int chunkIndex,
        CancellationToken cancellationToken = default)
    {
        var key = $"traces/{traceId}/chunks/chunk_{chunkIndex:D4}.json";

        try
        {
            var json = await GetObjectAsStringAsync(key, cancellationToken);
            if (json is null) return null;

            return JsonSerializer.Deserialize<TraceChunkDto>(json, JsonOptions);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO error reading chunk {ChunkIndex} for trace {TraceId}", chunkIndex, traceId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid chunk JSON {ChunkIndex} for trace {TraceId}", chunkIndex, traceId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<(byte[] Data, string Extension)?> GetOriginalFileAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        var prefix = $"traces/{traceId}/original.";

        try
        {
            string? foundKey = null;
            var listArgs = new ListObjectsArgs()
                .WithBucket(_settings.Bucket)
                .WithPrefix(prefix);

            await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs, cancellationToken))
            {
                foundKey = item.Key;
                break;
            }

            if (foundKey is null)
            {
                _logger.LogDebug("Original file not found for trace {TraceId}", traceId);
                return null;
            }

            var extension = Path.GetExtension(foundKey).TrimStart('.');

            using var ms = new MemoryStream();
            var getArgs = new GetObjectArgs()
                .WithBucket(_settings.Bucket)
                .WithObject(foundKey)
                .WithCallbackStream(stream => stream.CopyTo(ms));

            await _minioClient.GetObjectAsync(getArgs, cancellationToken);

            return (ms.ToArray(), extension);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO error retrieving original file for trace {TraceId}", traceId);
            return null;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error retrieving original file for trace {TraceId}", traceId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetAnalysisResultAsync<T>(
        string traceId,
        string analysisType,
        CancellationToken cancellationToken = default) where T : class
    {
        var json = await GetAnalysisResultJsonAsync(traceId, analysisType, cancellationToken);
        if (json is null) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize analysis result {AnalysisType} for trace {TraceId}",
                analysisType, traceId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetAnalysisResultJsonAsync(
        string traceId,
        string analysisType,
        CancellationToken cancellationToken = default)
    {
        var key = $"traces/{traceId}/analysis/{analysisType}.json";
        return await GetObjectAsStringAsync(key, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListAnalysisResultsAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        var prefix = $"traces/{traceId}/analysis/";
        var results = new List<string>();

        try
        {
            var args = new ListObjectsArgs()
                .WithBucket(_settings.Bucket)
                .WithPrefix(prefix);

            await foreach (var item in _minioClient.ListObjectsEnumAsync(args, cancellationToken))
            {
                var name = Path.GetFileNameWithoutExtension(item.Key);
                if (!string.IsNullOrEmpty(name))
                {
                    results.Add(name);
                }
            }

            return results;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO error listing analysis results for trace {TraceId}", traceId);
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasChunkedDataAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        var key = $"traces/{traceId}/manifest.json";
        return await ObjectExistsAsync(key, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new BucketExistsArgs().WithBucket(_settings.Bucket);
            await _minioClient.BucketExistsAsync(args, cancellationToken);
            return true;
        }
        catch (MinioException ex)
        {
            _logger.LogWarning(ex, "MinIO datalake storage health check failed");
            return false;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "I/O error during datalake storage health check");
            return false;
        }
    }

    private async Task<string?> GetObjectAsStringAsync(
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            using var ms = new MemoryStream();
            var args = new GetObjectArgs()
                .WithBucket(_settings.Bucket)
                .WithObject(key)
                .WithCallbackStream(stream => stream.CopyTo(ms));

            await _minioClient.GetObjectAsync(args, cancellationToken);

            ms.Position = 0;
            using var reader = new StreamReader(ms);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch (ObjectNotFoundException)
        {
            _logger.LogDebug("Object not found: {Key}", key);
            return null;
        }
    }

    private async Task<bool> ObjectExistsAsync(
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(_settings.Bucket)
                .WithObject(key);

            await _minioClient.StatObjectAsync(args, cancellationToken);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }
}

using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace GeneFlow.ApiNet2.Infrastructure.Storage.Services;

/// <summary>
/// MinIO implementation of IFileStorageService.
/// Stores and retrieves files from object storage.
/// </summary>
public sealed class MinIOFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly DatalakeStorageSettings _settings;
    private readonly ILogger<MinIOFileStorageService> _logger;

    public MinIOFileStorageService(
        IMinioClient minioClient,
        IOptions<DatalakeStorageSettings> settings,
        ILogger<MinIOFileStorageService> logger)
    {
        _minioClient = minioClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> StoreFileAsync(
        string base64Data,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var data = Convert.FromBase64String(base64Data);
        return await StoreFileAsync(data, relativePath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> StoreFileAsync(
        byte[] data,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(data);
        return await StoreFileAsync(stream, relativePath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> StoreFileAsync(
        Stream stream,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var key = NormalizePath(relativePath);

        try
        {
            // Ensure bucket exists
            await EnsureBucketExistsAsync(cancellationToken);

            var args = new PutObjectArgs()
                .WithBucket(_settings.Bucket)
                .WithObject(key)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length);

            await _minioClient.PutObjectAsync(args, cancellationToken);

            _logger.LogDebug("File stored successfully: {Key}", key);

            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store file: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var key = NormalizePath(relativePath);

        try
        {
            var args = new RemoveObjectArgs()
                .WithBucket(_settings.Bucket)
                .WithObject(key);

            await _minioClient.RemoveObjectAsync(args, cancellationToken);

            _logger.LogDebug("File deleted: {Key}", key);
        }
        catch (ObjectNotFoundException)
        {
            _logger.LogDebug("File not found for deletion: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var key = NormalizePath(relativePath);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check file existence: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetFileAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var key = NormalizePath(relativePath);

        try
        {
            using var ms = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(_settings.Bucket)
                .WithObject(key)
                .WithCallbackStream(stream => stream.CopyTo(ms));

            await _minioClient.GetObjectAsync(args, cancellationToken);

            _logger.LogDebug("File retrieved: {Key}, size: {Size} bytes", key, ms.Length);

            return ms.ToArray();
        }
        catch (ObjectNotFoundException)
        {
            _logger.LogDebug("File not found: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file: {Key}", key);
            return null;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var args = new BucketExistsArgs().WithBucket(_settings.Bucket);
        var exists = await _minioClient.BucketExistsAsync(args, cancellationToken);

        if (!exists)
        {
            var makeArgs = new MakeBucketArgs().WithBucket(_settings.Bucket);
            await _minioClient.MakeBucketAsync(makeArgs, cancellationToken);
            _logger.LogInformation("Created bucket: {Bucket}", _settings.Bucket);
        }
    }

    private static string NormalizePath(string path)
    {
        return path.TrimStart('/').Replace('\\', '/');
    }
}

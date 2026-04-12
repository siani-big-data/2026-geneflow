using GeneFlow.ApiNet2.Infrastructure.Storage.Configuration;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeneFlow.ApiNet2.Infrastructure.Storage.Services;

/// <summary>
/// Local file system implementation of file storage for development and single-server deployments.
/// Files are stored on disk and served via the /storage endpoint.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly StorageSettings _settings;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _basePath;

    /// <summary>
    /// Initializes a new instance of the LocalFileStorageService.
    /// </summary>
    public LocalFileStorageService(
        IOptions<StorageSettings> settings,
        ILogger<LocalFileStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _basePath = Path.GetFullPath(_settings.BasePath);

        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created storage directory at {Path}", _basePath);
        }
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
        // Normalize path separators
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, normalizedPath);

        // Ensure the directory exists
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write the file
        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await stream.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation(
            "Stored file at {Path}, size: {Size} bytes",
            fullPath,
            fileStream.Length);

        // Return the URL path
        return $"{_settings.BaseUrl}/{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, normalizedPath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file at {Path}", fullPath);

            // Clean up empty directories
            var directory = Path.GetDirectoryName(fullPath);
            CleanupEmptyDirectories(directory);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, normalizedPath);
        return Task.FromResult(File.Exists(fullPath));
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, normalizedPath);

        if (!File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    /// <summary>
    /// Recursively removes empty directories up to the base path.
    /// </summary>
    private void CleanupEmptyDirectories(string? directory)
    {
        while (!string.IsNullOrEmpty(directory) &&
               directory.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase) &&
               directory != _basePath)
        {
            try
            {
                if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                    directory = Path.GetDirectoryName(directory);
                }
                else
                {
                    break;
                }
            }
            catch
            {
                break;
            }
        }
    }
}

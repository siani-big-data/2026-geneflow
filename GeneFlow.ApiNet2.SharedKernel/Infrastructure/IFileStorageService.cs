namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure;

/// <summary>
/// Service for storing and retrieving files in the datalake storage.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Stores a file from base64 data.
    /// </summary>
    /// <param name="base64Data">The file content as base64 string.</param>
    /// <param name="relativePath">The relative path where to store the file (e.g., "profiles/P00000001/photo.jpg").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full URL path to access the stored file.</returns>
    Task<string> StoreFileAsync(string base64Data, string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a file from byte array.
    /// </summary>
    /// <param name="data">The file content as byte array.</param>
    /// <param name="relativePath">The relative path where to store the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full URL path to access the stored file.</returns>
    Task<string> StoreFileAsync(byte[] data, string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a file from a stream.
    /// </summary>
    /// <param name="stream">The file content stream.</param>
    /// <param name="relativePath">The relative path where to store the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full URL path to access the stored file.</returns>
    Task<string> StoreFileAsync(Stream stream, string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    /// <param name="relativePath">The relative path of the file to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in storage.
    /// </summary>
    /// <param name="relativePath">The relative path of the file to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    Task<bool> FileExistsAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file content as byte array.
    /// </summary>
    /// <param name="relativePath">The relative path of the file to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content as byte array, or null if not found.</returns>
    Task<byte[]?> GetFileAsync(string relativePath, CancellationToken cancellationToken = default);
}

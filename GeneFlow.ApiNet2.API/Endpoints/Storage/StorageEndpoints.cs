using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Storage;

/// <summary>
/// Serves files stored in object storage (MinIO) under the /storage path.
/// Acts as a proxy so the frontend can use URLs like /storage/profiles/{id}/photo.jpg
/// without exposing MinIO directly.
/// </summary>
public sealed class StorageEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Profile photos and thumbnails: /storage/profiles/{profileId}/{filename}
        // Maps to MinIO key: profiles/{profileId}/{filename}
        app.MapGet("/storage/profiles/{profileId}/{filename}", GetProfileFile)
            .WithName("Storage_GetProfileFile")
            .WithTags("Storage")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ExcludeFromDescription();
    }

    private static async Task<IResult> GetProfileFile(
        [FromRoute] string profileId,
        [FromRoute] string filename,
        [FromServices] IFileStorageService storage,
        CancellationToken cancellationToken)
    {
        var key = $"profiles/{profileId}/{filename}";
        var bytes = await storage.GetFileAsync(key, cancellationToken);

        if (bytes is null)
            return Results.NotFound();

        var contentType = GetContentType(filename);
        return Results.File(bytes, contentType);
    }

    private static string GetContentType(string filename)
    {
        var extension = Path.GetExtension(filename).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream",
        };
    }
}

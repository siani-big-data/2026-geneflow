using GeneFlow.ApiNet2.Domain.Profiles.Events;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Profiles.EventHandlers;

/// <summary>
/// Handles the ProfilePhotoDeletedEvent by removing the photo from datalake storage.
/// </summary>
/// <remarks>
/// Exceptions are intentionally caught and logged without rethrowing because
/// storage cleanup failures must not block the user-facing deletion flow.
/// </remarks>
public sealed class ProfilePhotoDeletedEventHandler
    : INotificationHandler<ProfilePhotoDeletedEvent>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<ProfilePhotoDeletedEventHandler> _logger;

    /// <summary>
    /// Supported image extensions probed during deletion because the original
    /// extension is not stored on the event.
    /// </summary>
    private static readonly string[] ImageExtensions = { "jpg", "jpeg", "png", "gif", "webp" };

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public ProfilePhotoDeletedEventHandler(
        IFileStorageService fileStorageService,
        ILogger<ProfilePhotoDeletedEventHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Handle(
        ProfilePhotoDeletedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing profile photo deletion for profile {ProfileId}",
            notification.ProfileId.Value);

        try
        {
            foreach (var ext in ImageExtensions)
            {
                var photoPath = $"profiles/{notification.ProfileId.Value}/photo.{ext}";
                var thumbnailPath = $"profiles/{notification.ProfileId.Value}/thumbnail.{ext}";

                if (await _fileStorageService.FileExistsAsync(photoPath, cancellationToken))
                {
                    await _fileStorageService.DeleteFileAsync(photoPath, cancellationToken);
                    _logger.LogInformation("Deleted profile photo at {Path}", photoPath);
                }

                if (await _fileStorageService.FileExistsAsync(thumbnailPath, cancellationToken))
                {
                    await _fileStorageService.DeleteFileAsync(thumbnailPath, cancellationToken);
                    _logger.LogInformation("Deleted profile thumbnail at {Path}", thumbnailPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete profile photo for {ProfileId}",
                notification.ProfileId.Value);
        }
    }
}

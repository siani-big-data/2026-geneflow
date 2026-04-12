using GeneFlow.ApiNet2.Domain.Profiles.Events;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Profiles.EventHandlers;

/// <summary>
/// Handles the ProfilePhotoUploadedEvent by storing the photo in the datalake storage.
/// </summary>
public sealed class ProfilePhotoUploadedEventHandler
    : INotificationHandler<ProfilePhotoUploadedEvent>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILogger<ProfilePhotoUploadedEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public ProfilePhotoUploadedEventHandler(
        IFileStorageService fileStorageService,
        IImageProcessingService imageProcessingService,
        ILogger<ProfilePhotoUploadedEventHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _imageProcessingService = imageProcessingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Handle(
        ProfilePhotoUploadedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing profile photo upload for profile {ProfileId}",
            notification.ProfileId.Value);

        try
        {
            // Decode the base64 photo data
            var photoData = Convert.FromBase64String(notification.PhotoData);

            // Store the original photo
            var photoPath = $"profiles/{notification.ProfileId.Value}/photo.{notification.Extension}";
            await _fileStorageService.StoreFileAsync(photoData, photoPath, cancellationToken);

            _logger.LogInformation(
                "Stored profile photo at {Path}, size: {Size} bytes",
                photoPath,
                notification.SizeBytes);

            // Create and store thumbnail
            var thumbnailData = await _imageProcessingService.CreateThumbnailAsync(
                photoData,
                width: 150,
                height: 150,
                cancellationToken);

            var thumbnailPath = $"profiles/{notification.ProfileId.Value}/thumbnail.{notification.Extension}";
            await _fileStorageService.StoreFileAsync(thumbnailData, thumbnailPath, cancellationToken);

            _logger.LogInformation(
                "Stored profile thumbnail at {Path}, size: {Size} bytes",
                thumbnailPath,
                thumbnailData.Length);
        }
        catch (FormatException ex)
        {
            _logger.LogError(
                ex,
                "Invalid base64 data for profile photo {ProfileId}",
                notification.ProfileId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to store profile photo for {ProfileId}",
                notification.ProfileId.Value);
            throw;
        }
    }
}

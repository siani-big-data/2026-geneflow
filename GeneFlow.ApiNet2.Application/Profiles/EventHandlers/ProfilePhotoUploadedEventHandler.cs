using GeneFlow.ApiNet2.Domain.Profiles.Events;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Profiles.EventHandlers;

/// <summary>
/// Handles the ProfilePhotoUploadedEvent by storing the photo locally
/// and publishing to Redis for datalake storage (MinIO).
/// </summary>
public sealed class ProfilePhotoUploadedEventHandler
    : INotificationHandler<ProfilePhotoUploadedEvent>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IEventBusPublisher _eventBusPublisher;
    private readonly ILogger<ProfilePhotoUploadedEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public ProfilePhotoUploadedEventHandler(
        IFileStorageService fileStorageService,
        IImageProcessingService imageProcessingService,
        IEventBusPublisher eventBusPublisher,
        ILogger<ProfilePhotoUploadedEventHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _imageProcessingService = imageProcessingService;
        _eventBusPublisher = eventBusPublisher;
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
            var photoData = Convert.FromBase64String(notification.PhotoData);

            var photoPath = $"profiles/{notification.ProfileId.Value}/photo.{notification.Extension}";
            await _fileStorageService.StoreFileAsync(photoData, photoPath, cancellationToken);

            _logger.LogInformation(
                "Stored profile photo at {Path}, size: {Size} bytes",
                photoPath,
                notification.SizeBytes);

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

            await _eventBusPublisher.PublishAsync(notification, "profiles", cancellationToken);
            _logger.LogInformation(
                "Published ProfilePhotoUploadedEvent to datalake for profile {ProfileId}",
                notification.ProfileId.Value);
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

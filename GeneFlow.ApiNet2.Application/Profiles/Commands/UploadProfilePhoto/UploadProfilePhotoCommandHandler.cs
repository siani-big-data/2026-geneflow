using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.Application.Profiles.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Events;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.UploadProfilePhoto;

/// <summary>
/// Handler for uploading profile photo.
/// Publishes the photo data to the datalake for storage.
/// </summary>
public sealed class UploadProfilePhotoCommandHandler
    : ICommandHandler<UploadProfilePhotoCommand, Result<ProfileDto>>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileUnitOfWork _unitOfWork;
    private readonly IEventBusPublisher _eventBusPublisher;
    private readonly ILogger<UploadProfilePhotoCommandHandler> _logger;

    private const long MaxPhotoSize = 10 * 1024 * 1024; // 10 MB
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    private static readonly Dictionary<string, string> ContentTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        { "image/jpeg", "jpg" },
        { "image/jpg", "jpg" },
        { "image/png", "png" },
        { "image/gif", "gif" },
        { "image/webp", "webp" }
    };

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public UploadProfilePhotoCommandHandler(
        IProfileRepository profileRepository,
        IProfileUnitOfWork unitOfWork,
        IEventBusPublisher eventBusPublisher,
        ILogger<UploadProfilePhotoCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _eventBusPublisher = eventBusPublisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ProfileDto>> Handle(
        UploadProfilePhotoCommand request,
        CancellationToken cancellationToken)
    {
        // Validate content type
        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            return Result.Failure<ProfileDto>(ProfileErrors.InvalidPhotoFormat);
        }

        // Validate size
        if (request.SizeBytes > MaxPhotoSize)
        {
            return Result.Failure<ProfileDto>(ProfileErrors.PhotoTooLarge);
        }

        // Validate base64 data
        if (string.IsNullOrWhiteSpace(request.PhotoDataBase64))
        {
            return Result.Failure<ProfileDto>(ProfileErrors.InvalidPhotoData);
        }

        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
        {
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);
        }

        // Get profile by user ID
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);
        }

        // Get file extension
        var extension = ContentTypeToExtension.GetValueOrDefault(request.ContentType, "jpg");

        // Generate storage URLs (based on datalake storage pattern)
        var photoUrl = $"/storage/profiles/{profile.Id.Value}/photo.{extension}";
        var thumbnailUrl = $"/storage/profiles/{profile.Id.Value}/thumbnail.{extension}";

        // Create ProfilePhoto value object with the URLs
        var photoResult = ProfilePhoto.Create(photoUrl, thumbnailUrl, request.SizeBytes);
        if (photoResult.IsFailure)
        {
            return Result.Failure<ProfileDto>(photoResult.Error);
        }

        // Update profile with photo URLs
        var updateResult = profile.UpdatePhoto(photoResult.Value);
        if (updateResult.IsFailure)
        {
            return Result.Failure<ProfileDto>(updateResult.Error);
        }

        // Persist profile changes
        _profileRepository.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event to datalake for binary storage
        var uploadEvent = new ProfilePhotoUploadedEvent(
            profile.Id,
            request.PhotoDataBase64,
            extension,
            request.ContentType,
            request.SizeBytes);

        await _eventBusPublisher.PublishAsync(uploadEvent, "profiles", cancellationToken);

        _logger.LogInformation(
            "Profile photo uploaded for profile {ProfileId}, size: {Size} bytes",
            profile.Id.Value,
            request.SizeBytes);

        return profile.ToDto();
    }
}

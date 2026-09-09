using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.Application.Profiles.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Events;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.UploadProfilePhoto;

/// <summary>
/// Handler for uploading a profile photo. Validates the payload, updates the profile
/// aggregate with the new photo URLs and publishes a domain event so that the
/// underlying file storage subscriber can persist the binary content asynchronously.
/// </summary>
public sealed class UploadProfilePhotoCommandHandler
    : ICommandHandler<UploadProfilePhotoCommand, Result<ProfileDto>>
{
    private const int BytesPerKilobyte = 1024;
    private const int KilobytesPerMegabyte = 1024;
    private const int MaxPhotoSizeMegabytes = 10;
    private const long MaxPhotoSizeBytes = MaxPhotoSizeMegabytes * KilobytesPerMegabyte * BytesPerKilobyte;

    private const string DefaultPhotoExtension = "jpg";
    private const string PhotoStoragePathTemplate = "/storage/profiles/{0}/photo.{1}";
    private const string ThumbnailStoragePathTemplate = "/storage/profiles/{0}/thumbnail.{1}";

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "image/webp",
    };

    private static readonly Dictionary<string, string> ContentTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        { "image/jpeg", "jpg" },
        { "image/jpg", "jpg" },
        { "image/png", "png" },
        { "image/gif", "gif" },
        { "image/webp", "webp" },
    };

    private readonly IProfileRepository _profileRepository;
    private readonly IProfileUnitOfWork _unitOfWork;
    private readonly IPublisher _mediatorPublisher;
    private readonly ILogger<UploadProfilePhotoCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadProfilePhotoCommandHandler"/> class.
    /// </summary>
    public UploadProfilePhotoCommandHandler(
        IProfileRepository profileRepository,
        IProfileUnitOfWork unitOfWork,
        IPublisher mediatorPublisher,
        ILogger<UploadProfilePhotoCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _mediatorPublisher = mediatorPublisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ProfileDto>> Handle(
        UploadProfilePhotoCommand request,
        CancellationToken cancellationToken)
    {
        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            return Result.Failure<ProfileDto>(ProfileErrors.InvalidPhotoFormat);
        }

        if (request.SizeBytes > MaxPhotoSizeBytes)
        {
            return Result.Failure<ProfileDto>(ProfileErrors.PhotoTooLarge);
        }

        if (string.IsNullOrWhiteSpace(request.PhotoDataBase64))
        {
            return Result.Failure<ProfileDto>(ProfileErrors.InvalidPhotoData);
        }

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
        {
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);
        }

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);
        }

        var extension = ContentTypeToExtension.GetValueOrDefault(request.ContentType, DefaultPhotoExtension);

        var photoUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, PhotoStoragePathTemplate, profile.Id.Value, extension);
        var thumbnailUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, ThumbnailStoragePathTemplate, profile.Id.Value, extension);

        var photoResult = ProfilePhoto.Create(photoUrl, thumbnailUrl, request.SizeBytes);
        if (photoResult.IsFailure)
        {
            return Result.Failure<ProfileDto>(photoResult.Error);
        }

        var updateResult = profile.UpdatePhoto(photoResult.Value);
        if (updateResult.IsFailure)
        {
            return Result.Failure<ProfileDto>(updateResult.Error);
        }

        _profileRepository.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var uploadEvent = new ProfilePhotoUploadedEvent(
            profile.Id,
            request.PhotoDataBase64,
            extension,
            request.ContentType,
            request.SizeBytes);

        await _mediatorPublisher.Publish(uploadEvent, cancellationToken);

        _logger.LogInformation(
            "Profile photo uploaded for profile {ProfileId}, size: {Size} bytes",
            profile.Id.Value,
            request.SizeBytes);

        return profile.ToDto();
    }
}

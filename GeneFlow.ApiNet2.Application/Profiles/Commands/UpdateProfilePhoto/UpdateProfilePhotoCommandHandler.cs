using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.Application.Profiles.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.UpdateProfilePhoto;

/// <summary>
/// Handler for updating profile photo.
/// </summary>
public sealed class UpdateProfilePhotoCommandHandler
    : ICommandHandler<UpdateProfilePhotoCommand, Result<ProfileDto>>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public UpdateProfilePhotoCommandHandler(
        IProfileRepository profileRepository,
        IProfileUnitOfWork unitOfWork)
    {
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<ProfileDto>> Handle(
        UpdateProfilePhotoCommand request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);

        // Get profile by user ID
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);

        // Create ProfilePhoto value object
        var photoResult = ProfilePhoto.Create(request.PhotoUrl, request.ThumbnailUrl, request.SizeBytes);
        if (photoResult.IsFailure)
            return Result.Failure<ProfileDto>(photoResult.Error);

        // Update profile
        var updateResult = profile.UpdatePhoto(photoResult.Value);
        if (updateResult.IsFailure)
            return Result.Failure<ProfileDto>(updateResult.Error);

        // Persist
        _profileRepository.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }
}

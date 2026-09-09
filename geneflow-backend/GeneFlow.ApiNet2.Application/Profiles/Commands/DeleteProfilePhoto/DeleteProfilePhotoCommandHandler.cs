using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.DeleteProfilePhoto;

/// <summary>
/// Handler for deleting profile photo.
/// </summary>
public sealed class DeleteProfilePhotoCommandHandler
    : ICommandHandler<DeleteProfilePhotoCommand, Result>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileUnitOfWork _unitOfWork;
    private readonly IPublisher _mediatorPublisher;
    private readonly ILogger<DeleteProfilePhotoCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public DeleteProfilePhotoCommandHandler(
        IProfileRepository profileRepository,
        IProfileUnitOfWork unitOfWork,
        IPublisher mediatorPublisher,
        ILogger<DeleteProfilePhotoCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _mediatorPublisher = mediatorPublisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(
        DeleteProfilePhotoCommand request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(ProfileErrors.NotFound);

        // Get profile by user ID
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result.Failure(ProfileErrors.NotFound);

        // Remove photo
        var removeResult = profile.RemovePhoto();
        if (removeResult.IsFailure)
            return removeResult;

        // Persist
        _profileRepository.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event via MediatR to trigger storage cleanup handler
        var deleteEvent = new ProfilePhotoDeletedEvent(profile.Id);
        await _mediatorPublisher.Publish(deleteEvent, cancellationToken);

        _logger.LogInformation("Profile photo deleted for profile {ProfileId}", profile.Id.Value);

        return Result.Success();
    }
}

using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
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
    private readonly IEventBusPublisher _eventBusPublisher;
    private readonly ILogger<DeleteProfilePhotoCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public DeleteProfilePhotoCommandHandler(
        IProfileRepository profileRepository,
        IProfileUnitOfWork unitOfWork,
        IEventBusPublisher eventBusPublisher,
        ILogger<DeleteProfilePhotoCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _eventBusPublisher = eventBusPublisher;
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

        // Publish event to datalake for storage cleanup
        var deleteEvent = new ProfilePhotoDeletedEvent(profile.Id);
        await _eventBusPublisher.PublishAsync(deleteEvent, "profiles", cancellationToken);

        _logger.LogInformation("Profile photo deleted for profile {ProfileId}", profile.Id.Value);

        return Result.Success();
    }
}

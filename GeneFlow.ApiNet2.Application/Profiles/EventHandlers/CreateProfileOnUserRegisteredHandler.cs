using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Profiles.EventHandlers;

/// <summary>
/// Creates a profile automatically when a new user is registered.
/// </summary>
public sealed class CreateProfileOnUserRegisteredHandler
    : IDomainEventHandler<UserRegisteredEvent>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileUnitOfWork _unitOfWork;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly ILogger<CreateProfileOnUserRegisteredHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public CreateProfileOnUserRegisteredHandler(
        IProfileRepository profileRepository,
        IProfileUnitOfWork unitOfWork,
        ISequenceGenerator sequenceGenerator,
        ILogger<CreateProfileOnUserRegisteredHandler> logger)
    {
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _sequenceGenerator = sequenceGenerator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating profile for newly registered user {UserId} ({Username})",
            notification.UserId,
            notification.Username);

        // Check if profile already exists (idempotency)
        if (await _profileRepository.ExistsForUserAsync(notification.UserId, cancellationToken))
        {
            _logger.LogWarning(
                "Profile already exists for user {UserId}, skipping creation",
                notification.UserId);
            return;
        }

        // Create PersonName using username as first name
        var nameResult = PersonName.Create(notification.Username);
        if (nameResult.IsFailure)
        {
            _logger.LogError(
                "Failed to create PersonName for user {UserId}: {Error}",
                notification.UserId,
                nameResult.Error.Message);
            return;
        }

        // Generate ProfileId
        var sequence = await _sequenceGenerator.NextAsync(ProfileId.SequenceName, cancellationToken);
        var profileId = ProfileId.FromSequence(sequence);

        // Create Profile
        var profileResult = Profile.Create(profileId, notification.UserId, nameResult.Value);
        if (profileResult.IsFailure)
        {
            _logger.LogError(
                "Failed to create Profile for user {UserId}: {Error}",
                notification.UserId,
                profileResult.Error.Message);
            return;
        }

        // Persist
        await _profileRepository.AddAsync(profileResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Profile {ProfileId} created successfully for user {UserId}",
            profileId,
            notification.UserId);
    }
}

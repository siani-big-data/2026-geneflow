using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Profiles.EventHandlers;

/// <summary>
/// Creates a profile automatically when a new user is registered via OAuth.
/// </summary>
public sealed class CreateProfileOnOAuthUserRegisteredHandler
    : IDomainEventHandler<UserRegisteredViaOAuthEvent>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileUnitOfWork _unitOfWork;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly ILogger<CreateProfileOnOAuthUserRegisteredHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public CreateProfileOnOAuthUserRegisteredHandler(
        IProfileRepository profileRepository,
        IProfileUnitOfWork unitOfWork,
        ISequenceGenerator sequenceGenerator,
        ILogger<CreateProfileOnOAuthUserRegisteredHandler> logger)
    {
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _sequenceGenerator = sequenceGenerator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Handle(UserRegisteredViaOAuthEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating profile for OAuth registered user {UserId} ({Username}) via {Provider}",
            notification.UserId,
            notification.Username,
            notification.Provider.Name);

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
            "Profile {ProfileId} created successfully for OAuth user {UserId}",
            profileId,
            notification.UserId);
    }
}

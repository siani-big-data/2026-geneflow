using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.Application.Profiles.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.CreateProfile;

/// <summary>
/// Handler for creating a new profile.
/// </summary>
public sealed class CreateProfileCommandHandler
    : ICommandHandler<CreateProfileCommand, Result<ProfileDto>>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileUnitOfWork _unitOfWork;
    private readonly ISequenceGenerator _sequenceGenerator;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public CreateProfileCommandHandler(
        IProfileRepository profileRepository,
        IProfileUnitOfWork unitOfWork,
        ISequenceGenerator sequenceGenerator)
    {
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _sequenceGenerator = sequenceGenerator;
    }

    /// <inheritdoc />
    public async Task<Result<ProfileDto>> Handle(
        CreateProfileCommand request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);

        // Check if profile already exists for this user
        if (await _profileRepository.ExistsForUserAsync(userId, cancellationToken))
            return Result.Failure<ProfileDto>(ProfileErrors.AlreadyExists);

        // Create PersonName value object
        var nameResult = PersonName.Create(request.FirstName, request.LastName);
        if (nameResult.IsFailure)
            return Result.Failure<ProfileDto>(nameResult.Error);

        // Generate ProfileId
        var sequence = await _sequenceGenerator.NextAsync(ProfileId.SequenceName, cancellationToken);
        var profileId = ProfileId.FromSequence(sequence);

        // Create Profile aggregate
        var profileResult = Profile.Create(profileId, userId, nameResult.Value);
        if (profileResult.IsFailure)
            return Result.Failure<ProfileDto>(profileResult.Error);

        var profile = profileResult.Value;

        // Persist
        await _profileRepository.AddAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }
}

using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.Application.Profiles.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Enumerations;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<CreateProfileCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public CreateProfileCommandHandler(
        IProfileRepository profileRepository,
        IProfileUnitOfWork unitOfWork,
        ISequenceGenerator sequenceGenerator,
        ILogger<CreateProfileCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _sequenceGenerator = sequenceGenerator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ProfileDto>> Handle(
        CreateProfileCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateProfile: Received request for UserId={UserId}", request.UserId);

        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
        {
            _logger.LogError("CreateProfile: Failed to parse UserId={UserId}", request.UserId);
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);
        }

        _logger.LogInformation("CreateProfile: Parsed UserId successfully, Value={Value}", userId.Value);

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

        // Create value objects for additional fields
        var bioResult = Bio.Create(request.Bio);
        if (bioResult.IsFailure)
            return Result.Failure<ProfileDto>(bioResult.Error);

        var locationResult = Location.Create(request.Location);
        if (locationResult.IsFailure)
            return Result.Failure<ProfileDto>(locationResult.Error);

        var professionalRoleResult = ProfessionalRole.Create(request.ProfessionalRole);
        if (professionalRoleResult.IsFailure)
            return Result.Failure<ProfileDto>(professionalRoleResult.Error);

        var institutionResult = Institution.Create(request.InstitutionName, request.InstitutionDepartment);
        if (institutionResult.IsFailure)
            return Result.Failure<ProfileDto>(institutionResult.Error);

        var researchField = !string.IsNullOrWhiteSpace(request.ResearchField)
            ? ResearchField.FromName(request.ResearchField)
            : null;

        // Update with additional fields
        profile.UpdateBasicInfo(
            nameResult.Value,
            bioResult.Value,
            locationResult.Value,
            professionalRoleResult.Value,
            institutionResult.Value,
            researchField);

        // Update research identifiers if provided
        if (!string.IsNullOrWhiteSpace(request.OrcidId) || !string.IsNullOrWhiteSpace(request.Website))
        {
            var identifiersResult = ResearchIdentifiers.Create(request.OrcidId, request.Website);
            if (identifiersResult.IsSuccess)
            {
                profile.UpdateResearchIdentifiers(identifiersResult.Value);
            }
        }

        // Persist
        await _profileRepository.AddAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }
}

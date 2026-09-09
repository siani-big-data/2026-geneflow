using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.Application.Profiles.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Enumerations;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.UpdateProfile;

/// <summary>
/// Handler for updating basic profile information.
/// </summary>
public sealed class UpdateProfileCommandHandler
    : ICommandHandler<UpdateProfileCommand, Result<ProfileDto>>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public UpdateProfileCommandHandler(
        IProfileRepository profileRepository,
        IProfileUnitOfWork unitOfWork)
    {
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<ProfileDto>> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);

        // Get profile by user ID
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);

        // Create value objects
        var nameResult = PersonName.Create(request.FirstName, request.LastName);
        if (nameResult.IsFailure)
            return Result.Failure<ProfileDto>(nameResult.Error);

        var bioResult = Bio.Create(request.Bio);
        if (bioResult.IsFailure)
            return Result.Failure<ProfileDto>(bioResult.Error);

        var locationResult = Location.Create(request.Location);
        if (locationResult.IsFailure)
            return Result.Failure<ProfileDto>(locationResult.Error);

        var roleResult = ProfessionalRole.Create(request.ProfessionalRole);
        if (roleResult.IsFailure)
            return Result.Failure<ProfileDto>(roleResult.Error);

        var institutionResult = Institution.Create(request.InstitutionName, request.InstitutionDepartment);
        if (institutionResult.IsFailure)
            return Result.Failure<ProfileDto>(institutionResult.Error);

        // Parse research field
        ResearchField? researchField = null;
        if (!string.IsNullOrWhiteSpace(request.ResearchField))
        {
            researchField = ResearchField.FromName(request.ResearchField);
            if (researchField is null)
                return Result.Failure<ProfileDto>(ProfileErrors.InvalidResearchField);
        }

        // Update profile
        var updateResult = profile.UpdateBasicInfo(
            nameResult.Value,
            bioResult.Value,
            locationResult.Value,
            roleResult.Value,
            institutionResult.Value,
            researchField);

        if (updateResult.IsFailure)
            return Result.Failure<ProfileDto>(updateResult.Error);

        // Persist
        _profileRepository.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }
}

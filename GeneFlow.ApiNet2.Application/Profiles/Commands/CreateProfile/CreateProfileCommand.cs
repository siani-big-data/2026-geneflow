using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.CreateProfile;

/// <summary>
/// Command to create a new profile for a user.
/// </summary>
public sealed record CreateProfileCommand(
    string UserId,
    string FirstName,
    string? LastName = null,
    string? Bio = null,
    string? Location = null,
    string? ProfessionalRole = null,
    string? InstitutionName = null,
    string? InstitutionDepartment = null,
    string? ResearchField = null,
    string? OrcidId = null,
    string? Website = null) : ICommand<Result<ProfileDto>>;

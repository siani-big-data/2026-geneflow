using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.UpdateProfile;

/// <summary>
/// Command to update basic profile information.
/// </summary>
public sealed record UpdateProfileCommand(
    string UserId,
    string FirstName,
    string? LastName,
    string? Bio,
    string? Location,
    string? ProfessionalRole,
    string? InstitutionName,
    string? InstitutionDepartment,
    string? ResearchField) : ICommand<Result<ProfileDto>>;

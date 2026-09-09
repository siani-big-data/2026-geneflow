using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Commands.UpdateResearchIdentifiers;

/// <summary>
/// Command to update research identifiers (ORCID and Website).
/// </summary>
public sealed record UpdateResearchIdentifiersCommand(
    string UserId,
    string? OrcidId,
    string? Website) : ICommand<Result<ProfileDto>>;

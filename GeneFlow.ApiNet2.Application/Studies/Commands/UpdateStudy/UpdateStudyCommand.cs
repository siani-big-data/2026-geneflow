using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.UpdateStudy;

/// <summary>
/// Command to update an existing study.
/// </summary>
public sealed record UpdateStudyCommand(
    string StudyId,
    string UserId,
    string Title,
    string? Description,
    int ResearchFieldId,
    string? Institution = null,
    string? PrincipalInvestigator = null,
    IReadOnlyList<string>? Tags = null) : ICommand<Result<StudyDto>>;

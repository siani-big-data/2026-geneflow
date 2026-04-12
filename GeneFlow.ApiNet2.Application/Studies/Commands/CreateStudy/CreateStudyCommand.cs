using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.CreateStudy;

/// <summary>
/// Command to create a new study.
/// </summary>
public sealed record CreateStudyCommand(
    string UserId,
    string Title,
    string? Description,
    int ResearchFieldId,
    string? Institution = null,
    string? PrincipalInvestigator = null,
    IReadOnlyList<string>? Tags = null) : ICommand<Result<StudyDto>>;

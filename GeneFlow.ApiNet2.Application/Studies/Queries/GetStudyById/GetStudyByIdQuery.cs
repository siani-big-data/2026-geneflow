using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyById;

/// <summary>
/// Query to get a study by ID.
/// </summary>
public sealed record GetStudyByIdQuery(
    string StudyId,
    string? UserId = null) : IQuery<Result<StudyDto>>;

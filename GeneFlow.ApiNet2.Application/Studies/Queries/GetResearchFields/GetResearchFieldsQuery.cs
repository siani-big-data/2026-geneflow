using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetResearchFields;

/// <summary>
/// Query to get all research fields.
/// </summary>
public sealed record GetResearchFieldsQuery : IQuery<Result<IReadOnlyList<ResearchFieldDto>>>;

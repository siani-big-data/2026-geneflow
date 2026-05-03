using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetStepTypes;

/// <summary>
/// Query to get available step types.
/// </summary>
public sealed record GetStepTypesQuery(
    string UserId) : IQuery<Result<IReadOnlyList<StepTypeDto>>>, IRequireAuthentication;

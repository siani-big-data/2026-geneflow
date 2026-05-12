using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetRecentUserExecutions;

/// <summary>
/// Query for retrieving the current user's most recent pipeline executions
/// across all studies they are a member of.
/// </summary>
public sealed record GetRecentUserExecutionsQuery(
    string UserId,
    int Limit = 5)
    : IQuery<Result<IReadOnlyList<RecentPipelineExecutionDto>>>,
      IRequireAuthentication;

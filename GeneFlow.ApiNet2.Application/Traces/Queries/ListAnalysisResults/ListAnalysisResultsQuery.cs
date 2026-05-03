using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.ListAnalysisResults;

/// <summary>
/// Query to list all available analysis result types for a trace.
/// </summary>
public sealed record ListAnalysisResultsQuery(
    string UserId,
    string TraceId) : IQuery<Result<IReadOnlyList<string>>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
}

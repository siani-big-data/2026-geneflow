using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetAnalysisResult;

/// <summary>
/// Query to get a specific analysis result from datalake storage.
/// Returns the raw JSON of the analysis result.
/// </summary>
public sealed record GetAnalysisResultQuery(
    string UserId,
    string TraceId,
    string AnalysisType) : IQuery<Result<string>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
}

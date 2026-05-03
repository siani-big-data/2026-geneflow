using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetSequencePage;

/// <summary>
/// Query to get a paginated section of trace sequence data from datalake storage.
/// Returns sequence bases, quality scores, and chromatogram data for the requested page.
/// </summary>
public sealed record GetSequencePageQuery(
    string UserId,
    string TraceId,
    int Page,
    int PageSize = 10000) : IQuery<Result<SequencePageDto>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
}

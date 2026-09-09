using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceManifest;

/// <summary>
/// Query to get trace manifest metadata from datalake storage.
/// Returns information about total bases, chunks, format, and available data.
/// </summary>
public sealed record GetTraceManifestQuery(
    string UserId,
    string TraceId) : IQuery<Result<TraceManifestDto>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
}

using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceTrims;

/// <summary>
/// Query to get all trims for a trace.
/// </summary>
public sealed record GetTraceTrimsQuery(
    string UserId,
    string TraceId,
    bool ActiveOnly = true) : IQuery<Result<IReadOnlyList<TraceTrimDto>>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
    StudyRole? IRequireTraceAccess.MinimumRole => StudyRole.Viewer;
}

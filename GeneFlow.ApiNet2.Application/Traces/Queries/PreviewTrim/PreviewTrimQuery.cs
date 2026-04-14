using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.PreviewTrim;

/// <summary>
/// Query to preview auto-trim results without applying them.
/// </summary>
public sealed record PreviewTrimQuery(
    string TraceId,
    int QualityThreshold = 20,
    int WindowSize = 10,
    int MinimumLength = 50) : IQuery<Result<TrimPreviewDto>>;

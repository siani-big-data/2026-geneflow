using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetSequenceEdits;

/// <summary>
/// Handler for GetSequenceEditsQuery.
/// Returns sequence edits for a trace.
/// </summary>
public sealed class GetSequenceEditsQueryHandler
    : IQueryHandler<GetSequenceEditsQuery, Result<IReadOnlyList<SequenceEditDto>>>
{
    private readonly ITraceRepository _repository;

    public GetSequenceEditsQueryHandler(ITraceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<SequenceEditDto>>> Handle(
        GetSequenceEditsQuery request,
        CancellationToken cancellationToken)
    {
        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<IReadOnlyList<SequenceEditDto>>(TraceErrors.NotFound);

        // Get trace with edits
        var trace = await _repository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<IReadOnlyList<SequenceEditDto>>(TraceErrors.NotFound);

        // Get edits
        var edits = request.IncludeInactive
            ? trace.Edits
            : trace.GetActiveEdits();

        var editDtos = edits
            .OrderBy(e => e.Position)
            .Select(e => e.ToDto())
            .ToList();

        return Result.Success<IReadOnlyList<SequenceEditDto>>(editDtos);
    }
}

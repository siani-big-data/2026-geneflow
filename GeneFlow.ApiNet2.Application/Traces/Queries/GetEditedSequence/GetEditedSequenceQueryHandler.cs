using System.Text;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetEditedSequence;

/// <summary>
/// Handler for GetEditedSequenceQuery.
/// Reads the original sequence and applies active edits.
/// </summary>
public sealed class GetEditedSequenceQueryHandler
    : IQueryHandler<GetEditedSequenceQuery, Result<EditedSequenceDto>>
{
    private readonly ITraceRepository _repository;
    private readonly ITraceAnalysisService _analysisService;

    public GetEditedSequenceQueryHandler(
        ITraceRepository repository,
        ITraceAnalysisService analysisService)
    {
        _repository = repository;
        _analysisService = analysisService;
    }

    public async Task<Result<EditedSequenceDto>> Handle(
        GetEditedSequenceQuery request,
        CancellationToken cancellationToken)
    {
        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<EditedSequenceDto>(TraceErrors.NotFound);

        // Get trace
        var trace = await _repository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<EditedSequenceDto>(TraceErrors.NotFound);

        // Get original sequence from analysis file
        var sequenceResult = await _analysisService.GetSequenceAsync(trace, cancellationToken);
        if (sequenceResult.IsFailure)
            return Result.Failure<EditedSequenceDto>(sequenceResult.Error);

        var originalSequence = sequenceResult.Value;
        var activeEdits = trace.GetActiveEdits();

        // Apply edits to get edited sequence
        var editedSequence = ApplyEdits(originalSequence, activeEdits);

        var editDtos = activeEdits.Select(e => e.ToDto()).ToList();

        return Result.Success(new EditedSequenceDto(
            traceId.Value.ToString(),
            originalSequence,
            editedSequence,
            activeEdits.Count,
            editDtos));
    }

    /// <summary>
    /// Applies edits to the original sequence.
    /// Edits are applied in reverse position order for insertions/deletions
    /// to avoid position shifting issues.
    /// </summary>
    private static string ApplyEdits(string sequence, IReadOnlyList<Domain.Traces.Entities.SequenceEdit> edits)
    {
        if (edits.Count == 0)
            return sequence;

        var sb = new StringBuilder(sequence);

        // Sort edits by position descending to apply from end to start
        // This prevents position shifting issues
        var sortedEdits = edits.OrderByDescending(e => e.Position).ToList();

        foreach (var edit in sortedEdits)
        {
            switch (edit.EditType.Id)
            {
                case 1: // Change
                    if (edit.Position >= 0 && edit.Position < sb.Length && edit.NewBase.HasValue)
                    {
                        sb[edit.Position] = edit.NewBase.Value;
                    }
                    break;

                case 2: // Insert
                    if (edit.Position >= 0 && edit.Position <= sb.Length && edit.NewBase.HasValue)
                    {
                        sb.Insert(edit.Position, edit.NewBase.Value);
                    }
                    break;

                case 3: // Delete
                    if (edit.Position >= 0 && edit.Position < sb.Length)
                    {
                        sb.Remove(edit.Position, 1);
                    }
                    break;
            }
        }

        return sb.ToString();
    }
}

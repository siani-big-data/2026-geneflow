using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.CreateSequenceEdit;

/// <summary>
/// Command to create a sequence edit on a trace.
/// </summary>
public sealed record CreateSequenceEditCommand(
    string UserId,
    string TraceId,
    int EditType,
    int Position,
    char? OriginalBase,
    char? NewBase,
    string? Reason) : ICommand<Result<SequenceEditDto>>;

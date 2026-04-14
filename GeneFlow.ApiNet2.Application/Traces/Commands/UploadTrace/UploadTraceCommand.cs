using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UploadTrace;

/// <summary>
/// Command to upload a new trace with metadata.
/// The actual file upload is handled separately.
/// </summary>
public sealed record UploadTraceCommand(
    string UserId,
    string StudyId,
    string Name,
    string? Description,
    string FileName,
    string ContentType,
    string StoragePath,
    long SizeBytes,
    string Checksum) : ICommand<Result<TraceDto>>;

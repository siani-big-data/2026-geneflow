using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UploadTrace;

/// <summary>
/// Handler for UploadTraceCommand.
/// </summary>
public sealed class UploadTraceCommandHandler
    : ICommandHandler<UploadTraceCommand, Result<TraceDto>>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public UploadTraceCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TraceDto>> Handle(
        UploadTraceCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<TraceDto>(TraceErrors.InvalidUserId);

        // Parse study ID
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<TraceDto>(TraceErrors.InvalidStudyId);

        // Create name value object
        var nameResult = TraceName.Create(request.Name);
        if (nameResult.IsFailure)
            return Result.Failure<TraceDto>(nameResult.Error);

        // Create description value object
        var descriptionResult = TraceDescription.Create(request.Description);
        if (descriptionResult.IsFailure)
            return Result.Failure<TraceDto>(descriptionResult.Error);

        // Create file value object
        var fileResult = TraceFile.Create(
            request.FileName,
            request.ContentType,
            request.StoragePath,
            request.SizeBytes,
            request.Checksum);
        if (fileResult.IsFailure)
            return Result.Failure<TraceDto>(fileResult.Error);

        // Determine format from file extension
        var extension = Path.GetExtension(request.FileName);
        var format = TraceFormat.FromExtension(extension);
        if (format is null)
            return Result.Failure<TraceDto>(TraceErrors.UnsupportedFormat);

        // Generate trace ID
        var traceId = TraceId.New();

        // Create trace
        var traceResult = Trace.Create(
            traceId,
            studyId,
            userId,
            nameResult.Value,
            descriptionResult.Value,
            fileResult.Value,
            format);

        if (traceResult.IsFailure)
            return Result.Failure<TraceDto>(traceResult.Error);

        var trace = traceResult.Value;

        // Persist
        await _unitOfWork.Traces.AddAsync(trace, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(trace.ToDto());
    }
}

using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces.Entities;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.Events;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using System.Text.Json;

namespace GeneFlow.ApiNet2.Domain.Traces;

/// <summary>
/// Trace aggregate root representing a sequencing trace file.
/// </summary>
public sealed class Trace : FullAuditableAggregateRoot<TraceId>
{
    #region Constants
    public const int MaxFailureReasonLength = 1000;
    #endregion

    #region Properties
    public StudyId StudyId { get; private set; } = null!;
    public UserId UploadedBy { get; private set; } = null!;
    public TraceName Name { get; private set; } = null!;
    public TraceDescription Description { get; private set; } = null!;
    public TraceFile File { get; private set; } = null!;
    public TraceFormat Format { get; private set; } = null!;
    public TraceStatus Status { get; private set; } = null!;
    public QualityMetrics? QualityMetrics { get; private set; }
    public TrimRegion? TrimRegion { get; private set; }
    public bool HasChromatogramData { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private readonly List<SequenceEdit> _edits = new();
    public IReadOnlyList<SequenceEdit> Edits => _edits.AsReadOnly();

    private readonly List<TraceAnnotation> _annotations = new();
    public IReadOnlyList<TraceAnnotation> Annotations => _annotations.AsReadOnly();
    #endregion

    #region Computed Properties
    public bool IsProcessed => Status == TraceStatus.Processed;
    public bool IsFailed => Status == TraceStatus.Failed;
    public bool IsArchived => Status == TraceStatus.Archived;
    public bool CanBeEdited => Status.CanEdit;
    public int ActiveEditCount => _edits.Count(e => e.IsActive);
    public int AnnotationCount => _annotations.Count;
    public int TotalBases => QualityMetrics?.TotalBases ?? 0;
    #endregion

    #region Constructors
    private Trace() : base() { }

    private Trace(
        TraceId id,
        StudyId studyId,
        UserId uploadedBy,
        TraceName name,
        TraceDescription description,
        TraceFile file,
        TraceFormat format) : base(id)
    {
        StudyId = studyId;
        UploadedBy = uploadedBy;
        Name = name;
        Description = description;
        File = file;
        Format = format;
        Status = TraceStatus.Uploaded;
        HasChromatogramData = format.HasChromatogramData;

        InitializeCreatedAt(uploadedBy.Value.ToString());
    }
    #endregion

    #region Factory Methods
    public static Result<Trace> Create(
        TraceId id,
        StudyId studyId,
        UserId uploadedBy,
        TraceName name,
        TraceDescription description,
        TraceFile file,
        TraceFormat format)
    {
        var trace = new Trace(id, studyId, uploadedBy, name, description, file, format);

        trace.RaiseDomainEvent(new TraceUploadedEvent(
            trace.Id,
            trace.StudyId,
            trace.File.FileName,
            trace.Format,
            trace.UploadedBy));

        return trace;
    }
    #endregion

    #region Name and Description Management
    public Result UpdateName(TraceName name, UserId updatedBy)
    {
        Name = name;
        SetModified(updatedBy.Value.ToString());
        return Result.Success();
    }

    public Result UpdateDescription(TraceDescription description, UserId updatedBy)
    {
        Description = description;
        SetModified(updatedBy.Value.ToString());
        return Result.Success();
    }
    #endregion

    #region Status Management
    public Result StartProcessing()
    {
        if (!Status.CanTransitionTo(TraceStatus.Validating))
            return Result.Failure(TraceErrors.InvalidStatusTransition(Status, TraceStatus.Validating));

        Status = TraceStatus.Validating;
        SetModified();

        RaiseDomainEvent(new TraceProcessingStartedEvent(Id, StudyId));

        return Result.Success();
    }

    public Result TransitionToProcessing()
    {
        if (!Status.CanTransitionTo(TraceStatus.Processing))
            return Result.Failure(TraceErrors.InvalidStatusTransition(Status, TraceStatus.Processing));

        Status = TraceStatus.Processing;
        SetModified();

        return Result.Success();
    }

    public Result CompleteProcessing(QualityMetrics qualityMetrics, bool hasChromatogramData)
    {
        if (!Status.CanTransitionTo(TraceStatus.Processed))
            return Result.Failure(TraceErrors.InvalidStatusTransition(Status, TraceStatus.Processed));

        Status = TraceStatus.Processed;
        QualityMetrics = qualityMetrics;
        HasChromatogramData = hasChromatogramData;
        ProcessedAt = DateTime.UtcNow;
        FailureReason = null;
        SetModified();

        RaiseDomainEvent(new TraceProcessedEvent(Id, StudyId, qualityMetrics.AverageQualityScore));

        return Result.Success();
    }

    public Result FailProcessing(string reason)
    {
        if (!Status.CanTransitionTo(TraceStatus.Failed))
            return Result.Failure(TraceErrors.InvalidStatusTransition(Status, TraceStatus.Failed));

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(TraceErrors.FailureReasonRequired);

        if (reason.Length > MaxFailureReasonLength)
            return Result.Failure(TraceErrors.FailureReasonTooLong(MaxFailureReasonLength));

        Status = TraceStatus.Failed;
        FailureReason = reason.Trim();
        SetModified();

        RaiseDomainEvent(new TraceProcessingFailedEvent(Id, StudyId, FailureReason));

        return Result.Success();
    }

    public Result Retry()
    {
        if (!Status.CanRetry)
            return Result.Failure(TraceErrors.CannotRetryNotFailed);

        Status = TraceStatus.Uploaded;
        FailureReason = null;
        SetModified();

        return Result.Success();
    }

    public Result Archive(UserId archivedBy)
    {
        if (!Status.CanTransitionTo(TraceStatus.Archived))
            return Result.Failure(TraceErrors.InvalidStatusTransition(Status, TraceStatus.Archived));

        Status = TraceStatus.Archived;
        SetModified(archivedBy.Value.ToString());

        RaiseDomainEvent(new TraceArchivedEvent(Id, archivedBy));

        return Result.Success();
    }
    #endregion

    #region Trimming
    public Result ApplyTrim(TrimRegion trimRegion, UserId trimmedBy)
    {
        if (!CanBeEdited)
            return Result.Failure(TraceErrors.CannotEditInCurrentStatus);

        if (TrimRegion is not null)
            return Result.Failure(TraceErrors.AlreadyTrimmed);

        TrimRegion = trimRegion;
        SetModified(trimmedBy.Value.ToString());

        RaiseDomainEvent(new TraceTrimmedEvent(
            Id,
            StudyId,
            trimRegion.End5Prime,
            trimRegion.Start3Prime,
            trimRegion.Algorithm,
            trimmedBy));

        return Result.Success();
    }

    public Result UndoTrim(UserId undoneBy)
    {
        if (!CanBeEdited)
            return Result.Failure(TraceErrors.CannotEditInCurrentStatus);

        if (TrimRegion is null)
            return Result.Failure(TraceErrors.NotTrimmed);

        TrimRegion = null;
        SetModified(undoneBy.Value.ToString());

        return Result.Success();
    }
    #endregion

    #region Sequence Editing
    public Result<SequenceEdit> AddEdit(
        EditType editType,
        int position,
        char? originalBase,
        char? newBase,
        string? reason,
        UserId editedBy)
    {
        if (!CanBeEdited)
            return Result.Failure<SequenceEdit>(TraceErrors.CannotEditInCurrentStatus);

        var sequenceLength = QualityMetrics?.TotalBases ?? 0;
        if (sequenceLength == 0)
            return Result.Failure<SequenceEdit>(TraceErrors.InvalidPosition);

        var editResult = SequenceEdit.Create(
            editType,
            position,
            originalBase,
            newBase,
            reason,
            editedBy,
            sequenceLength);

        if (editResult.IsFailure)
            return editResult;

        var edit = editResult.Value;
        _edits.Add(edit);
        SetModified(editedBy.Value.ToString());

        RaiseDomainEvent(new SequenceEditCreatedEvent(
            Id,
            StudyId,
            edit.Id,
            editType,
            position,
            editedBy));

        return edit;
    }

    public Result UndoEdit(Guid editId, UserId undoneBy)
    {
        if (!CanBeEdited)
            return Result.Failure(TraceErrors.CannotEditInCurrentStatus);

        var edit = _edits.FirstOrDefault(e => e.Id == editId);
        if (edit is null)
            return Result.Failure(TraceErrors.EditNotFound);

        var undoResult = edit.Undo();
        if (undoResult.IsFailure)
            return undoResult;

        SetModified(undoneBy.Value.ToString());

        RaiseDomainEvent(new SequenceEditUndoneEvent(Id, StudyId, editId, undoneBy));

        return Result.Success();
    }

    public Result UndoAllEdits(UserId undoneBy)
    {
        if (!CanBeEdited)
            return Result.Failure(TraceErrors.CannotEditInCurrentStatus);

        var activeEdits = _edits.Where(e => e.IsActive).ToList();
        if (activeEdits.Count == 0)
            return Result.Failure(TraceErrors.NoActiveEdits);

        foreach (var edit in activeEdits)
        {
            edit.Undo();
            RaiseDomainEvent(new SequenceEditUndoneEvent(Id, StudyId, edit.Id, undoneBy));
        }

        SetModified(undoneBy.Value.ToString());

        return Result.Success();
    }

    public IReadOnlyList<SequenceEdit> GetActiveEdits() =>
        _edits.Where(e => e.IsActive).OrderBy(e => e.Position).ToList();
    #endregion

    #region Annotations
    public Result<TraceAnnotation> AddAnnotation(
        AnnotationType type,
        string label,
        string? description,
        int startPosition,
        int endPosition,
        AnnotationStrand strand,
        string color,
        bool isShared,
        JsonDocument? metadata,
        UserId createdBy)
    {
        if (!CanBeEdited)
            return Result.Failure<TraceAnnotation>(TraceErrors.CannotEditInCurrentStatus);

        var sequenceLength = QualityMetrics?.TotalBases ?? 0;
        if (sequenceLength == 0)
            return Result.Failure<TraceAnnotation>(TraceErrors.AnnotationOutOfBounds);

        var annotationResult = TraceAnnotation.Create(
            type,
            label,
            description,
            startPosition,
            endPosition,
            strand,
            color,
            isShared,
            metadata,
            createdBy,
            sequenceLength);

        if (annotationResult.IsFailure)
            return annotationResult;

        var annotation = annotationResult.Value;
        _annotations.Add(annotation);
        SetModified(createdBy.Value.ToString());

        RaiseDomainEvent(new AnnotationCreatedEvent(
            annotation.Id,
            Id,
            StudyId,
            type,
            label,
            startPosition,
            endPosition,
            createdBy));

        return annotation;
    }

    public Result UpdateAnnotation(
        Guid annotationId,
        string label,
        string? description,
        int startPosition,
        int endPosition,
        AnnotationStrand strand,
        string color,
        bool isShared,
        JsonDocument? metadata,
        UserId updatedBy)
    {
        if (!CanBeEdited)
            return Result.Failure(TraceErrors.CannotEditInCurrentStatus);

        var annotation = _annotations.FirstOrDefault(a => a.Id == annotationId);
        if (annotation is null)
            return Result.Failure(TraceErrors.AnnotationNotFound);

        var sequenceLength = QualityMetrics?.TotalBases ?? 0;
        var updateResult = annotation.Update(
            label,
            description,
            startPosition,
            endPosition,
            strand,
            color,
            isShared,
            metadata,
            updatedBy,
            sequenceLength);

        if (updateResult.IsFailure)
            return updateResult;

        SetModified(updatedBy.Value.ToString());

        RaiseDomainEvent(new AnnotationUpdatedEvent(annotationId, Id, StudyId, updatedBy));

        return Result.Success();
    }

    public Result RemoveAnnotation(Guid annotationId, UserId deletedBy)
    {
        if (!CanBeEdited)
            return Result.Failure(TraceErrors.CannotEditInCurrentStatus);

        var annotation = _annotations.FirstOrDefault(a => a.Id == annotationId);
        if (annotation is null)
            return Result.Failure(TraceErrors.AnnotationNotFound);

        _annotations.Remove(annotation);
        SetModified(deletedBy.Value.ToString());

        RaiseDomainEvent(new AnnotationDeletedEvent(annotationId, Id, StudyId, deletedBy));

        return Result.Success();
    }

    public TraceAnnotation? GetAnnotation(Guid annotationId) =>
        _annotations.FirstOrDefault(a => a.Id == annotationId);

    public IReadOnlyList<TraceAnnotation> GetSharedAnnotations() =>
        _annotations.Where(a => a.IsShared).ToList();
    #endregion
}

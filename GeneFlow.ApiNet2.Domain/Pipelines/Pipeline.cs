using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.Events;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Pipelines;

/// <summary>
/// Pipeline aggregate root representing a configurable analysis workflow.
/// </summary>
public sealed class Pipeline : FullAuditableAggregateRoot<PipelineId>
{
    #region Constants
    /// <summary>
    /// Maximum number of steps allowed in a single pipeline.
    /// Capped at 10 to keep pipelines manageable for users (UI editing,
    /// step reordering, debugging) and to bound execution complexity:
    /// each additional step multiplies the surface area of validation,
    /// configuration, and runtime cost. Pipelines that need more stages
    /// should be split into chained pipelines instead.
    /// </summary>
    public const int MaxSteps = 10;
    #endregion

    #region Properties
    private readonly List<PipelineStep> _steps = new();

    public StudyId StudyId { get; private set; } = null!;
    public UserId OwnerId { get; private set; } = null!;
    public PipelineName Name { get; private set; } = null!;
    public PipelineDescription Description { get; private set; } = null!;
    public PipelineStatus Status { get; private set; } = null!;

    public IReadOnlyList<PipelineStep> Steps => _steps.AsReadOnly();
    #endregion

    #region Computed Properties
    public bool CanBeEdited => Status.CanEdit;
    public bool CanBeExecuted => Status.CanExecute;
    public bool CanBeDeleted => Status.CanDelete;
    public int EnabledStepCount => _steps.Count(s => s.IsEnabled);
    #endregion

    #region Constructors
    private Pipeline() : base() { }

    private Pipeline(
        PipelineId id,
        StudyId studyId,
        UserId ownerId,
        PipelineName name,
        PipelineDescription description) : base(id)
    {
        StudyId = studyId;
        OwnerId = ownerId;
        Name = name;
        Description = description;
        Status = PipelineStatus.Draft;

        InitializeCreatedAt(ownerId.Value.ToString());
    }
    #endregion

    #region Factory Methods
    public static Result<Pipeline> Create(
        PipelineId id,
        StudyId studyId,
        UserId ownerId,
        PipelineName name,
        PipelineDescription description)
    {
        var pipeline = new Pipeline(id, studyId, ownerId, name, description);

        pipeline.RaiseDomainEvent(new PipelineCreatedEvent(
            pipeline.Id,
            pipeline.StudyId,
            pipeline.Name.Value,
            pipeline.OwnerId));

        return pipeline;
    }
    #endregion

    #region Basic Info Management
    public Result Update(
        PipelineName name,
        PipelineDescription description,
        UserId updatedBy)
    {
        if (!CanBeEdited)
            return Result.Failure(PipelineErrors.PipelineNotEditable);

        Name = name;
        Description = description;
        SetModified(updatedBy.Value.ToString());

        return Result.Success();
    }
    #endregion

    #region Step Management
    public Result<PipelineStep> AddStep(
        StepType stepType,
        StepConfiguration configuration,
        string? label,
        bool isEnabled,
        UserId addedBy)
    {
        if (!CanBeEdited)
            return Result.Failure<PipelineStep>(PipelineErrors.PipelineNotEditable);

        if (_steps.Count >= MaxSteps)
            return Result.Failure<PipelineStep>(PipelineErrors.MaxStepsExceeded(MaxSteps));

        if (!string.IsNullOrWhiteSpace(label) && label.Length > PipelineStep.MaxLabelLength)
            return Result.Failure<PipelineStep>(PipelineErrors.StepLabelTooLong);

        var order = _steps.Count + 1;
        var step = PipelineStep.Create(stepType, order, label, configuration, isEnabled);
        _steps.Add(step);

        SetModified(addedBy.Value.ToString());
        RaiseDomainEvent(new PipelineStepAddedEvent(Id, step.Id, stepType, order));

        return step;
    }

    public Result UpdateStep(
        Guid stepId,
        StepConfiguration configuration,
        string? label,
        bool isEnabled,
        UserId updatedBy)
    {
        if (!CanBeEdited)
            return Result.Failure(PipelineErrors.PipelineNotEditable);

        var step = _steps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            return Result.Failure(PipelineErrors.StepNotFound);

        if (!string.IsNullOrWhiteSpace(label) && label.Length > PipelineStep.MaxLabelLength)
            return Result.Failure(PipelineErrors.StepLabelTooLong);

        step.UpdateConfiguration(configuration);
        step.UpdateLabel(label);

        if (isEnabled)
            step.Enable();
        else
            step.Disable();

        SetModified(updatedBy.Value.ToString());
        return Result.Success();
    }

    public Result RemoveStep(Guid stepId, UserId removedBy)
    {
        if (!CanBeEdited)
            return Result.Failure(PipelineErrors.PipelineNotEditable);

        var step = _steps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            return Result.Failure(PipelineErrors.StepNotFound);

        var removedOrder = step.Order;
        _steps.Remove(step);

        foreach (var s in _steps.Where(s => s.Order > removedOrder))
        {
            s.SetOrder(s.Order - 1);
        }

        SetModified(removedBy.Value.ToString());
        RaiseDomainEvent(new PipelineStepRemovedEvent(Id, stepId, step.StepType, removedOrder));

        return Result.Success();
    }

    public Result ReorderSteps(IReadOnlyList<Guid> stepIds, UserId reorderedBy)
    {
        if (!CanBeEdited)
            return Result.Failure(PipelineErrors.PipelineNotEditable);

        if (stepIds.Count != _steps.Count)
            return Result.Failure(PipelineErrors.InvalidStepOrder);

        if (stepIds.Distinct().Count() != stepIds.Count)
            return Result.Failure(PipelineErrors.DuplicateStepOrder);

        foreach (var stepId in stepIds)
        {
            if (!_steps.Any(s => s.Id == stepId))
                return Result.Failure(PipelineErrors.StepNotFound);
        }

        for (var i = 0; i < stepIds.Count; i++)
        {
            var step = _steps.First(s => s.Id == stepIds[i]);
            step.SetOrder(i + 1);
        }

        SetModified(reorderedBy.Value.ToString());
        return Result.Success();
    }

    /// <summary>
    /// Flips every step's order to its negative value. The
    /// (pipeline_id, order) unique index is not deferrable, so persisting a
    /// reorder directly can collide with the previous values mid-update; the
    /// reorder handler parks the orders in the negative range, saves, then
    /// calls this again to restore the (newly assigned) positive orders.
    /// </summary>
    public void ToggleParkedStepOrders()
    {
        foreach (var step in _steps)
            step.SetOrder(-step.Order);
    }
    #endregion

    #region Status Management
    public Result Activate(UserId activatedBy)
    {
        if (!Status.CanTransitionTo(PipelineStatus.Active))
            return Result.Failure(PipelineErrors.InvalidStatusTransition(Status, PipelineStatus.Active));

        if (EnabledStepCount == 0)
            return Result.Failure(PipelineErrors.NoStepsConfigured);

        Status = PipelineStatus.Active;
        SetModified(activatedBy.Value.ToString());
        RaiseDomainEvent(new PipelineActivatedEvent(Id, activatedBy));

        return Result.Success();
    }

    public Result Deactivate(UserId deactivatedBy)
    {
        if (Status != PipelineStatus.Active)
            return Result.Failure(PipelineErrors.InvalidStatusTransition(Status, PipelineStatus.Draft));

        Status = PipelineStatus.Draft;
        SetModified(deactivatedBy.Value.ToString());

        return Result.Success();
    }

    public Result Archive(UserId archivedBy)
    {
        if (!Status.CanTransitionTo(PipelineStatus.Archived))
            return Result.Failure(PipelineErrors.InvalidStatusTransition(Status, PipelineStatus.Archived));

        Status = PipelineStatus.Archived;
        SetModified(archivedBy.Value.ToString());
        RaiseDomainEvent(new PipelineArchivedEvent(Id, archivedBy));

        return Result.Success();
    }

    public Result Restore(UserId restoredBy)
    {
        if (!Status.CanTransitionTo(PipelineStatus.Draft))
            return Result.Failure(PipelineErrors.InvalidStatusTransition(Status, PipelineStatus.Draft));

        Status = PipelineStatus.Draft;
        SetModified(restoredBy.Value.ToString());

        return Result.Success();
    }
    #endregion

    #region Execution
    public Result<PipelineExecution> StartExecution(
        PipelineExecutionId executionId,
        TraceId traceId,
        UserId startedBy)
    {
        if (!CanBeExecuted)
            return Result.Failure<PipelineExecution>(PipelineErrors.PipelineNotExecutable);

        if (EnabledStepCount == 0)
            return Result.Failure<PipelineExecution>(PipelineErrors.NoStepsConfigured);

        var execution = PipelineExecution.Create(
            executionId,
            Id,
            traceId,
            startedBy,
            _steps);

        RaiseDomainEvent(new PipelineExecutionStartedEvent(
            executionId,
            Id,
            traceId,
            startedBy,
            EnabledStepCount));

        return execution;
    }
    #endregion
}

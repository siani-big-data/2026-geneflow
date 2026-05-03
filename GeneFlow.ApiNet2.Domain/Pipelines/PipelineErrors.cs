using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Pipelines;

/// <summary>
/// Contains all domain errors related to the Pipeline aggregate.
/// </summary>
public static class PipelineErrors
{
    // General errors
    public static readonly Error NotFound = Error.NotFound("Pipeline.NotFound", "Pipeline was not found.");
    public static Error PipelineNotFoundById(string id) => Error.NotFound("Pipeline.NotFoundById", $"Pipeline with ID '{id}' was not found.");
    public static readonly Error InvalidUserId = Error.Validation("Pipeline.InvalidUserId", "Invalid user ID.");
    public static readonly Error InvalidStudyId = Error.Validation("Pipeline.InvalidStudyId", "Invalid study ID.");

    // Name errors
    public static readonly Error NameRequired = Error.Validation("Pipeline.NameRequired", "Pipeline name is required.");
    public static Error NameTooShort(int min) => Error.Validation("Pipeline.NameTooShort", $"Pipeline name must be at least {min} characters.");
    public static Error NameTooLong(int max) => Error.Validation("Pipeline.NameTooLong", $"Pipeline name must not exceed {max} characters.");

    // Description errors
    public static Error DescriptionTooLong(int max) => Error.Validation("Pipeline.DescriptionTooLong", $"Pipeline description must not exceed {max} characters.");

    // Status errors
    public static readonly Error InvalidStatus = Error.Validation("Pipeline.InvalidStatus", "Invalid pipeline status.");
    public static Error InvalidStatusTransition(PipelineStatus from, PipelineStatus to) =>
        Error.Validation("Pipeline.InvalidStatusTransition", $"Cannot transition pipeline from '{from.DisplayName}' to '{to.DisplayName}'.");
    public static readonly Error PipelineNotEditable = Error.Validation("Pipeline.NotEditable", "Pipeline cannot be edited in its current state. Only draft pipelines can be modified.");
    public static readonly Error PipelineNotExecutable = Error.Validation("Pipeline.NotExecutable", "Pipeline cannot be executed. Only active pipelines can be executed.");
    public static readonly Error PipelineNotDeletable = Error.Validation("Pipeline.NotDeletable", "Pipeline cannot be deleted in its current state.");

    // Step errors
    public static readonly Error StepNotFound = Error.NotFound("Pipeline.StepNotFound", "Pipeline step was not found.");
    public static Error MaxStepsExceeded(int max) => Error.Validation("Pipeline.MaxStepsExceeded", $"Pipeline cannot have more than {max} steps.");
    public static readonly Error NoStepsConfigured = Error.Validation("Pipeline.NoStepsConfigured", "Pipeline must have at least one step to be activated.");
    public static readonly Error InvalidStepType = Error.Validation("Pipeline.InvalidStepType", "Invalid step type.");
    public static readonly Error InvalidStepOrder = Error.Validation("Pipeline.InvalidStepOrder", "Invalid step order. Steps must be reordered sequentially starting from 1.");
    public static readonly Error DuplicateStepOrder = Error.Validation("Pipeline.DuplicateStepOrder", "Duplicate step order detected.");
    public static readonly Error StepLabelTooLong = Error.Validation("Pipeline.StepLabelTooLong", "Step label must not exceed 100 characters.");

    // Step configuration errors
    public static Error StepConfigurationRequired(string stepType) => Error.Validation("Pipeline.StepConfigurationRequired", $"Configuration is required for step type '{stepType}'.");
    public static Error StepConfigurationTooLong(int max) => Error.Validation("Pipeline.StepConfigurationTooLong", $"Step configuration must not exceed {max} characters.");
    public static readonly Error InvalidStepConfiguration = Error.Validation("Pipeline.InvalidStepConfiguration", "Invalid step configuration JSON format.");
    public static readonly Error MotifPatternRequired = Error.Validation("Pipeline.MotifPatternRequired", "Motif pattern is required for motif search steps.");
    public static readonly Error RestrictionEnzymesRequired = Error.Validation("Pipeline.RestrictionEnzymesRequired", "At least one restriction enzyme must be specified.");
    public static readonly Error TrimmingCutoffOutOfRange = Error.Validation("Pipeline.TrimmingCutoffOutOfRange", "Trimming cutoff must be between 0.01 and 0.5.");

    // Execution errors
    public static readonly Error ExecutionNotFound = Error.NotFound("Pipeline.ExecutionNotFound", "Pipeline execution was not found.");
    public static Error ExecutionNotFoundById(string id) => Error.NotFound("Pipeline.ExecutionNotFoundById", $"Pipeline execution with ID '{id}' was not found.");
    public static readonly Error TraceAlreadyRunning = Error.Conflict("Pipeline.TraceAlreadyRunning", "A pipeline execution is already running for this trace.");
    public static readonly Error TraceNotProcessed = Error.Validation("Pipeline.TraceNotProcessed", "Pipeline can only be executed on processed traces.");
    public static readonly Error ExecutionNotCancellable = Error.Validation("Pipeline.ExecutionNotCancellable", "Execution cannot be cancelled in its current state.");
    public static Error InvalidExecutionStatusTransition(ExecutionStatus from, ExecutionStatus to) =>
        Error.Validation("Pipeline.InvalidExecutionStatusTransition", $"Cannot transition execution from '{from.DisplayName}' to '{to.DisplayName}'.");

    // Step execution errors
    public static readonly Error StepExecutionNotFound = Error.NotFound("Pipeline.StepExecutionNotFound", "Step execution was not found.");
    public static Error InvalidStepExecutionStatusTransition(StepExecutionStatus from, StepExecutionStatus to) =>
        Error.Validation("Pipeline.InvalidStepExecutionStatusTransition", $"Cannot transition step execution from '{from.DisplayName}' to '{to.DisplayName}'.");

    // Permission errors
    public static readonly Error InsufficientPermissions = Error.Forbidden("Pipeline.InsufficientPermissions", "You do not have permission to perform this action on the pipeline.");
    public static readonly Error StudyNotFound = Error.NotFound("Pipeline.StudyNotFound", "The study associated with this pipeline was not found.");
}

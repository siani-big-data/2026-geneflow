using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;

namespace GeneFlow.ApiNet2.Application.Pipelines.Mappings;

/// <summary>
/// Mapping extensions for Pipeline domain entities to DTOs.
/// </summary>
public static class PipelineMappings
{
    public static PipelineDto ToDto(this Pipeline pipeline)
    {
        return new PipelineDto
        {
            Id = pipeline.Id.ToString(),
            StudyId = pipeline.StudyId.ToString(),
            OwnerId = pipeline.OwnerId.ToString(),
            Name = pipeline.Name.Value,
            Description = pipeline.Description.Value,
            StatusId = pipeline.Status.Id,
            StatusName = pipeline.Status.DisplayName,
            CanBeEdited = pipeline.CanBeEdited,
            CanBeExecuted = pipeline.CanBeExecuted,
            StepCount = pipeline.Steps.Count,
            EnabledStepCount = pipeline.EnabledStepCount,
            Steps = pipeline.Steps.Select(s => s.ToDto()).ToList(),
            CreatedAt = pipeline.CreatedAt,
            CreatedBy = pipeline.CreatedBy,
            ModifiedAt = pipeline.ModifiedAt,
            ModifiedBy = pipeline.ModifiedBy
        };
    }

    public static PipelineSummaryDto ToSummaryDto(this Pipeline pipeline)
    {
        return new PipelineSummaryDto
        {
            Id = pipeline.Id.ToString(),
            Name = pipeline.Name.Value,
            Description = pipeline.Description.Value,
            StatusId = pipeline.Status.Id,
            StatusName = pipeline.Status.DisplayName,
            StepCount = pipeline.Steps.Count,
            EnabledStepCount = pipeline.EnabledStepCount,
            CreatedAt = pipeline.CreatedAt,
            ModifiedAt = pipeline.ModifiedAt
        };
    }

    public static IReadOnlyList<PipelineSummaryDto> ToSummaryDtos(this IEnumerable<Pipeline> pipelines)
    {
        return pipelines.Select(p => p.ToSummaryDto()).ToList();
    }

    public static PipelineStepDto ToDto(this PipelineStep step)
    {
        return new PipelineStepDto
        {
            Id = step.Id.ToString(),
            StepTypeId = step.StepType.Id,
            StepTypeName = step.StepType.Name,
            StepTypeDisplayName = step.StepType.DisplayName,
            Order = step.Order,
            Label = step.Label,
            Configuration = step.Configuration.Value,
            IsEnabled = step.IsEnabled,
            CreatedAt = step.CreatedAt
        };
    }

    public static IReadOnlyList<PipelineStepDto> ToDtos(this IEnumerable<PipelineStep> steps)
    {
        return steps.Select(s => s.ToDto()).ToList();
    }

    public static PipelineExecutionDto ToDto(this PipelineExecution execution, string pipelineName)
    {
        return new PipelineExecutionDto
        {
            Id = execution.Id.ToString(),
            PipelineId = execution.PipelineId.ToString(),
            PipelineName = pipelineName,
            TraceId = execution.TraceId.ToString(),
            StartedById = execution.StartedBy.ToString(),
            StatusId = execution.Status.Id,
            StatusName = execution.Status.DisplayName,
            IsInProgress = execution.Status.IsInProgress,
            IsTerminal = execution.Status.IsTerminal,
            TotalSteps = execution.TotalSteps,
            CompletedSteps = execution.CompletedSteps,
            ProgressPercentage = execution.ProgressPercentage,
            ErrorMessage = execution.ErrorMessage,
            CreatedAt = execution.CreatedAt,
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt,
            DurationSeconds = execution.Duration?.TotalSeconds,
            StepExecutions = execution.StepExecutions.Select(s => s.ToDto()).ToList()
        };
    }

    public static PipelineExecutionSummaryDto ToSummaryDto(this PipelineExecution execution, string pipelineName)
    {
        return new PipelineExecutionSummaryDto
        {
            Id = execution.Id.ToString(),
            PipelineId = execution.PipelineId.ToString(),
            PipelineName = pipelineName,
            TraceId = execution.TraceId.ToString(),
            StatusId = execution.Status.Id,
            StatusName = execution.Status.DisplayName,
            TotalSteps = execution.TotalSteps,
            CompletedSteps = execution.CompletedSteps,
            ProgressPercentage = execution.ProgressPercentage,
            CreatedAt = execution.CreatedAt,
            CompletedAt = execution.CompletedAt,
            DurationSeconds = execution.Duration?.TotalSeconds
        };
    }

    public static StepExecutionDto ToDto(this StepExecution stepExecution)
    {
        return new StepExecutionDto
        {
            Id = stepExecution.Id.ToString(),
            PipelineStepId = stepExecution.PipelineStepId.ToString(),
            Order = stepExecution.Order,
            StepTypeId = stepExecution.StepType.Id,
            StepTypeName = stepExecution.StepType.Name,
            StepTypeDisplayName = stepExecution.StepType.DisplayName,
            StatusId = stepExecution.Status.Id,
            StatusName = stepExecution.Status.DisplayName,
            IsInProgress = stepExecution.Status.IsInProgress,
            IsSuccess = stepExecution.Status.IsSuccess,
            StartedAt = stepExecution.StartedAt,
            CompletedAt = stepExecution.CompletedAt,
            DurationSeconds = stepExecution.Duration?.TotalSeconds,
            ErrorMessage = stepExecution.ErrorMessage,
            ResultSummary = stepExecution.ResultSummary,
            ResultData = stepExecution.ResultData
        };
    }

    public static IReadOnlyList<StepExecutionDto> ToDtos(this IEnumerable<StepExecution> stepExecutions)
    {
        return stepExecutions.Select(s => s.ToDto()).ToList();
    }

    public static StepTypeDto ToDto(this StepType stepType)
    {
        return new StepTypeDto
        {
            Id = stepType.Id,
            Name = stepType.Name,
            DisplayName = stepType.DisplayName,
            AnalysisKey = stepType.AnalysisKey,
            RequiresConfiguration = stepType.RequiresConfiguration,
            ConfigurationSchema = stepType.GetConfigurationSchema()
        };
    }

    public static IReadOnlyList<StepTypeDto> ToDtos(this IEnumerable<StepType> stepTypes)
    {
        return stepTypes.Select(s => s.ToDto()).ToList();
    }
}

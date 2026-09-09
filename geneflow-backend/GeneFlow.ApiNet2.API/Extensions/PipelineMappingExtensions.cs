using GeneFlow.ApiNet2.API.Contracts.Pipelines.Responses;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Mapping extensions for Pipeline DTOs to API Responses.
/// </summary>
public static class PipelineMappingExtensions
{
    public static PipelineResponse ToResponse(this PipelineDto dto) =>
        new(
            Id: dto.Id,
            StudyId: dto.StudyId,
            OwnerId: dto.OwnerId,
            Name: dto.Name,
            Description: dto.Description,
            StatusId: dto.StatusId,
            StatusName: dto.StatusName,
            CanBeEdited: dto.CanBeEdited,
            CanBeExecuted: dto.CanBeExecuted,
            StepCount: dto.StepCount,
            EnabledStepCount: dto.EnabledStepCount,
            Steps: dto.Steps.Select(s => s.ToResponse()).ToList(),
            CreatedAt: dto.CreatedAt,
            CreatedBy: dto.CreatedBy,
            ModifiedAt: dto.ModifiedAt,
            ModifiedBy: dto.ModifiedBy);

    public static PipelineSummaryResponse ToSummaryResponse(this PipelineSummaryDto dto) =>
        new(
            Id: dto.Id,
            Name: dto.Name,
            Description: dto.Description,
            StatusId: dto.StatusId,
            StatusName: dto.StatusName,
            StepCount: dto.StepCount,
            EnabledStepCount: dto.EnabledStepCount,
            CreatedAt: dto.CreatedAt,
            ModifiedAt: dto.ModifiedAt);

    public static PipelineStepResponse ToResponse(this PipelineStepDto dto) =>
        new(
            Id: dto.Id,
            StepTypeId: dto.StepTypeId,
            StepTypeName: dto.StepTypeName,
            StepTypeDisplayName: dto.StepTypeDisplayName,
            Order: dto.Order,
            Label: dto.Label,
            Configuration: dto.Configuration,
            IsEnabled: dto.IsEnabled,
            DisplayName: dto.DisplayName,
            CreatedAt: dto.CreatedAt);

    public static PipelineExecutionResponse ToResponse(this PipelineExecutionDto dto) =>
        new(
            Id: dto.Id,
            PipelineId: dto.PipelineId,
            PipelineName: dto.PipelineName,
            TraceId: dto.TraceId,
            StartedById: dto.StartedById,
            StatusId: dto.StatusId,
            StatusName: dto.StatusName,
            IsInProgress: dto.IsInProgress,
            IsTerminal: dto.IsTerminal,
            TotalSteps: dto.TotalSteps,
            CompletedSteps: dto.CompletedSteps,
            ProgressPercentage: dto.ProgressPercentage,
            ErrorMessage: dto.ErrorMessage,
            CreatedAt: dto.CreatedAt,
            StartedAt: dto.StartedAt,
            CompletedAt: dto.CompletedAt,
            DurationSeconds: dto.DurationSeconds,
            StepExecutions: dto.StepExecutions.Select(s => s.ToResponse()).ToList());

    public static PipelineExecutionSummaryResponse ToSummaryResponse(this PipelineExecutionSummaryDto dto) =>
        new(
            Id: dto.Id,
            PipelineId: dto.PipelineId,
            PipelineName: dto.PipelineName,
            TraceId: dto.TraceId,
            StatusId: dto.StatusId,
            StatusName: dto.StatusName,
            TotalSteps: dto.TotalSteps,
            CompletedSteps: dto.CompletedSteps,
            ProgressPercentage: dto.ProgressPercentage,
            CreatedAt: dto.CreatedAt,
            CompletedAt: dto.CompletedAt,
            DurationSeconds: dto.DurationSeconds);

    public static StepExecutionResponse ToResponse(this StepExecutionDto dto) =>
        new(
            Id: dto.Id,
            PipelineStepId: dto.PipelineStepId,
            Order: dto.Order,
            StepTypeId: dto.StepTypeId,
            StepTypeName: dto.StepTypeName,
            StepTypeDisplayName: dto.StepTypeDisplayName,
            StatusId: dto.StatusId,
            StatusName: dto.StatusName,
            IsInProgress: dto.IsInProgress,
            IsSuccess: dto.IsSuccess,
            StartedAt: dto.StartedAt,
            CompletedAt: dto.CompletedAt,
            DurationSeconds: dto.DurationSeconds,
            ErrorMessage: dto.ErrorMessage,
            ResultSummary: dto.ResultSummary,
            ResultData: dto.ResultData);

    public static StepTypeResponse ToResponse(this StepTypeDto dto) =>
        new(
            Id: dto.Id,
            Name: dto.Name,
            DisplayName: dto.DisplayName,
            AnalysisKey: dto.AnalysisKey,
            RequiresConfiguration: dto.RequiresConfiguration,
            ConfigurationSchema: dto.ConfigurationSchema);
}

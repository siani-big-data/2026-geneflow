/**
 * Pipelines Service for GeneFlow.
 * Integrates with the backend Pipeline module API.
 */

import { api } from "@/lib/api-client";
import type {
  Pipeline,
  PipelineSummary,
  PipelineStep,
  PipelineExecution,
  PipelineExecutionSummary,
  RecentPipelineExecution,
  StepType,
  CreatePipelineInput,
  UpdatePipelineInput,
  AddPipelineStepInput,
  UpdatePipelineStepInput,
  ReorderStepsInput,
  ExecutePipelineInput,
  PipelineFilters,
  ExecutionFilters,
} from "@/types/pipeline";
import type { PagedResponse } from "@/types/common";

export const pipelinesService = {
  // ============== Step Types ==============

  /**
   * Get available step types with their configuration schemas.
   */
  async getStepTypes(): Promise<StepType[]> {
    return api.get<StepType[]>("/api/v1/pipelines/step-types");
  },

  // ============== Pipeline CRUD ==============

  /**
   * Get all pipelines for a study with optional filters and pagination.
   */
  async getByStudy(
    studyId: string,
    filters?: PipelineFilters,
    pageNumber: number = 1,
    pageSize: number = 20
  ): Promise<PagedResponse<PipelineSummary>> {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });

    if (filters?.searchTerm) {
      params.append("searchTerm", filters.searchTerm);
    }
    if (filters?.statusId !== undefined) {
      params.append("statusId", filters.statusId.toString());
    }

    return api.get<PagedResponse<PipelineSummary>>(
      `/api/v1/studies/${studyId}/pipelines?${params.toString()}`
    );
  },

  /**
   * Get a pipeline by ID with all its steps.
   */
  async getById(studyId: string, pipelineId: string): Promise<Pipeline> {
    return api.get<Pipeline>(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}`
    );
  },

  /**
   * Create a new pipeline in a study.
   */
  async create(studyId: string, input: CreatePipelineInput): Promise<Pipeline> {
    return api.post<Pipeline>(`/api/v1/studies/${studyId}/pipelines`, input);
  },

  /**
   * Update a pipeline's name and description.
   */
  async update(
    studyId: string,
    pipelineId: string,
    input: UpdatePipelineInput
  ): Promise<Pipeline> {
    return api.put<Pipeline>(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}`,
      input
    );
  },

  /**
   * Delete a pipeline.
   */
  async delete(studyId: string, pipelineId: string): Promise<void> {
    return api.delete(`/api/v1/studies/${studyId}/pipelines/${pipelineId}`);
  },

  // ============== Pipeline Status Management ==============

  /**
   * Activate a draft pipeline (Draft -> Active).
   */
  async activate(studyId: string, pipelineId: string): Promise<Pipeline> {
    return api.post<Pipeline>(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}/activate`
    );
  },

  /**
   * Deactivate an active pipeline (Active -> Draft).
   */
  async deactivate(studyId: string, pipelineId: string): Promise<Pipeline> {
    return api.post<Pipeline>(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}/deactivate`
    );
  },

  /**
   * Archive a pipeline.
   */
  async archive(studyId: string, pipelineId: string): Promise<Pipeline> {
    return api.post<Pipeline>(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}/archive`
    );
  },

  // ============== Pipeline Step Management ==============

  /**
   * Add a step to a pipeline.
   */
  async addStep(
    studyId: string,
    pipelineId: string,
    input: AddPipelineStepInput
  ): Promise<PipelineStep> {
    return api.post<PipelineStep>(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}/steps`,
      input
    );
  },

  /**
   * Update a pipeline step.
   */
  async updateStep(
    studyId: string,
    pipelineId: string,
    stepId: string,
    input: UpdatePipelineStepInput
  ): Promise<PipelineStep> {
    return api.put<PipelineStep>(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}/steps/${stepId}`,
      input
    );
  },

  /**
   * Remove a step from a pipeline.
   */
  async removeStep(
    studyId: string,
    pipelineId: string,
    stepId: string
  ): Promise<void> {
    return api.delete(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}/steps/${stepId}`
    );
  },

  /**
   * Reorder the steps in a pipeline.
   */
  async reorderSteps(
    studyId: string,
    pipelineId: string,
    input: ReorderStepsInput
  ): Promise<Pipeline> {
    return api.put<Pipeline>(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}/steps/reorder`,
      input
    );
  },

  // ============== Pipeline Execution ==============

  /**
   * Execute a pipeline on a trace.
   */
  async execute(
    studyId: string,
    pipelineId: string,
    input: ExecutePipelineInput
  ): Promise<PipelineExecution> {
    return api.post<PipelineExecution>(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}/execute`,
      input
    );
  },

  /**
   * Get executions for a specific pipeline.
   */
  async getExecutions(
    studyId: string,
    pipelineId: string,
    filters?: ExecutionFilters,
    pageNumber: number = 1,
    pageSize: number = 20
  ): Promise<PagedResponse<PipelineExecutionSummary>> {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });

    if (filters?.statusId !== undefined) {
      params.append("statusId", filters.statusId.toString());
    }

    return api.get<PagedResponse<PipelineExecutionSummary>>(
      `/api/v1/studies/${studyId}/pipelines/${pipelineId}/executions?${params.toString()}`
    );
  },

  /**
   * Get executions for a specific trace.
   */
  async getTraceExecutions(
    studyId: string,
    traceId: string,
    filters?: ExecutionFilters,
    pageNumber: number = 1,
    pageSize: number = 20
  ): Promise<PagedResponse<PipelineExecutionSummary>> {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });

    if (filters?.statusId !== undefined) {
      params.append("statusId", filters.statusId.toString());
    }

    return api.get<PagedResponse<PipelineExecutionSummary>>(
      `/api/v1/studies/${studyId}/traces/${traceId}/executions?${params.toString()}`
    );
  },

  /**
   * Get execution details by ID.
   */
  async getExecutionById(executionId: string): Promise<PipelineExecution> {
    return api.get<PipelineExecution>(
      `/api/v1/pipeline-executions/${executionId}`
    );
  },

  /**
   * Cancel a running execution.
   */
  async cancelExecution(executionId: string): Promise<PipelineExecution> {
    return api.post<PipelineExecution>(
      `/api/v1/pipeline-executions/${executionId}/cancel`
    );
  },

  /**
   * Get the most recent pipeline executions across all studies the current user
   * is a member of. Used by the dashboard's recent-pipelines lateral panel.
   */
  async getRecentForCurrentUser(
    limit: number = 5
  ): Promise<RecentPipelineExecution[]> {
    const params = new URLSearchParams({ limit: limit.toString() });
    return api.get<RecentPipelineExecution[]>(
      `/api/v1/me/pipeline-executions?${params.toString()}`
    );
  },
};

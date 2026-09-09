"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { pipelinesService } from "@/services";
import type {
  PipelineFilters,
  ExecutionFilters,
  CreatePipelineInput,
  UpdatePipelineInput,
  AddPipelineStepInput,
  UpdatePipelineStepInput,
  ReorderStepsInput,
  ExecutePipelineInput,
} from "@/types/pipeline";

// ============== Query Keys ==============

export const pipelineKeys = {
  all: ["pipelines"] as const,
  stepTypes: () => [...pipelineKeys.all, "step-types"] as const,
  lists: () => [...pipelineKeys.all, "list"] as const,
  list: (studyId: string, filters?: PipelineFilters, page?: number) =>
    [...pipelineKeys.lists(), studyId, { filters, page }] as const,
  details: () => [...pipelineKeys.all, "detail"] as const,
  detail: (studyId: string, pipelineId: string) =>
    [...pipelineKeys.details(), studyId, pipelineId] as const,
  executions: () => [...pipelineKeys.all, "executions"] as const,
  pipelineExecutions: (
    studyId: string,
    pipelineId: string,
    filters?: ExecutionFilters,
    page?: number
  ) => [...pipelineKeys.executions(), "pipeline", studyId, pipelineId, { filters, page }] as const,
  traceExecutions: (
    studyId: string,
    traceId: string,
    filters?: ExecutionFilters,
    page?: number
  ) => [...pipelineKeys.executions(), "trace", studyId, traceId, { filters, page }] as const,
  execution: (executionId: string) =>
    [...pipelineKeys.executions(), executionId] as const,
  recentForCurrentUser: (limit: number) =>
    [...pipelineKeys.executions(), "me", { limit }] as const,
};

// ============== Queries ==============

/**
 * Fetch available step types.
 */
export function useStepTypes() {
  return useQuery({
    queryKey: pipelineKeys.stepTypes(),
    queryFn: () => pipelinesService.getStepTypes(),
    staleTime: 1000 * 60 * 60, // Step types rarely change, cache for 1 hour
  });
}

/**
 * Fetch pipelines for a study.
 */
export function usePipelines(
  studyId: string,
  filters?: PipelineFilters,
  page = 1,
  pageSize = 20
) {
  return useQuery({
    queryKey: pipelineKeys.list(studyId, filters, page),
    queryFn: () => pipelinesService.getByStudy(studyId, filters, page, pageSize),
    enabled: !!studyId,
  });
}

/**
 * Fetch a single pipeline with all its steps.
 */
export function usePipeline(studyId: string, pipelineId: string) {
  return useQuery({
    queryKey: pipelineKeys.detail(studyId, pipelineId),
    queryFn: () => pipelinesService.getById(studyId, pipelineId),
    enabled: !!studyId && !!pipelineId,
  });
}

/**
 * Fetch executions for a pipeline.
 */
export function usePipelineExecutions(
  studyId: string,
  pipelineId: string,
  filters?: ExecutionFilters,
  page = 1,
  pageSize = 20
) {
  return useQuery({
    queryKey: pipelineKeys.pipelineExecutions(studyId, pipelineId, filters, page),
    queryFn: () =>
      pipelinesService.getExecutions(studyId, pipelineId, filters, page, pageSize),
    enabled: !!studyId && !!pipelineId,
  });
}

/**
 * Fetch executions for a trace.
 */
export function useTraceExecutions(
  studyId: string,
  traceId: string,
  filters?: ExecutionFilters,
  page = 1,
  pageSize = 20
) {
  return useQuery({
    queryKey: pipelineKeys.traceExecutions(studyId, traceId, filters, page),
    queryFn: () =>
      pipelinesService.getTraceExecutions(studyId, traceId, filters, page, pageSize),
    enabled: !!studyId && !!traceId,
  });
}

/**
 * Fetch execution details with live updates.
 */
export function useExecution(executionId: string, refetchInterval?: number) {
  return useQuery({
    queryKey: pipelineKeys.execution(executionId),
    queryFn: () => pipelinesService.getExecutionById(executionId),
    enabled: !!executionId,
    refetchInterval: refetchInterval ?? false, // Can be set for live progress updates
  });
}

/**
 * Fetch the current user's most recent pipeline executions across all studies
 * they are a member of. Used by the dashboard's recent-pipelines lateral panel.
 *
 * Polls every 15 seconds so in-progress executions show live progress.
 */
export function useRecentExecutions(limit: number = 5) {
  return useQuery({
    queryKey: pipelineKeys.recentForCurrentUser(limit),
    queryFn: () => pipelinesService.getRecentForCurrentUser(limit),
    refetchInterval: 1000 * 15,
    staleTime: 1000 * 10,
  });
}

// ============== Mutations ==============

/**
 * Create a new pipeline.
 */
export function useCreatePipeline(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreatePipelineInput) =>
      pipelinesService.create(studyId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pipelineKeys.lists() });
    },
  });
}

/**
 * Update a pipeline.
 */
export function useUpdatePipeline(studyId: string, pipelineId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: UpdatePipelineInput) =>
      pipelinesService.update(studyId, pipelineId, input),
    onSuccess: (updatedPipeline) => {
      queryClient.setQueryData(
        pipelineKeys.detail(studyId, pipelineId),
        updatedPipeline
      );
      queryClient.invalidateQueries({ queryKey: pipelineKeys.lists() });
    },
  });
}

/**
 * Delete a pipeline.
 */
export function useDeletePipeline(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (pipelineId: string) =>
      pipelinesService.delete(studyId, pipelineId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pipelineKeys.lists() });
    },
  });
}

/**
 * Activate a pipeline.
 */
export function useActivatePipeline(studyId: string, pipelineId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => pipelinesService.activate(studyId, pipelineId),
    onSuccess: (updatedPipeline) => {
      queryClient.setQueryData(
        pipelineKeys.detail(studyId, pipelineId),
        updatedPipeline
      );
      queryClient.invalidateQueries({ queryKey: pipelineKeys.lists() });
    },
  });
}

/**
 * Deactivate a pipeline.
 */
export function useDeactivatePipeline(studyId: string, pipelineId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => pipelinesService.deactivate(studyId, pipelineId),
    onSuccess: (updatedPipeline) => {
      queryClient.setQueryData(
        pipelineKeys.detail(studyId, pipelineId),
        updatedPipeline
      );
      queryClient.invalidateQueries({ queryKey: pipelineKeys.lists() });
    },
  });
}

/**
 * Archive a pipeline.
 */
export function useArchivePipeline(studyId: string, pipelineId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => pipelinesService.archive(studyId, pipelineId),
    onSuccess: (updatedPipeline) => {
      queryClient.setQueryData(
        pipelineKeys.detail(studyId, pipelineId),
        updatedPipeline
      );
      queryClient.invalidateQueries({ queryKey: pipelineKeys.lists() });
    },
  });
}

/**
 * Add a step to a pipeline.
 */
export function useAddPipelineStep(studyId: string, pipelineId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: AddPipelineStepInput) =>
      pipelinesService.addStep(studyId, pipelineId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: pipelineKeys.detail(studyId, pipelineId),
      });
    },
  });
}

/**
 * Update a pipeline step.
 */
export function useUpdatePipelineStep(studyId: string, pipelineId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ stepId, input }: { stepId: string; input: UpdatePipelineStepInput }) =>
      pipelinesService.updateStep(studyId, pipelineId, stepId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: pipelineKeys.detail(studyId, pipelineId),
      });
    },
  });
}

/**
 * Remove a step from a pipeline.
 */
export function useRemovePipelineStep(studyId: string, pipelineId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (stepId: string) =>
      pipelinesService.removeStep(studyId, pipelineId, stepId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: pipelineKeys.detail(studyId, pipelineId),
      });
    },
  });
}

/**
 * Reorder pipeline steps.
 */
export function useReorderPipelineSteps(studyId: string, pipelineId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: ReorderStepsInput) =>
      pipelinesService.reorderSteps(studyId, pipelineId, input),
    onSuccess: (updatedPipeline) => {
      queryClient.setQueryData(
        pipelineKeys.detail(studyId, pipelineId),
        updatedPipeline
      );
    },
  });
}

/**
 * Execute a pipeline on a trace.
 */
export function useExecutePipeline(studyId: string, pipelineId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: ExecutePipelineInput) =>
      pipelinesService.execute(studyId, pipelineId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: pipelineKeys.executions(),
      });
    },
  });
}

/**
 * Cancel a running execution.
 */
export function useCancelExecution() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (executionId: string) =>
      pipelinesService.cancelExecution(executionId),
    onSuccess: (updatedExecution) => {
      queryClient.setQueryData(
        pipelineKeys.execution(updatedExecution.id),
        updatedExecution
      );
      queryClient.invalidateQueries({
        queryKey: pipelineKeys.executions(),
      });
    },
  });
}

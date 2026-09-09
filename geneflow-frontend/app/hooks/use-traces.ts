"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { tracesService } from "@/services";
import type {
  TraceFilters,
  UploadTraceInput,
  UpdateTraceNameInput,
  AddTrimInput,
  CreateAnnotationInput,
  UpdateAnnotationInput,
} from "@/types";

// =============================================================================
// TRACE QUERIES
// =============================================================================

/**
 * Get traces for a study (paginated).
 */
export function useStudyTraces(
  studyId: string,
  pageNumber: number = 1,
  pageSize: number = 20,
  filters?: TraceFilters
) {
  return useQuery({
    queryKey: ["traces", studyId, pageNumber, pageSize, filters],
    queryFn: () => tracesService.getByStudyId(studyId, pageNumber, pageSize, filters),
    enabled: !!studyId,
  });
}

/**
 * Get trace counts by status for a study.
 */
export function useTraceCounts(studyId: string) {
  return useQuery({
    queryKey: ["traces", studyId, "counts"],
    queryFn: () => tracesService.getCountsByStatus(studyId),
    enabled: !!studyId,
  });
}

/**
 * Get a single trace by ID.
 */
export function useTrace(studyId: string, traceId: string) {
  return useQuery({
    queryKey: ["trace", studyId, traceId],
    queryFn: () => tracesService.getById(studyId, traceId),
    enabled: !!studyId && !!traceId,
  });
}

// =============================================================================
// TRACE MUTATIONS
// =============================================================================

/**
 * Upload a new trace file.
 */
export function useUploadTrace(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: UploadTraceInput) => tracesService.upload(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["traces", studyId] });
    },
  });
}

/**
 * Update trace name.
 */
export function useUpdateTraceName(studyId: string, traceId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: UpdateTraceNameInput) =>
      tracesService.updateName(studyId, traceId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId] });
      queryClient.invalidateQueries({ queryKey: ["traces", studyId] });
    },
  });
}

/**
 * Archive a trace.
 */
export function useArchiveTrace(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (traceId: string) => tracesService.archive(studyId, traceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["traces", studyId] });
    },
  });
}

/**
 * Retry processing for a failed trace.
 */
export function useRetryTraceProcessing(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (traceId: string) => tracesService.retryProcessing(studyId, traceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["traces", studyId] });
    },
  });
}

/**
 * Delete a trace.
 */
export function useDeleteTrace(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (traceId: string) => tracesService.delete(studyId, traceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["traces", studyId] });
    },
  });
}

// =============================================================================
// BATCH MUTATIONS
// =============================================================================

/**
 * Delete multiple traces.
 */
export function useDeleteTraces(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (traceIds: string[]) => tracesService.deleteMany(studyId, traceIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["traces", studyId] });
    },
  });
}

/**
 * Retry processing for multiple failed traces.
 */
export function useRetryTraces(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (traceIds: string[]) => tracesService.retryMany(studyId, traceIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["traces", studyId] });
    },
  });
}

/**
 * Archive multiple traces.
 */
export function useArchiveTraces(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (traceIds: string[]) => tracesService.archiveMany(studyId, traceIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["traces", studyId] });
    },
  });
}

// =============================================================================
// TRIMS (multiple trims per trace)
// =============================================================================

/**
 * Get all trims for a trace.
 */
export function useTraceTrims(studyId: string, traceId: string, activeOnly: boolean = true) {
  return useQuery({
    queryKey: ["trace", studyId, traceId, "trims", activeOnly],
    queryFn: () => tracesService.getTrims(studyId, traceId, activeOnly),
    enabled: !!studyId && !!traceId,
  });
}

/**
 * Add a new trim to a trace.
 */
export function useAddTrim(studyId: string, traceId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: AddTrimInput) =>
      tracesService.addTrim(studyId, traceId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId] });
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId, "trims"] });
      queryClient.invalidateQueries({ queryKey: ["traces", studyId] });
    },
  });
}

/**
 * Apply auto-trim using quality-based algorithm.
 */
export function useAutoTrim(studyId: string, traceId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (options?: {
      qualityThreshold?: number;
      windowSize?: number;
      minimumLength?: number;
    }) => tracesService.autoTrim(studyId, traceId, options),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId] });
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId, "trims"] });
      queryClient.invalidateQueries({ queryKey: ["traces", studyId] });
    },
  });
}

/**
 * Undo a specific trim.
 */
export function useUndoTrim(studyId: string, traceId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (trimId: string) => tracesService.undoTrim(studyId, traceId, trimId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId] });
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId, "trims"] });
      queryClient.invalidateQueries({ queryKey: ["traces", studyId] });
    },
  });
}

/**
 * Undo all active trims on a trace.
 */
export function useUndoAllTrims(studyId: string, traceId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => tracesService.undoAllTrims(studyId, traceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId] });
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId, "trims"] });
      queryClient.invalidateQueries({ queryKey: ["traces", studyId] });
    },
  });
}

/**
 * Get trimmed sequence with all active trims applied.
 */
export function useTrimmedSequence(studyId: string, traceId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: ["trace", studyId, traceId, "trimmed"],
    queryFn: () => tracesService.getTrimmedSequence(studyId, traceId),
    enabled: !!studyId && !!traceId && enabled,
  });
}

// =============================================================================
// ANNOTATIONS
// =============================================================================

/**
 * Get all annotations for a trace.
 */
export function useTraceAnnotations(studyId: string, traceId: string) {
  return useQuery({
    queryKey: ["trace", studyId, traceId, "annotations"],
    queryFn: () => tracesService.getAnnotations(studyId, traceId),
    enabled: !!studyId && !!traceId,
  });
}

/**
 * Create a new annotation.
 */
export function useCreateAnnotation(studyId: string, traceId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreateAnnotationInput) =>
      tracesService.createAnnotation(studyId, traceId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId, "annotations"] });
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId] });
    },
  });
}

/**
 * Update an annotation.
 */
export function useUpdateAnnotation(studyId: string, traceId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ annotationId, input }: { annotationId: string; input: UpdateAnnotationInput }) =>
      tracesService.updateAnnotation(studyId, traceId, annotationId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId, "annotations"] });
    },
  });
}

/**
 * Delete an annotation.
 */
export function useDeleteAnnotation(studyId: string, traceId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (annotationId: string) =>
      tracesService.deleteAnnotation(studyId, traceId, annotationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId, "annotations"] });
      queryClient.invalidateQueries({ queryKey: ["trace", studyId, traceId] });
    },
  });
}

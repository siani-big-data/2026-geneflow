import { useMutation, useQuery } from "@tanstack/react-query";
import { analysisService } from "@/services/analysis.service";
import { tracesService } from "@/services/traces.service";
import type {
  TrimmingRequest,
  HeterozygoteRequest,
  MotifSearchRequest,
  TranslationRequest,
  ORFDetectionRequest,
  RestrictionAnalysisRequest,
  AlignmentRequest,
  AnalysisType,
  AnalysisResultsListResponse,
} from "@/types";

const ANALYSIS_RESULT_STALE_MS = 5 * 60 * 1000;

export const analysisKeys = {
  all: ["analysis"] as const,
  list: (traceId: string) => [...analysisKeys.all, "list", traceId] as const,
  result: (traceId: string, type: AnalysisType) =>
    [...analysisKeys.all, "result", traceId, type] as const,
};

// =============================================================================
// PRIMITIVE TRIGGER MUTATIONS
// =============================================================================

export function useRequestTrimming(traceId: string) {
  return useMutation({
    mutationFn: (options: TrimmingRequest = {}) =>
      analysisService.requestTrimming(traceId, options),
  });
}

export function useRequestHeterozygoteDetection(traceId: string) {
  return useMutation({
    mutationFn: (options: HeterozygoteRequest = {}) =>
      analysisService.requestHeterozygoteDetection(traceId, options),
  });
}

export function useRequestMotifSearch(traceId: string) {
  return useMutation({
    mutationFn: (options: MotifSearchRequest) =>
      analysisService.requestMotifSearch(traceId, options),
  });
}

export function useRequestTranslation(traceId: string) {
  return useMutation({
    mutationFn: (options: TranslationRequest = {}) =>
      analysisService.requestTranslation(traceId, options),
  });
}

export function useRequestORFDetection(traceId: string) {
  return useMutation({
    mutationFn: (options: ORFDetectionRequest = {}) =>
      analysisService.requestORFDetection(traceId, options),
  });
}

export function useRequestRestrictionAnalysis(traceId: string) {
  return useMutation({
    mutationFn: (options: RestrictionAnalysisRequest = {}) =>
      analysisService.requestRestrictionAnalysis(traceId, options),
  });
}

export function useRequestAlignment() {
  return useMutation({
    mutationFn: (options: AlignmentRequest) =>
      analysisService.requestAlignment(options),
  });
}

// =============================================================================
// COMPOSITE: useTraceAnalysis
// =============================================================================

/**
 * Composite hook returning all analysis trigger mutations for a single trace.
 * Used by the studies page analysis tab.
 */
export function useTraceAnalysis(traceId: string) {
  return {
    trimming: useRequestTrimming(traceId),
    heterozygote: useRequestHeterozygoteDetection(traceId),
    motifSearch: useRequestMotifSearch(traceId),
    translation: useRequestTranslation(traceId),
    orfDetection: useRequestORFDetection(traceId),
    restrictionAnalysis: useRequestRestrictionAnalysis(traceId),
  };
}

// =============================================================================
// LIST + RESULT QUERIES
// =============================================================================

export function useAnalysisList(traceId: string) {
  return useQuery<AnalysisResultsListResponse>({
    queryKey: analysisKeys.list(traceId),
    queryFn: () => tracesService.listAnalysisResults(traceId),
    enabled: !!traceId,
  });
}

export function useAnalysisResult<T>(
  traceId: string,
  type: AnalysisType,
  enabled: boolean = true,
) {
  return useQuery<T>({
    queryKey: analysisKeys.result(traceId, type),
    queryFn: () => tracesService.getAnalysisResult<T>(traceId, type),
    enabled: enabled && !!traceId && !!type,
    staleTime: ANALYSIS_RESULT_STALE_MS,
  });
}

// =============================================================================
// FIRE-AND-FORGET TRIGGER
// =============================================================================

function selectTriggerByType(
  type: AnalysisType,
  traceId: string,
): (input: unknown) => Promise<void> {
  switch (type) {
    case "trimming":
      return (input) =>
        analysisService.requestTrimming(traceId, (input as TrimmingRequest) ?? {});
    case "heterozygote":
      return (input) =>
        analysisService.requestHeterozygoteDetection(
          traceId,
          (input as HeterozygoteRequest) ?? {},
        );
    case "motif":
      return (input) =>
        analysisService.requestMotifSearch(traceId, input as MotifSearchRequest);
    case "translation":
      return (input) =>
        analysisService.requestTranslation(
          traceId,
          (input as TranslationRequest) ?? {},
        );
    case "orf":
      return (input) =>
        analysisService.requestORFDetection(
          traceId,
          (input as ORFDetectionRequest) ?? {},
        );
    case "restriction":
      return (input) =>
        analysisService.requestRestrictionAnalysis(
          traceId,
          (input as RestrictionAnalysisRequest) ?? {},
        );
  }
}

/**
 * Fires an analysis request (returns 202 from the API) and resolves as soon as
 * the request is accepted. Completion is delivered separately via SSE — see
 * {@link useAnalysisEvents}.
 */
export function useTriggerAnalysis(traceId: string, type: AnalysisType) {
  const trigger = selectTriggerByType(type, traceId);
  return useMutation({
    mutationFn: (req: unknown) => trigger(req),
  });
}

import { api } from "@/lib/api-client";
import type {
  TrimmingRequest,
  HeterozygoteRequest,
  MotifSearchRequest,
  TranslationRequest,
  ORFDetectionRequest,
  RestrictionAnalysisRequest,
  AlignmentRequest,
} from "@/types";

/**
 * Analysis service: triggers backend analysis jobs for traces.
 *
 * All endpoints return 202 Accepted with no body when the analysis is queued.
 * Results are read separately via tracesService.getAnalysisResult.
 */
export const analysisService = {
  async requestTrimming(traceId: string, options: TrimmingRequest = {}): Promise<void> {
    await api.post(`/api/v1/traces/${traceId}/analysis/trimming`, options);
  },

  async requestHeterozygoteDetection(
    traceId: string,
    options: HeterozygoteRequest = {},
  ): Promise<void> {
    await api.post(`/api/v1/traces/${traceId}/analysis/heterozygote`, options);
  },

  async requestMotifSearch(
    traceId: string,
    options: MotifSearchRequest,
  ): Promise<void> {
    await api.post(`/api/v1/traces/${traceId}/analysis/motif`, options);
  },

  async requestTranslation(
    traceId: string,
    options: TranslationRequest = {},
  ): Promise<void> {
    await api.post(`/api/v1/traces/${traceId}/analysis/translation`, options);
  },

  async requestORFDetection(
    traceId: string,
    options: ORFDetectionRequest = {},
  ): Promise<void> {
    await api.post(`/api/v1/traces/${traceId}/analysis/orf`, options);
  },

  async requestRestrictionAnalysis(
    traceId: string,
    options: RestrictionAnalysisRequest = {},
  ): Promise<void> {
    await api.post(`/api/v1/traces/${traceId}/analysis/restriction`, options);
  },

  async requestAlignment(options: AlignmentRequest): Promise<void> {
    await api.post(`/api/v1/analysis/alignment`, options);
  },
};

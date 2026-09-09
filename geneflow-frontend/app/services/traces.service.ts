/**
 * Traces Service - Real API Integration
 *
 * Connects to GeneFlow.ApiNet2 backend API for trace management.
 * All trace endpoints require a studyId context.
 */

import { api } from "@/lib/api-client";
import type {
  Trace,
  TraceSummary,
  TraceCounts,
  TraceFilters,
  UploadTraceInput,
  UpdateTraceNameInput,
  PagedResponse,
  TraceManifest,
  SequencePage,
  TraceAnnotation,
  TraceTrim,
  AddTrimInput,
  CreateAnnotationInput,
  UpdateAnnotationInput,
  TrimmedSequence,
  AnalysisResultsListResponse,
} from "@/types";

// =============================================================================
// TRACE QUERIES
// =============================================================================

export const tracesService = {
  /**
   * Get traces for a study (paginated).
   */
  async getByStudyId(
    studyId: string,
    pageNumber: number = 1,
    pageSize: number = 20,
    filters?: TraceFilters
  ): Promise<PagedResponse<TraceSummary>> {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });

    if (filters?.searchTerm) params.append("searchTerm", filters.searchTerm);
    if (filters?.statusId) params.append("statusId", filters.statusId.toString());
    if (filters?.formatId) params.append("formatId", filters.formatId.toString());
    if (filters?.sortBy) params.append("sortBy", filters.sortBy);
    if (filters?.sortDescending !== undefined) {
      params.append("sortDescending", filters.sortDescending.toString());
    }

    return api.get<PagedResponse<TraceSummary>>(
      `/api/v1/studies/${studyId}/traces?${params}`
    );
  },

  /**
   * Get trace counts by status for a study.
   */
  async getCountsByStatus(studyId: string): Promise<TraceCounts> {
    return api.get<TraceCounts>(`/api/v1/studies/${studyId}/traces/counts`);
  },

  /**
   * Get a single trace by ID.
   */
  async getById(studyId: string, traceId: string): Promise<Trace> {
    return api.get<Trace>(`/api/v1/studies/${studyId}/traces/${traceId}`);
  },

  // ===========================================================================
  // TRACE MUTATIONS
  // ===========================================================================

  /**
   * Upload a new trace file.
   */
  async upload(input: UploadTraceInput): Promise<Trace> {
    const formData = new FormData();
    formData.append("name", input.name);
    formData.append("file", input.file);
    if (input.description) {
      formData.append("description", input.description);
    }

    return api.postForm<Trace>(
      `/api/v1/studies/${input.studyId}/traces/upload`,
      formData
    );
  },

  /**
   * Update trace name.
   */
  async updateName(
    studyId: string,
    traceId: string,
    input: UpdateTraceNameInput
  ): Promise<void> {
    return api.patch<void>(
      `/api/v1/studies/${studyId}/traces/${traceId}/name`,
      input
    );
  },

  /**
   * Archive a trace.
   */
  async archive(studyId: string, traceId: string): Promise<void> {
    return api.post<void>(`/api/v1/studies/${studyId}/traces/${traceId}/archive`);
  },

  /**
   * Retry processing for a failed trace.
   */
  async retryProcessing(studyId: string, traceId: string): Promise<void> {
    return api.post<void>(`/api/v1/studies/${studyId}/traces/${traceId}/retry`);
  },

  /**
   * Delete a trace permanently.
   */
  async delete(studyId: string, traceId: string): Promise<void> {
    return api.delete<void>(`/api/v1/studies/${studyId}/traces/${traceId}`);
  },

  // ===========================================================================
  // BATCH OPERATIONS
  // ===========================================================================

  /**
   * Delete multiple traces.
   */
  async deleteMany(studyId: string, traceIds: string[]): Promise<void> {
    await Promise.all(traceIds.map((id) => this.delete(studyId, id)));
  },

  /**
   * Retry processing for multiple failed traces.
   */
  async retryMany(studyId: string, traceIds: string[]): Promise<void> {
    await Promise.all(traceIds.map((id) => this.retryProcessing(studyId, id)));
  },

  /**
   * Archive multiple traces.
   */
  async archiveMany(studyId: string, traceIds: string[]): Promise<void> {
    await Promise.all(traceIds.map((id) => this.archive(studyId, id)));
  },

  // ===========================================================================
  // SEQUENCE DATA (from datalake storage)
  // ===========================================================================

  /**
   * Get trace manifest with chunk information.
   */
  async getManifest(traceId: string): Promise<TraceManifest> {
    return api.get<TraceManifest>(`/api/v1/traces/${traceId}/sequence/manifest`);
  },

  /**
   * Get a page of sequence data.
   */
  async getSequencePage(
    traceId: string,
    page: number = 1,
    pageSize: number = 10000
  ): Promise<SequencePage> {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    return api.get<SequencePage>(
      `/api/v1/traces/${traceId}/sequence/page?${params}`
    );
  },

  /**
   * List available analysis results for a trace.
   */
  async listAnalysisResults(traceId: string): Promise<AnalysisResultsListResponse> {
    return api.get<AnalysisResultsListResponse>(
      `/api/v1/traces/${traceId}/analysis`
    );
  },

  /**
   * Get a specific analysis result.
   */
  async getAnalysisResult<T = unknown>(traceId: string, analysisType: string): Promise<T> {
    return api.get<T>(`/api/v1/traces/${traceId}/analysis/${analysisType}`);
  },

  // ===========================================================================
  // TRIMS (multiple trims per trace)
  // ===========================================================================

  /**
   * Get all trims for a trace.
   */
  async getTrims(
    studyId: string,
    traceId: string,
    activeOnly: boolean = true
  ): Promise<TraceTrim[]> {
    const params = new URLSearchParams({ activeOnly: activeOnly.toString() });
    return api.get<TraceTrim[]>(
      `/api/v1/studies/${studyId}/traces/${traceId}/trims?${params}`
    );
  },

  /**
   * Add a new trim to a trace.
   */
  async addTrim(
    studyId: string,
    traceId: string,
    input: AddTrimInput
  ): Promise<TraceTrim> {
    return api.post<TraceTrim>(
      `/api/v1/studies/${studyId}/traces/${traceId}/trims`,
      input
    );
  },

  /**
   * Apply auto-trim using quality-based algorithm.
   * Creates separate trims for 5' and 3' ends.
   */
  async autoTrim(
    studyId: string,
    traceId: string,
    options?: {
      qualityThreshold?: number;
      windowSize?: number;
      minimumLength?: number;
    }
  ): Promise<TraceTrim[]> {
    return api.post<TraceTrim[]>(
      `/api/v1/studies/${studyId}/traces/${traceId}/trims/auto`,
      {
        qualityThreshold: options?.qualityThreshold ?? 20,
        windowSize: options?.windowSize ?? 10,
        minimumLength: options?.minimumLength ?? 50,
      }
    );
  },

  /**
   * Undo a specific trim.
   */
  async undoTrim(studyId: string, traceId: string, trimId: string): Promise<void> {
    return api.delete<void>(
      `/api/v1/studies/${studyId}/traces/${traceId}/trims/${trimId}`
    );
  },

  /**
   * Undo all active trims on a trace.
   */
  async undoAllTrims(studyId: string, traceId: string): Promise<void> {
    return api.delete<void>(
      `/api/v1/studies/${studyId}/traces/${traceId}/trims`
    );
  },

  /**
   * Get trimmed sequence with all active trims applied.
   */
  async getTrimmedSequence(studyId: string, traceId: string): Promise<TrimmedSequence> {
    return api.get<TrimmedSequence>(
      `/api/v1/studies/${studyId}/traces/${traceId}/sequence/trimmed`
    );
  },

  // ===========================================================================
  // ANNOTATIONS
  // ===========================================================================

  /**
   * Get all annotations for a trace.
   */
  async getAnnotations(studyId: string, traceId: string): Promise<TraceAnnotation[]> {
    return api.get<TraceAnnotation[]>(
      `/api/v1/studies/${studyId}/traces/${traceId}/annotations`
    );
  },

  /**
   * Create a new annotation.
   */
  async createAnnotation(
    studyId: string,
    traceId: string,
    input: CreateAnnotationInput
  ): Promise<TraceAnnotation> {
    return api.post<TraceAnnotation>(
      `/api/v1/studies/${studyId}/traces/${traceId}/annotations`,
      input
    );
  },

  /**
   * Update an annotation.
   */
  async updateAnnotation(
    studyId: string,
    traceId: string,
    annotationId: string,
    input: UpdateAnnotationInput
  ): Promise<TraceAnnotation> {
    return api.put<TraceAnnotation>(
      `/api/v1/studies/${studyId}/traces/${traceId}/annotations/${annotationId}`,
      input
    );
  },

  /**
   * Delete an annotation.
   */
  async deleteAnnotation(
    studyId: string,
    traceId: string,
    annotationId: string
  ): Promise<void> {
    return api.delete<void>(
      `/api/v1/studies/${studyId}/traces/${traceId}/annotations/${annotationId}`
    );
  },
};

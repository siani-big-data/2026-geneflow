import { tracesApi } from "@/mocks/api";
import type { Trace, TraceFilters, PaginatedResponse } from "@/types";

export const tracesService = {
  getAll(
    filters?: TraceFilters,
    page?: number,
    limit?: number
  ): Promise<PaginatedResponse<Trace>> {
    return tracesApi.getAll(filters, page, limit);
  },

  getById(id: string): Promise<Trace | null> {
    return tracesApi.getById(id);
  },

  getByStudyId(studyId: string): Promise<Trace[]> {
    return tracesApi.getByStudyId(studyId);
  },

  getStats() {
    return tracesApi.getStats();
  },
};

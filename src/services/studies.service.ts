import { studiesApi } from "@/mocks/api";
import type { Study, StudyFilters, PaginatedResponse } from "@/types";

export const studiesService = {
  getAll(
    filters?: StudyFilters,
    page?: number,
    limit?: number
  ): Promise<PaginatedResponse<Study>> {
    return studiesApi.getAll(filters, page, limit);
  },

  getById(id: string): Promise<Study | null> {
    return studiesApi.getById(id);
  },

  getStats() {
    return studiesApi.getStats();
  },
};

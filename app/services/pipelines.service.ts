import { pipelinesApi } from "@/mocks/api";
import type { Pipeline, PipelineFilters, PaginatedResponse } from "@/types";

export const pipelinesService = {
  getAll(
    filters?: PipelineFilters,
    page?: number,
    limit?: number
  ): Promise<PaginatedResponse<Pipeline>> {
    return pipelinesApi.getAll(filters, page, limit);
  },

  getById(id: string): Promise<Pipeline | null> {
    return pipelinesApi.getById(id);
  },

  getRunning(): Promise<Pipeline[]> {
    return pipelinesApi.getRunning();
  },

  getStats() {
    return pipelinesApi.getStats();
  },
};

"use client";

import { useQuery } from "@tanstack/react-query";
import { pipelinesService } from "@/services";
import type { PipelineFilters } from "@/types";

export function usePipelines(filters?: PipelineFilters, page = 1, limit = 10) {
  return useQuery({
    queryKey: ["pipelines", filters, page, limit],
    queryFn: () => pipelinesService.getAll(filters, page, limit),
  });
}

export function usePipeline(id: string) {
  return useQuery({
    queryKey: ["pipeline", id],
    queryFn: () => pipelinesService.getById(id),
    enabled: !!id,
  });
}

export function useRunningPipelines() {
  return useQuery({
    queryKey: ["pipelines", "running"],
    queryFn: () => pipelinesService.getRunning(),
    refetchInterval: 5000,
  });
}

export function usePipelineStats() {
  return useQuery({
    queryKey: ["pipelines", "stats"],
    queryFn: () => pipelinesService.getStats(),
  });
}

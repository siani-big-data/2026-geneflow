"use client";

import { useQuery } from "@tanstack/react-query";
import { tracesService } from "@/services";
import type { TraceFilters } from "@/types";

export function useTraces(filters?: TraceFilters, page = 1, limit = 10) {
  return useQuery({
    queryKey: ["traces", filters, page, limit],
    queryFn: () => tracesService.getAll(filters, page, limit),
  });
}

export function useTrace(id: string) {
  return useQuery({
    queryKey: ["trace", id],
    queryFn: () => tracesService.getById(id),
    enabled: !!id,
  });
}

export function useStudyTraces(studyId: string) {
  return useQuery({
    queryKey: ["traces", "study", studyId],
    queryFn: () => tracesService.getByStudyId(studyId),
    enabled: !!studyId,
  });
}

export function useTraceStats() {
  return useQuery({
    queryKey: ["traces", "stats"],
    queryFn: () => tracesService.getStats(),
  });
}

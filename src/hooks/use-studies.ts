"use client";

import { useQuery } from "@tanstack/react-query";
import { studiesService } from "@/services";
import type { StudyFilters } from "@/types";

export function useStudies(filters?: StudyFilters, page = 1, limit = 10) {
  return useQuery({
    queryKey: ["studies", filters, page, limit],
    queryFn: () => studiesService.getAll(filters, page, limit),
  });
}

export function useStudy(id: string) {
  return useQuery({
    queryKey: ["study", id],
    queryFn: () => studiesService.getById(id),
    enabled: !!id,
  });
}

export function useStudyStats() {
  return useQuery({
    queryKey: ["studies", "stats"],
    queryFn: () => studiesService.getStats(),
  });
}

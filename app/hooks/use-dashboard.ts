"use client";

import { useQuery } from "@tanstack/react-query";
import { dashboardService } from "@/services";

export function useDashboardOverview() {
  return useQuery({
    queryKey: ["dashboard", "overview"],
    queryFn: () => dashboardService.getOverview(),
  });
}

export function useRecentActivity() {
  return useQuery({
    queryKey: ["dashboard", "activity"],
    queryFn: () => dashboardService.getRecentActivity(),
  });
}

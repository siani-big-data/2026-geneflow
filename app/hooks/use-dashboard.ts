"use client";

import { useQuery } from "@tanstack/react-query";
import { dashboardService } from "@/services";
import type { DashboardStats, ActivityItem } from "@/types";

/**
 * Fetch dashboard statistics for the current user.
 */
export function useDashboardOverview() {
  return useQuery<DashboardStats>({
    queryKey: ["dashboard", "overview"],
    queryFn: () => dashboardService.getOverview(),
    staleTime: 1000 * 60 * 2, // 2 minutes
  });
}

/**
 * Fetch recent activity feed for the current user.
 */
export function useRecentActivity(limit = 20) {
  return useQuery<ActivityItem[]>({
    queryKey: ["dashboard", "activity", limit],
    queryFn: () => dashboardService.getRecentActivity(limit),
    staleTime: 1000 * 60 * 1, // 1 minute
  });
}

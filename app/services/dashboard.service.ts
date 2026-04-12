import { api } from "@/lib/api-client";
import type { DashboardStats, ActivityItem } from "@/types";

export const dashboardService = {
  /**
   * Get dashboard statistics for the current user.
   */
  async getOverview(): Promise<DashboardStats> {
    return api.get<DashboardStats>("/api/v1/dashboard/stats");
  },

  /**
   * Get recent activity feed for the current user.
   */
  async getRecentActivity(limit = 20): Promise<ActivityItem[]> {
    const params = new URLSearchParams({ limit: limit.toString() });
    return api.get<ActivityItem[]>(`/api/v1/dashboard/activity?${params}`);
  },
};

import { api } from "@/lib/api-client";
import type { DashboardStats, ActivityItem } from "@/types";

export const dashboardService = {
  async getOverview(): Promise<DashboardStats> {
    return api.get<DashboardStats>("/api/v1/usage/dashboard");
  },

  async getRecentActivity(_limit = 20): Promise<ActivityItem[]> {
    // TODO: Implement when backend activity endpoint is available
    // return api.get<ActivityItem[]>(`/api/v1/usage/activity?limit=${limit}`);
    return [];
  },
};

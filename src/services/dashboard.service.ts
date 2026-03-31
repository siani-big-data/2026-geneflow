import { dashboardApi } from "@/mocks/api";

export const dashboardService = {
  getOverview() {
    return dashboardApi.getOverview();
  },

  getRecentActivity() {
    return dashboardApi.getRecentActivity();
  },
};

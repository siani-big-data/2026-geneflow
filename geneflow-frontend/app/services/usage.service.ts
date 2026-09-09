/**
 * Usage statistics API service.
 */

import { api } from "@/lib/api-client";
import type { BillingUsage, DashboardStats } from "@/types";

// =============================================================================
// API ENDPOINTS
// =============================================================================

const USAGE_BASE = "/api/v1/usage";

// =============================================================================
// USAGE SERVICE
// =============================================================================

export const usageService = {
  /**
   * Get billing usage statistics for the current user.
   * Returns studies, traces, and members usage with limits.
   */
  async getBillingUsage(): Promise<BillingUsage> {
    const response = await api.get<BillingUsage>(`${USAGE_BASE}/billing`);
    return response;
  },

  /**
   * Get dashboard statistics for the current user.
   * Returns active studies, processed traces, and other metrics.
   */
  async getDashboardStats(): Promise<DashboardStats> {
    const response = await api.get<DashboardStats>(`${USAGE_BASE}/dashboard`);
    return response;
  },
};

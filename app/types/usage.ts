/**
 * Usage statistics types.
 */

// =============================================================================
// BILLING USAGE
// =============================================================================

/**
 * A single usage item showing used vs total.
 */
export interface UsageItem {
  used: number;
  total: number;
  percentage: number;
}

/**
 * Billing period information.
 */
export interface BillingPeriod {
  startDate: string;
  endDate: string;
  daysRemaining: number;
  totalDays: number;
}

/**
 * Complete billing usage response.
 */
export interface BillingUsage {
  studies: UsageItem;
  traces: UsageItem;
  members: UsageItem;
  period: BillingPeriod;
}

// =============================================================================
// DASHBOARD STATS
// =============================================================================

/**
 * Dashboard statistics for the user.
 */
export interface DashboardStats {
  activeStudies: number;
  processedTraces: number;
  pendingTraces: number;
  teamActivity: number;
  alignmentsCompleted: number;
}

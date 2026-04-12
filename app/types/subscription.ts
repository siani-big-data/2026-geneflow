/**
 * Subscription and Plan types.
 */

// =============================================================================
// PLAN TYPES
// =============================================================================

export interface PlanPricing {
  monthlyPrice: number;
  annualPrice: number;
  currency: string;
}

export interface PlanLimits {
  maxStudies: number;
  maxTracesPerMonth: number;
  maxMembersPerStudy: number;
}

export interface Plan {
  id: string;
  name: string;
  description: string | null;
  pricing: PlanPricing;
  limits: PlanLimits;
  features: string[];
  isActive: boolean;
  isDefault: boolean;
  isFree: boolean;
  displayOrder: number;
}

// =============================================================================
// SUBSCRIPTION TYPES
// =============================================================================

export type SubscriptionStatus =
  | "Active"
  | "Cancelled"
  | "Expired"
  | "Trial"
  | "PendingPayment"
  | "Suspended";

export type BillingCycle = "Monthly" | "Yearly";

export interface SubscriptionPeriod {
  startDate: string;
  endDate: string;
  daysRemaining: number;
}

export interface Subscription {
  id: string;
  userId: string;
  planId: string;
  planName: string;
  status: SubscriptionStatus;
  billingCycle: BillingCycle;
  currentPeriod: SubscriptionPeriod;
  autoRenew: boolean;
  grantsAccess: boolean;
  isFree: boolean;
  isInTrial: boolean;
  trialEndDate: string | null;
  cancelledAt: string | null;
  cancellationReason: string | null;
  createdAt: string;
  modifiedAt: string | null;
}

export interface SubscriptionSummary {
  id: string;
  planName: string;
  status: SubscriptionStatus;
  startDate: string;
  endDate: string;
  grantsAccess: boolean;
}

// =============================================================================
// REQUEST TYPES
// =============================================================================

export interface CreateSubscriptionRequest {
  planId: string;
  billingCycleId: string;
  startWithTrial?: boolean;
}

export interface CancelSubscriptionRequest {
  reason?: string;
}

export interface ChangePlanRequest {
  newPlanId: string;
  billingCycleId: string;
}

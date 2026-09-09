/**
 * Subscription and Plan API service.
 */

import { api } from "@/lib/api-client";
import type {
  Plan,
  Subscription,
  SubscriptionSummary,
  CreateSubscriptionRequest,
  CancelSubscriptionRequest,
  ChangePlanRequest,
} from "@/types";

// =============================================================================
// API ENDPOINTS
// =============================================================================

const PLANS_BASE = "/api/v1/plans";
const SUBSCRIPTIONS_BASE = "/api/v1/subscriptions";

// =============================================================================
// PLAN SERVICE
// =============================================================================

export const planService = {
  /**
   * Get all active plans.
   */
  async getAll(): Promise<Plan[]> {
    const response = await api.get<Plan[]>(PLANS_BASE);
    return response;
  },

  /**
   * Get a specific plan by ID.
   */
  async getById(planId: string): Promise<Plan> {
    const response = await api.get<Plan>(`${PLANS_BASE}/${planId}`);
    return response;
  },
};

// =============================================================================
// SUBSCRIPTION SERVICE
// =============================================================================

export const subscriptionService = {
  /**
   * Get the current user's active subscription.
   */
  async getCurrent(): Promise<Subscription | null> {
    try {
      const response = await api.get<Subscription>(`${SUBSCRIPTIONS_BASE}/current`);
      return response;
    } catch (error) {
      // 404 means no subscription, return null
      if (error instanceof Error && error.message.includes("404")) {
        return null;
      }
      throw error;
    }
  },

  /**
   * Get the user's subscription history.
   */
  async getHistory(): Promise<SubscriptionSummary[]> {
    const response = await api.get<SubscriptionSummary[]>(`${SUBSCRIPTIONS_BASE}/history`);
    return response;
  },

  /**
   * Create a new subscription.
   */
  async create(data: CreateSubscriptionRequest): Promise<Subscription> {
    const response = await api.post<Subscription>(SUBSCRIPTIONS_BASE, data);
    return response;
  },

  /**
   * Cancel the current subscription.
   */
  async cancel(data?: CancelSubscriptionRequest): Promise<void> {
    await api.post(`${SUBSCRIPTIONS_BASE}/cancel`, data || {});
  },

  /**
   * Change to a different plan.
   */
  async changePlan(data: ChangePlanRequest): Promise<Subscription> {
    const response = await api.post<Subscription>(`${SUBSCRIPTIONS_BASE}/change-plan`, data);
    return response;
  },
};

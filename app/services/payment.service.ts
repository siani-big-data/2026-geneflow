/**
 * Payment Methods Service for GeneFlow.
 *
 * Handles all payment method-related API calls:
 * - Setup Intent creation (for Stripe.js)
 * - Payment methods CRUD
 * - Default payment method management
 */

import { api } from "@/lib/api-client";
import type {
  PaymentMethod,
  PaymentMethodSummary,
  SetupIntent,
  AddPaymentMethodRequest,
  PaymentMethodApiResponse,
  SetupIntentApiResponse,
} from "@/types";
import { toPaymentMethod, toPaymentMethodSummary } from "@/types";

export const paymentService = {
  // ===========================================================================
  // SETUP INTENT
  // ===========================================================================

  /**
   * Create a Stripe SetupIntent for adding a new payment method.
   * Returns a clientSecret for use with Stripe.js confirmCardSetup().
   */
  async createSetupIntent(): Promise<SetupIntent> {
    const response = await api.get<SetupIntentApiResponse>(
      "/api/v1/payment-methods/setup-intent"
    );
    return { clientSecret: response.clientSecret };
  },

  // ===========================================================================
  // PAYMENT METHODS
  // ===========================================================================

  /**
   * Get all payment methods for the current user.
   */
  async getAll(): Promise<PaymentMethodSummary[]> {
    const response = await api.get<PaymentMethodApiResponse[]>(
      "/api/v1/payment-methods"
    );
    return response.map(toPaymentMethodSummary);
  },

  /**
   * Get the default payment method for the current user.
   * Returns null if no default is set.
   */
  async getDefault(): Promise<PaymentMethod | null> {
    try {
      const response = await api.get<PaymentMethodApiResponse>(
        "/api/v1/payment-methods/default"
      );
      return toPaymentMethod(response);
    } catch (error) {
      // 404 means no default payment method
      if ((error as { status?: number }).status === 404) {
        return null;
      }
      throw error;
    }
  },

  /**
   * Add a new payment method using a Stripe PaymentMethod ID.
   * This should be called after Stripe.js confirmCardSetup() succeeds.
   *
   * @param data - The Stripe paymentMethodId and whether to set as default
   */
  async add(data: AddPaymentMethodRequest): Promise<PaymentMethod> {
    const response = await api.post<PaymentMethodApiResponse>(
      "/api/v1/payment-methods",
      data
    );
    return toPaymentMethod(response);
  },

  /**
   * Set a payment method as the default for the current user.
   *
   * @param paymentMethodId - The ID of the payment method to set as default
   */
  async setDefault(paymentMethodId: string): Promise<void> {
    return api.post(`/api/v1/payment-methods/${paymentMethodId}/set-default`);
  },

  /**
   * Remove a payment method.
   * Cannot remove the default payment method if it's the only one.
   *
   * @param paymentMethodId - The ID of the payment method to remove
   */
  async remove(paymentMethodId: string): Promise<void> {
    return api.delete(`/api/v1/payment-methods/${paymentMethodId}`);
  },
};

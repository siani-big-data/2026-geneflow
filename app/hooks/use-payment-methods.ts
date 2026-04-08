"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { paymentService } from "@/services";
import type { AddPaymentMethodRequest, PaymentMethod } from "@/types";

// Query keys for cache management
export const paymentMethodKeys = {
  all: ["payment-methods"] as const,
  default: ["payment-methods", "default"] as const,
  setupIntent: ["payment-methods", "setup-intent"] as const,
};

/**
 * Get all payment methods for the current user.
 */
export function usePaymentMethods() {
  return useQuery({
    queryKey: paymentMethodKeys.all,
    queryFn: () => paymentService.getAll(),
  });
}

/**
 * Get the default payment method for the current user.
 */
export function useDefaultPaymentMethod() {
  return useQuery({
    queryKey: paymentMethodKeys.default,
    queryFn: () => paymentService.getDefault(),
  });
}

/**
 * Create a Stripe SetupIntent for adding a new payment method.
 * Use the clientSecret with Stripe.js confirmCardSetup().
 */
export function useSetupIntent() {
  return useQuery({
    queryKey: paymentMethodKeys.setupIntent,
    queryFn: () => paymentService.createSetupIntent(),
    // Don't cache setup intents - always get a fresh one
    staleTime: 0,
    gcTime: 0,
  });
}

/**
 * Add a new payment method after Stripe.js confirmCardSetup() succeeds.
 */
export function useAddPaymentMethod() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: AddPaymentMethodRequest) => paymentService.add(data),
    onSuccess: (newPaymentMethod: PaymentMethod) => {
      // Invalidate and refetch payment methods list
      queryClient.invalidateQueries({ queryKey: paymentMethodKeys.all });

      // If this is the default, also invalidate the default query
      if (newPaymentMethod.isDefault) {
        queryClient.invalidateQueries({ queryKey: paymentMethodKeys.default });
      }

      // Invalidate setup intent so we get a fresh one next time
      queryClient.invalidateQueries({ queryKey: paymentMethodKeys.setupIntent });
    },
  });
}

/**
 * Set a payment method as the default.
 */
export function useSetDefaultPaymentMethod() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (paymentMethodId: string) =>
      paymentService.setDefault(paymentMethodId),
    onSuccess: () => {
      // Invalidate both lists since default status changed
      queryClient.invalidateQueries({ queryKey: paymentMethodKeys.all });
      queryClient.invalidateQueries({ queryKey: paymentMethodKeys.default });
    },
  });
}

/**
 * Remove a payment method.
 */
export function useRemovePaymentMethod() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (paymentMethodId: string) =>
      paymentService.remove(paymentMethodId),
    onSuccess: () => {
      // Invalidate both lists
      queryClient.invalidateQueries({ queryKey: paymentMethodKeys.all });
      queryClient.invalidateQueries({ queryKey: paymentMethodKeys.default });
    },
  });
}

"use client";

import { useState } from "react";
import {
  PaymentElement,
  useStripe,
  useElements,
} from "@stripe/react-stripe-js";
import { Loader2, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui";
import { useAddPaymentMethod } from "@/hooks";

interface AddPaymentMethodFormProps {
  onSuccess?: () => void;
  onCancel?: () => void;
  setAsDefault?: boolean;
}

export function AddPaymentMethodForm({
  onSuccess,
  onCancel,
  setAsDefault = true,
}: AddPaymentMethodFormProps) {
  const stripe = useStripe();
  const elements = useElements();
  const addPaymentMethod = useAddPaymentMethod();

  const [error, setError] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!stripe || !elements) {
      setError("Stripe has not been initialized");
      return;
    }

    setError(null);
    setIsProcessing(true);

    try {
      // Confirm the SetupIntent with Stripe.js
      const { error: confirmError, setupIntent } = await stripe.confirmSetup({
        elements,
        redirect: "if_required",
      });

      if (confirmError) {
        setError(confirmError.message || "An error occurred while processing your card");
        setIsProcessing(false);
        return;
      }

      if (!setupIntent || setupIntent.status !== "succeeded") {
        setError("Card setup did not complete successfully");
        setIsProcessing(false);
        return;
      }

      // Get the payment method ID from the setup intent
      const paymentMethodId =
        typeof setupIntent.payment_method === "string"
          ? setupIntent.payment_method
          : setupIntent.payment_method?.id;

      if (!paymentMethodId) {
        setError("Failed to get payment method ID");
        setIsProcessing(false);
        return;
      }

      // Save the payment method to our backend
      await addPaymentMethod.mutateAsync({
        stripePaymentMethodId: paymentMethodId,
        setAsDefault,
      });

      onSuccess?.();
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "An unexpected error occurred";
      setError(message);
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Stripe Payment Element */}
      <div className="rounded-lg border border-border p-4">
        <PaymentElement
          options={{
            layout: "tabs",
          }}
        />
      </div>

      {/* Error Message */}
      {error && (
        <div className="flex items-center gap-2 rounded-lg border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
          <AlertCircle className="h-4 w-4 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {/* Actions */}
      <div className="flex items-center justify-end gap-3">
        <Button type="button" variant="ghost" onClick={onCancel} disabled={isProcessing}>
          Cancel
        </Button>
        <Button type="submit" disabled={!stripe || !elements || isProcessing}>
          {isProcessing ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Processing...
            </>
          ) : (
            "Add Card"
          )}
        </Button>
      </div>
    </form>
  );
}

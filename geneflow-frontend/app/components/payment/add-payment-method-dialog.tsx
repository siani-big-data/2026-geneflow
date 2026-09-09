"use client";

import { useEffect, useState } from "react";
import { Loader2, CreditCard, AlertTriangle } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui";
import { Button } from "@/components/ui";
import { useSetupIntent } from "@/hooks";
import { StripeProvider } from "./stripe-provider";
import { AddPaymentMethodForm } from "./add-payment-method-form";

// Check if Stripe is configured
const isStripeConfigured = !!process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY;

interface AddPaymentMethodDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess?: () => void;
}

export function AddPaymentMethodDialog({
  open,
  onOpenChange,
  onSuccess,
}: AddPaymentMethodDialogProps) {
  const [isReady, setIsReady] = useState(false);
  const { data: setupIntent, isLoading, error, refetch } = useSetupIntent();

  // Reset state when dialog opens
  useEffect(() => {
    if (open && isStripeConfigured) {
      setIsReady(false);
      refetch().then(() => setIsReady(true));
    }
  }, [open, refetch]);

  const handleSuccess = () => {
    onOpenChange(false);
    onSuccess?.();
  };

  const handleCancel = () => {
    onOpenChange(false);
  };

  // Show configuration message if Stripe is not set up
  if (!isStripeConfigured) {
    return (
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-[450px]">
          <DialogHeader>
            <DialogTitle>Payment Methods</DialogTitle>
            <DialogDescription>
              Configure payment processing to add payment methods.
            </DialogDescription>
          </DialogHeader>

          <div className="py-6">
            <div className="flex flex-col items-center gap-4 rounded-lg border border-dashed border-border p-8 text-center">
              <div className="rounded-full bg-muted p-3">
                <CreditCard className="h-6 w-6 text-muted-foreground" />
              </div>
              <div>
                <h3 className="text-sm font-medium text-foreground">
                  Stripe Not Configured
                </h3>
                <p className="mt-1 text-xs text-muted-foreground">
                  Payment methods require Stripe integration.
                  <br />
                  Add <code className="rounded bg-muted px-1">NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY</code> to enable.
                </p>
              </div>
              <Button variant="outline" size="sm" onClick={handleCancel}>
                Close
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Add Payment Method</DialogTitle>
          <DialogDescription>
            Add a new credit or debit card to your account. Your card information
            is securely processed by Stripe.
          </DialogDescription>
        </DialogHeader>

        <div className="py-4">
          {isLoading || !isReady ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-teal" />
            </div>
          ) : error ? (
            <div className="flex flex-col items-center gap-3 rounded-lg border border-amber-500/50 bg-amber-500/10 p-6 text-center">
              <AlertTriangle className="h-8 w-8 text-amber-500" />
              <div>
                <p className="text-sm font-medium text-foreground">
                  Payment Setup Unavailable
                </p>
                <p className="mt-1 text-xs text-muted-foreground">
                  Unable to initialize payment form. This may be due to missing Stripe configuration on the server.
                </p>
              </div>
              <Button variant="outline" size="sm" onClick={handleCancel}>
                Close
              </Button>
            </div>
          ) : setupIntent?.clientSecret ? (
            <StripeProvider clientSecret={setupIntent.clientSecret}>
              <AddPaymentMethodForm
                onSuccess={handleSuccess}
                onCancel={handleCancel}
                setAsDefault
              />
            </StripeProvider>
          ) : (
            <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-center text-sm text-destructive">
              Unable to load payment form. Please try again later.
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}

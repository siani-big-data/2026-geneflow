"use client";

import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui";
import { useSetupIntent } from "@/hooks";
import { StripeProvider } from "./stripe-provider";
import { AddPaymentMethodForm } from "./add-payment-method-form";

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
    if (open) {
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
            <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-center text-sm text-destructive">
              Failed to initialize payment form. Please try again.
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

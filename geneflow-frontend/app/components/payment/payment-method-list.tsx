"use client";

import { useState } from "react";
import { CreditCard, Plus, Loader2 } from "lucide-react";
import { Button } from "@/components/ui";
import {
  usePaymentMethods,
  useSetDefaultPaymentMethod,
  useRemovePaymentMethod,
} from "@/hooks";
import { PaymentMethodCard } from "./payment-method-card";
import { AddPaymentMethodDialog } from "./add-payment-method-dialog";

interface PaymentMethodListProps {
  showAddButton?: boolean;
  onPaymentMethodAdded?: () => void;
}

export function PaymentMethodList({
  showAddButton = true,
  onPaymentMethodAdded,
}: PaymentMethodListProps) {
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [settingDefaultId, setSettingDefaultId] = useState<string | null>(null);
  const [removingId, setRemovingId] = useState<string | null>(null);

  const { data: paymentMethods, isLoading, error } = usePaymentMethods();
  const setDefault = useSetDefaultPaymentMethod();
  const remove = useRemovePaymentMethod();

  const handleSetDefault = async (id: string) => {
    setSettingDefaultId(id);
    try {
      await setDefault.mutateAsync(id);
    } finally {
      setSettingDefaultId(null);
    }
  };

  const handleRemove = async (id: string) => {
    setRemovingId(id);
    try {
      await remove.mutateAsync(id);
    } finally {
      setRemovingId(null);
    }
  };

  const handleAddSuccess = () => {
    onPaymentMethodAdded?.();
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-6 w-6 animate-spin text-teal" />
      </div>
    );
  }

  // Payment methods endpoint not yet implemented - show coming soon
  if (error) {
    return (
      <div className="rounded-lg border border-dashed border-border p-6 text-center">
        <CreditCard className="mx-auto h-8 w-8 text-muted-foreground" />
        <h3 className="mt-3 text-sm font-medium text-foreground">
          Payment Methods
        </h3>
        <p className="mt-1 text-xs text-muted-foreground">
          Coming soon. Free plan active.
        </p>
      </div>
    );
  }

  const hasPaymentMethods = paymentMethods && paymentMethods.length > 0;

  return (
    <div className="space-y-4">
      {hasPaymentMethods ? (
        <div className="space-y-3">
          {paymentMethods.map((method) => (
            <PaymentMethodCard
              key={method.id}
              paymentMethod={method}
              onSetDefault={handleSetDefault}
              onRemove={handleRemove}
              isSettingDefault={settingDefaultId === method.id}
              isRemoving={removingId === method.id}
              disabled={settingDefaultId !== null || removingId !== null}
            />
          ))}
        </div>
      ) : (
        <div className="rounded-lg border border-dashed border-border p-8 text-center">
          <CreditCard className="mx-auto h-10 w-10 text-muted-foreground" />
          <h3 className="mt-4 text-sm font-medium text-foreground">
            No payment methods
          </h3>
          <p className="mt-1 text-sm text-muted-foreground">
            Add a payment method to enable automatic billing.
          </p>
        </div>
      )}

      {showAddButton && (
        <Button
          variant="outline"
          size="sm"
          className="w-full"
          onClick={() => setAddDialogOpen(true)}
        >
          <Plus className="mr-2 h-4 w-4" />
          Add Payment Method
        </Button>
      )}

      <AddPaymentMethodDialog
        open={addDialogOpen}
        onOpenChange={setAddDialogOpen}
        onSuccess={handleAddSuccess}
      />
    </div>
  );
}

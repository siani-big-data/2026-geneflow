"use client";

import { CreditCard, Trash2, Star, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import type { PaymentMethodSummary } from "@/types";
import { Button } from "@/components/ui";

// Card brand logos (using simple text for now, could use SVG icons)
const brandIcons: Record<string, string> = {
  visa: "VISA",
  mastercard: "MC",
  amex: "AMEX",
  discover: "DISC",
  diners: "DC",
  jcb: "JCB",
  unionpay: "UP",
};

interface PaymentMethodCardProps {
  paymentMethod: PaymentMethodSummary;
  onSetDefault?: (id: string) => void;
  onRemove?: (id: string) => void;
  isSettingDefault?: boolean;
  isRemoving?: boolean;
  disabled?: boolean;
}

export function PaymentMethodCard({
  paymentMethod,
  onSetDefault,
  onRemove,
  isSettingDefault,
  isRemoving,
  disabled,
}: PaymentMethodCardProps) {
  const { id, cardLast4, cardBrand, formattedExpiration, isDefault, canCharge } = paymentMethod;

  const brandLabel = brandIcons[cardBrand.toLowerCase()] || cardBrand.toUpperCase();
  const hasIssue = !canCharge;

  return (
    <div
      className={cn(
        "flex items-center gap-4 rounded-lg border p-4 transition-colors",
        isDefault && "border-teal bg-teal/5",
        hasIssue && "border-destructive/50 bg-destructive/5",
        !isDefault && !hasIssue && "border-border"
      )}
    >
      {/* Card Icon */}
      <div
        className={cn(
          "flex h-10 w-14 items-center justify-center rounded text-xs font-bold",
          hasIssue ? "bg-destructive/10 text-destructive" : "bg-muted text-muted-foreground"
        )}
      >
        {brandLabel}
      </div>

      {/* Card Info */}
      <div className="flex-1">
        <div className="flex items-center gap-2">
          <p className="text-sm font-medium text-foreground">
            **** **** **** {cardLast4}
          </p>
          {isDefault && (
            <span className="rounded bg-teal/10 px-2 py-0.5 text-xs font-medium text-teal">
              Default
            </span>
          )}
          {hasIssue && (
            <span className="rounded bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive">
              Issue
            </span>
          )}
        </div>
        <p className="text-xs text-muted-foreground">
          Expires {formattedExpiration}
        </p>
      </div>

      {/* Actions */}
      <div className="flex items-center gap-2">
        {!isDefault && onSetDefault && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onSetDefault(id)}
            disabled={disabled || isSettingDefault || hasIssue}
            title="Set as default"
          >
            {isSettingDefault ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Star className="h-4 w-4" />
            )}
          </Button>
        )}
        {onRemove && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onRemove(id)}
            disabled={disabled || isRemoving || isDefault}
            title={isDefault ? "Cannot remove default payment method" : "Remove"}
            className="text-muted-foreground hover:text-destructive"
          >
            {isRemoving ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Trash2 className="h-4 w-4" />
            )}
          </Button>
        )}
      </div>
    </div>
  );
}

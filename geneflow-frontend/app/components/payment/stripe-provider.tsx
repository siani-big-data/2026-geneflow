"use client";

import { Elements } from "@stripe/react-stripe-js";
import { loadStripe, type Stripe, type Appearance } from "@stripe/stripe-js";
import { useMemo, type ReactNode } from "react";
import { useTheme } from "@/providers/theme-provider";

// Singleton promise for loading Stripe
let stripePromise: Promise<Stripe | null> | null = null;

function getStripePromise() {
  if (!stripePromise) {
    const publishableKey = process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY;
    if (!publishableKey) {
      console.error("[Stripe] Missing NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY");
      return null;
    }
    stripePromise = loadStripe(publishableKey);
  }
  return stripePromise;
}

// Stripe appearance for light mode
const lightAppearance: Appearance = {
  theme: "stripe",
  variables: {
    colorPrimary: "#14b8a6",
    colorBackground: "#ffffff",
    colorText: "#1f2937",
    colorTextSecondary: "#6b7280",
    colorTextPlaceholder: "#9ca3af",
    colorDanger: "#ef4444",
    colorSuccess: "#10b981",
    fontFamily: "system-ui, -apple-system, sans-serif",
    fontSizeBase: "14px",
    borderRadius: "8px",
    spacingUnit: "4px",
  },
  rules: {
    ".Input": {
      border: "1px solid #e5e7eb",
      boxShadow: "0 1px 2px 0 rgba(0, 0, 0, 0.05)",
    },
    ".Input:focus": {
      border: "1px solid #14b8a6",
      boxShadow: "0 0 0 3px rgba(20, 184, 166, 0.1)",
    },
    ".Label": {
      fontWeight: "500",
      marginBottom: "6px",
    },
    ".Tab": {
      border: "1px solid #e5e7eb",
      borderRadius: "8px",
    },
    ".Tab--selected": {
      borderColor: "#14b8a6",
      backgroundColor: "rgba(20, 184, 166, 0.05)",
    },
  },
};

// Stripe appearance for dark mode
const darkAppearance: Appearance = {
  theme: "night",
  variables: {
    colorPrimary: "#2dd4bf",
    colorBackground: "#1f2937",
    colorText: "#f9fafb",
    colorTextSecondary: "#9ca3af",
    colorTextPlaceholder: "#6b7280",
    colorDanger: "#f87171",
    colorSuccess: "#34d399",
    fontFamily: "system-ui, -apple-system, sans-serif",
    fontSizeBase: "14px",
    borderRadius: "8px",
    spacingUnit: "4px",
  },
  rules: {
    ".Input": {
      border: "1px solid #374151",
      backgroundColor: "#111827",
      boxShadow: "none",
    },
    ".Input:focus": {
      border: "1px solid #2dd4bf",
      boxShadow: "0 0 0 3px rgba(45, 212, 191, 0.15)",
    },
    ".Label": {
      fontWeight: "500",
      marginBottom: "6px",
      color: "#e5e7eb",
    },
    ".Tab": {
      border: "1px solid #374151",
      borderRadius: "8px",
      backgroundColor: "#111827",
    },
    ".Tab--selected": {
      borderColor: "#2dd4bf",
      backgroundColor: "rgba(45, 212, 191, 0.1)",
    },
    ".Tab:hover": {
      backgroundColor: "#1f2937",
    },
    ".Error": {
      color: "#f87171",
    },
  },
};

interface StripeProviderProps {
  children: ReactNode;
  clientSecret?: string;
}

/**
 * Wraps children with Stripe Elements provider.
 * Automatically adapts to light/dark theme.
 */
export function StripeProvider({ children, clientSecret }: StripeProviderProps) {
  const { resolvedTheme } = useTheme();
  const stripePromise = useMemo(() => getStripePromise(), []);

  const appearance = resolvedTheme === "dark" ? darkAppearance : lightAppearance;

  if (!stripePromise) {
    return (
      <div className="flex items-center justify-center p-8 text-sm text-muted-foreground">
        Stripe is not configured. Please check your environment variables.
      </div>
    );
  }

  const options = clientSecret
    ? {
        clientSecret,
        appearance,
      }
    : undefined;

  return (
    <Elements stripe={stripePromise} options={options} key={resolvedTheme}>
      {children}
    </Elements>
  );
}

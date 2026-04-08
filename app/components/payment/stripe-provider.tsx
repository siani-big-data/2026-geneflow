"use client";

import { Elements } from "@stripe/react-stripe-js";
import { loadStripe, type Stripe } from "@stripe/stripe-js";
import { useMemo, type ReactNode } from "react";

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

interface StripeProviderProps {
  children: ReactNode;
  clientSecret?: string;
}

/**
 * Wraps children with Stripe Elements provider.
 * If clientSecret is provided, uses it for SetupIntent mode.
 */
export function StripeProvider({ children, clientSecret }: StripeProviderProps) {
  const stripePromise = useMemo(() => getStripePromise(), []);

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
        appearance: {
          theme: "stripe" as const,
          variables: {
            colorPrimary: "#14b8a6", // teal
            colorBackground: "#ffffff",
            colorText: "#1f2937",
            colorDanger: "#ef4444",
            fontFamily: "system-ui, sans-serif",
            borderRadius: "8px",
          },
        },
      }
    : undefined;

  return (
    <Elements stripe={stripePromise} options={options}>
      {children}
    </Elements>
  );
}

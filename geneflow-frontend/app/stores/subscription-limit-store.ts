import { create } from "zustand";

/**
 * Error codes emitted by the backend `SubscriptionLimitBehavior`.
 * Centralized here so the FE can match them without string-typing.
 */
export const SUBSCRIPTION_LIMIT_CODES = [
  "Subscription.NoActiveSubscription",
  "Subscription.StudyLimitReached",
  "Subscription.TraceLimitReached",
  "Subscription.MemberLimitReached",
] as const;

export type SubscriptionLimitCode = (typeof SUBSCRIPTION_LIMIT_CODES)[number];

/** Returns true when `code` is one of the recognised subscription-limit codes. */
export function isSubscriptionLimitCode(
  code: string | undefined | null,
): code is SubscriptionLimitCode {
  if (!code) return false;
  return (SUBSCRIPTION_LIMIT_CODES as readonly string[]).includes(code);
}

interface SubscriptionLimitError {
  code: SubscriptionLimitCode;
  /** Server-provided message; used as fallback when no i18n key matches. */
  message: string;
}

interface SubscriptionLimitState {
  isOpen: boolean;
  error: SubscriptionLimitError | null;
  open: (error: SubscriptionLimitError) => void;
  close: () => void;
}

/**
 * Holds the currently active subscription-limit error so any mutation can raise
 * it and a single global dialog renders the upgrade CTA. Hooked up from
 * `QueryProvider`'s MutationCache `onError`.
 */
export const useSubscriptionLimitStore = create<SubscriptionLimitState>(
  (set) => ({
    isOpen: false,
    error: null,
    open: (error) => set({ isOpen: true, error }),
    close: () => set({ isOpen: false }),
  }),
);

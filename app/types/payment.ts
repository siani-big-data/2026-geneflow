/**
 * Payment-related types for GeneFlow.
 * Integrates with Stripe for secure payment processing.
 */

// =============================================================================
// CARD BRANDS
// =============================================================================

export type CardBrand =
  | "visa"
  | "mastercard"
  | "amex"
  | "discover"
  | "diners"
  | "jcb"
  | "unionpay"
  | "unknown";

// =============================================================================
// PAYMENT METHOD STATUS
// =============================================================================

export type PaymentMethodStatus = "Active" | "Expired" | "Failed";

// =============================================================================
// RESPONSE TYPES
// =============================================================================

export interface CardDetails {
  last4: string;
  brand: string;
  expMonth: number;
  expYear: number;
  formattedExpiration: string;
  displayName: string;
  isExpired: boolean;
}

export interface BillingAddress {
  country: string | null;
  postalCode: string | null;
}

export interface PaymentMethod {
  id: string;
  userId: string;
  card: CardDetails;
  billingAddress: BillingAddress | null;
  status: PaymentMethodStatus;
  isDefault: boolean;
  canCharge: boolean;
  createdAt: string;
  modifiedAt: string | null;
}

export interface PaymentMethodSummary {
  id: string;
  cardLast4: string;
  cardBrand: string;
  formattedExpiration: string;
  isDefault: boolean;
  canCharge: boolean;
}

export interface SetupIntent {
  clientSecret: string;
  setupIntentId: string;
}

// =============================================================================
// REQUEST TYPES
// =============================================================================

export interface AddPaymentMethodRequest {
  stripePaymentMethodId: string;
  setAsDefault?: boolean;
}

// =============================================================================
// STATE TYPES
// =============================================================================

export interface PaymentMethodsState {
  paymentMethods: PaymentMethodSummary[];
  defaultPaymentMethod: PaymentMethod | null;
  isLoading: boolean;
  error: string | null;
}

// =============================================================================
// STRIPE TYPES
// =============================================================================

export interface StripeConfig {
  publishableKey: string;
}

// =============================================================================
// SUBSCRIPTION WITH PAYMENT
// =============================================================================

export interface SubscriptionPaymentInfo {
  defaultPaymentMethodId: string | null;
  hasPaymentMethod: boolean;
}

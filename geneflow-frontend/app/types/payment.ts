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
// API RESPONSE TYPES (from backend)
// =============================================================================

/** Raw response from the backend API */
export interface PaymentMethodApiResponse {
  id: string;
  brand: string;
  last4: string;
  expiryMonth: number;
  expiryYear: number;
  isDefault: boolean;
  createdAt: string;
}

/** Setup intent response from backend */
export interface SetupIntentApiResponse {
  clientSecret: string;
}

// =============================================================================
// FRONTEND TYPES (used by components)
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
  card: CardDetails;
  isDefault: boolean;
  canCharge: boolean;
  createdAt: string;
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
}

// =============================================================================
// HELPER FUNCTIONS
// =============================================================================

/** Format expiration date as MM/YY */
export function formatExpiration(month: number, year: number): string {
  const monthStr = month.toString().padStart(2, "0");
  const yearStr = year.toString().slice(-2);
  return `${monthStr}/${yearStr}`;
}

/** Check if card is expired */
export function isCardExpired(month: number, year: number): boolean {
  const now = new Date();
  const currentYear = now.getFullYear();
  const currentMonth = now.getMonth() + 1;
  return year < currentYear || (year === currentYear && month < currentMonth);
}

/** Transform API response to PaymentMethodSummary */
export function toPaymentMethodSummary(
  response: PaymentMethodApiResponse
): PaymentMethodSummary {
  return {
    id: response.id,
    cardLast4: response.last4,
    cardBrand: response.brand,
    formattedExpiration: formatExpiration(response.expiryMonth, response.expiryYear),
    isDefault: response.isDefault,
    canCharge: !isCardExpired(response.expiryMonth, response.expiryYear),
  };
}

/** Transform API response to full PaymentMethod */
export function toPaymentMethod(
  response: PaymentMethodApiResponse
): PaymentMethod {
  const expired = isCardExpired(response.expiryMonth, response.expiryYear);
  return {
    id: response.id,
    card: {
      last4: response.last4,
      brand: response.brand,
      expMonth: response.expiryMonth,
      expYear: response.expiryYear,
      formattedExpiration: formatExpiration(response.expiryMonth, response.expiryYear),
      displayName: `${response.brand.toUpperCase()} ****${response.last4}`,
      isExpired: expired,
    },
    isDefault: response.isDefault,
    canCharge: !expired,
    createdAt: response.createdAt,
  };
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

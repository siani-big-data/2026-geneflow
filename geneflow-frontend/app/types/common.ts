// =============================================================================
// COMMON TYPES - Shared across services
// =============================================================================

/**
 * Standard paginated response from the API.
 * Matches the PagedList<T> response from the backend.
 */
export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/**
 * Standard API error response.
 */
export interface ApiError {
  message: string;
  code: string;
  status: number;
  details?: Record<string, string[]>;
}

/**
 * Select option for dropdown components.
 */
export interface SelectOption {
  value: string;
  label: string;
}

/**
 * Sort option for table sorting.
 */
export interface SortOption {
  field: string;
  direction: "asc" | "desc";
}

/**
 * Date range for filtering.
 */
export interface DateRange {
  from: Date;
  to: Date;
}

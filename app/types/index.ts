export * from "./user";
export * from "./study";
export * from "./trace";
export * from "./pipeline";
export * from "./auth";
export * from "./profile";

export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  limit: number;
  totalPages: number;
}

export interface ApiError {
  message: string;
  code: string;
  status: number;
  details?: Record<string, string[]>;
}

export interface SelectOption {
  value: string;
  label: string;
}

export interface SortOption {
  field: string;
  direction: "asc" | "desc";
}

export interface DateRange {
  from: Date;
  to: Date;
}

export * from "./user";
export * from "./study";
export * from "./trace";
export * from "./pipeline";
export * from "./auth";
export * from "./profile";
export * from "./payment";
export * from "./subscription";
export * from "./usage";
export * from "./dashboard";
export * from "./analysis";
export * from "./activity";
export * from "./discussions";
export * from "./notifications";
export * from "./common";

// Legacy compatibility - prefer PagedResponse from common.ts
export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  limit: number;
  totalPages: number;
}

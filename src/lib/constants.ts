export const APP_NAME = "GeneFlow";

export const ROUTES = {
  HOME: "/",
  LOGIN: "/login",
  REGISTER: "/register",
  DASHBOARD: "/dashboard",
  DISCOVER: "/discover",
  STUDIES: "/studies",
  STUDY_DETAIL: (id: string) => `/studies/${id}`,
  TRACES: "/traces",
  TRACE_DETAIL: (id: string) => `/traces/${id}`,
  PIPELINES: "/pipelines",
  ANALYSIS: "/analysis",
  PROFILE: "/profile",
  USER_PROFILE: (id: string) => `/users/${id}`,
  SETTINGS: "/settings",
  BILLING: "/settings/billing",
  HELP: "/help",
} as const;

export const STUDY_STATUS = {
  DRAFT: "draft",
  ACTIVE: "active",
  COMPLETED: "completed",
  ARCHIVED: "archived",
} as const;

export const TRACE_STATUS = {
  PENDING: "pending",
  PROCESSING: "processing",
  COMPLETED: "completed",
  FAILED: "failed",
} as const;

export const PIPELINE_STATUS = {
  QUEUED: "queued",
  RUNNING: "running",
  COMPLETED: "completed",
  FAILED: "failed",
  CANCELLED: "cancelled",
} as const;

export const PLAN_TIERS = {
  FREE: "free",
  PRO: "pro",
  ENTERPRISE: "enterprise",
} as const;

export const PAGINATION = {
  DEFAULT_PAGE: 1,
  DEFAULT_LIMIT: 10,
  MAX_LIMIT: 100,
} as const;

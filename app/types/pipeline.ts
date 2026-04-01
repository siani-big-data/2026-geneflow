import type { User } from "./user";

export interface Pipeline {
  id: string;
  name: string;
  description?: string;
  status: PipelineStatus;
  type: PipelineType;
  studyId: string;
  studyName: string;
  traceIds: string[];
  progress: number;
  startedAt?: string;
  completedAt?: string;
  createdBy: User;
  createdAt: string;
  updatedAt: string;
  error?: string;
}

export type PipelineStatus = "queued" | "running" | "completed" | "failed" | "cancelled";

export type PipelineType = "alignment" | "blast" | "annotation" | "quality-check" | "export";

export interface PipelineStep {
  id: string;
  name: string;
  status: PipelineStepStatus;
  progress: number;
  startedAt?: string;
  completedAt?: string;
  error?: string;
}

export type PipelineStepStatus = "pending" | "running" | "completed" | "failed" | "skipped";

export interface PipelineResult {
  id: string;
  pipelineId: string;
  type: PipelineResultType;
  data: Record<string, unknown>;
  createdAt: string;
}

export type PipelineResultType = "alignment" | "blast-hits" | "annotations" | "quality-report" | "export-file";

export interface PipelineFilters {
  studyId?: string;
  status?: PipelineStatus;
  type?: PipelineType;
  search?: string;
}

export interface CreatePipelineInput {
  name: string;
  description?: string;
  type: PipelineType;
  studyId: string;
  traceIds: string[];
  config?: Record<string, unknown>;
}

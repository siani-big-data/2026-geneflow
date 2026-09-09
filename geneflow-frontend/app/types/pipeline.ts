/**
 * Pipeline types matching the backend API responses.
 */

// ============== Pipeline ==============

export interface Pipeline {
  id: string;
  studyId: string;
  ownerId: string;
  name: string;
  description?: string;
  statusId: number;
  statusName: PipelineStatusName;
  canBeEdited: boolean;
  canBeExecuted: boolean;
  stepCount: number;
  enabledStepCount: number;
  steps: PipelineStep[];
  createdAt: string;
  createdBy?: string;
  modifiedAt?: string;
  modifiedBy?: string;
}

export interface PipelineSummary {
  id: string;
  name: string;
  description?: string;
  statusId: number;
  statusName: PipelineStatusName;
  stepCount: number;
  enabledStepCount: number;
  createdAt: string;
  modifiedAt?: string;
}

export type PipelineStatusName = "Draft" | "Active" | "Archived";

export interface PipelineStatus {
  id: number;
  name: PipelineStatusName;
}

export const PipelineStatuses: PipelineStatus[] = [
  { id: 1, name: "Draft" },
  { id: 2, name: "Active" },
  { id: 3, name: "Archived" },
];

// ============== Pipeline Step ==============

export interface PipelineStep {
  id: string;
  stepTypeId: number;
  stepTypeName: string;
  stepTypeDisplayName: string;
  order: number;
  label?: string;
  configuration?: Record<string, unknown>;
  isEnabled: boolean;
  displayName: string;
  createdAt: string;
}

export interface StepType {
  id: number;
  name: string;
  displayName: string;
  analysisKey: string;
  requiresConfiguration: boolean;
  configurationSchema: string;
}

// ============== Pipeline Execution ==============

export interface PipelineExecution {
  id: string;
  pipelineId: string;
  pipelineName: string;
  traceId: string;
  startedById: string;
  statusId: number;
  statusName: ExecutionStatusName;
  isInProgress: boolean;
  isTerminal: boolean;
  totalSteps: number;
  completedSteps: number;
  progressPercentage: number;
  errorMessage?: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  durationSeconds?: number;
  stepExecutions: StepExecution[];
}

export interface PipelineExecutionSummary {
  id: string;
  pipelineId: string;
  pipelineName: string;
  traceId: string;
  statusId: number;
  statusName: ExecutionStatusName;
  totalSteps: number;
  completedSteps: number;
  progressPercentage: number;
  createdAt: string;
  completedAt?: string;
  durationSeconds?: number;
}

/**
 * Pipeline execution summary used by the dashboard's "recent pipelines" lateral panel.
 * Includes study identity so the UI can deep-link to the originating study.
 */
export interface RecentPipelineExecution {
  id: string;
  pipelineId: string;
  pipelineName: string;
  traceId: string;
  studyId: string;
  studyTitle: string;
  statusId: number;
  statusName: string;
  totalSteps: number;
  completedSteps: number;
  progressPercentage: number;
  createdAt: string;
  completedAt?: string;
  durationSeconds?: number;
}

export type ExecutionStatusName = "Pending" | "Running" | "Completed" | "Failed" | "Cancelled";

export interface ExecutionStatus {
  id: number;
  name: ExecutionStatusName;
}

export const ExecutionStatuses: ExecutionStatus[] = [
  { id: 1, name: "Pending" },
  { id: 2, name: "Running" },
  { id: 3, name: "Completed" },
  { id: 4, name: "Failed" },
  { id: 5, name: "Cancelled" },
];

// ============== Step Execution ==============

export interface StepExecution {
  id: string;
  pipelineStepId: string;
  order: number;
  stepTypeId: number;
  stepTypeName: string;
  stepTypeDisplayName: string;
  statusId: number;
  statusName: StepExecutionStatusName;
  isInProgress: boolean;
  isSuccess: boolean;
  startedAt?: string;
  completedAt?: string;
  durationSeconds?: number;
  errorMessage?: string;
  resultSummary?: string;
  resultData?: string;
}

export type StepExecutionStatusName = "Pending" | "Running" | "Completed" | "Failed" | "Skipped";

// ============== Request Types ==============

export interface CreatePipelineInput {
  name: string;
  description?: string;
}

export interface UpdatePipelineInput {
  name: string;
  description?: string;
}

export interface AddPipelineStepInput {
  stepTypeId: number;
  label?: string;
  configuration?: string;
  isEnabled?: boolean;
}

export interface UpdatePipelineStepInput {
  label?: string;
  configuration?: string;
  isEnabled?: boolean;
}

export interface ReorderStepsInput {
  stepIds: string[];
}

export interface ExecutePipelineInput {
  traceId: string;
}

// ============== Filter Types ==============

export interface PipelineFilters {
  searchTerm?: string;
  statusId?: number;
}

export interface ExecutionFilters {
  statusId?: number;
}

// ============== Stats (for dashboard) ==============

export interface PipelineStats {
  total: number;
  active: number;
  draft: number;
  archived: number;
  runningExecutions: number;
}

import type {
  Study,
  Trace,
  Pipeline,
  PaginatedResponse,
  StudyFilters,
  TraceFilters,
  PipelineFilters,
} from "@/types";
import {
  mockStudies,
  mockTraces,
  mockPipelines,
  getStudyById,
  getTraceById,
  getPipelineById,
  filterStudies,
  filterTraces,
  filterPipelines,
} from "./data";

const SIMULATED_DELAY = 300;

function delay(ms: number = SIMULATED_DELAY): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function paginate<T>(
  items: T[],
  page: number = 1,
  limit: number = 10
): PaginatedResponse<T> {
  const start = (page - 1) * limit;
  const end = start + limit;
  const data = items.slice(start, end);
  const total = items.length;
  const totalPages = Math.ceil(total / limit);

  return {
    data,
    total,
    page,
    limit,
    totalPages,
  };
}

// Studies API
export const studiesApi = {
  async getAll(
    filters?: StudyFilters,
    page: number = 1,
    limit: number = 10
  ): Promise<PaginatedResponse<Study>> {
    await delay();
    const filtered = filters ? filterStudies(filters) : mockStudies;
    return paginate(filtered, page, limit);
  },

  async getById(id: string): Promise<Study | null> {
    await delay();
    return getStudyById(id) || null;
  },

  async getStats(): Promise<{
    total: number;
    active: number;
    completed: number;
    draft: number;
  }> {
    await delay(200);
    return {
      total: mockStudies.length,
      active: mockStudies.filter((s) => s.status === "active").length,
      completed: mockStudies.filter((s) => s.status === "completed").length,
      draft: mockStudies.filter((s) => s.status === "draft").length,
    };
  },
};

// Traces API
export const tracesApi = {
  async getAll(
    filters?: TraceFilters,
    page: number = 1,
    limit: number = 10
  ): Promise<PaginatedResponse<Trace>> {
    await delay();
    const filtered = filters ? filterTraces(filters) : mockTraces;
    return paginate(filtered, page, limit);
  },

  async getById(id: string): Promise<Trace | null> {
    await delay();
    return getTraceById(id) || null;
  },

  async getByStudyId(studyId: string): Promise<Trace[]> {
    await delay();
    return mockTraces.filter((t) => t.studyId === studyId);
  },

  async getStats(): Promise<{
    total: number;
    pending: number;
    processing: number;
    completed: number;
    failed: number;
  }> {
    await delay(200);
    return {
      total: mockTraces.length,
      pending: mockTraces.filter((t) => t.status === "pending").length,
      processing: mockTraces.filter((t) => t.status === "processing").length,
      completed: mockTraces.filter((t) => t.status === "completed").length,
      failed: mockTraces.filter((t) => t.status === "failed").length,
    };
  },
};

// Pipelines API
export const pipelinesApi = {
  async getAll(
    filters?: PipelineFilters,
    page: number = 1,
    limit: number = 10
  ): Promise<PaginatedResponse<Pipeline>> {
    await delay();
    const filtered = filters ? filterPipelines(filters) : mockPipelines;
    return paginate(filtered, page, limit);
  },

  async getById(id: string): Promise<Pipeline | null> {
    await delay();
    return getPipelineById(id) || null;
  },

  async getRunning(): Promise<Pipeline[]> {
    await delay(200);
    return mockPipelines.filter(
      (p) => p.status === "running" || p.status === "queued"
    );
  },

  async getStats(): Promise<{
    total: number;
    running: number;
    queued: number;
    completed: number;
    failed: number;
  }> {
    await delay(200);
    return {
      total: mockPipelines.length,
      running: mockPipelines.filter((p) => p.status === "running").length,
      queued: mockPipelines.filter((p) => p.status === "queued").length,
      completed: mockPipelines.filter((p) => p.status === "completed").length,
      failed: mockPipelines.filter((p) => p.status === "failed").length,
    };
  },
};

// Dashboard API
export const dashboardApi = {
  async getOverview(): Promise<{
    studies: { active: number; total: number };
    traces: { total: number; thisWeek: number };
    pipelines: { running: number; completed: number };
    analyses: { completed: number };
  }> {
    await delay(250);
    return {
      studies: {
        active: mockStudies.filter((s) => s.status === "active").length,
        total: mockStudies.length,
      },
      traces: {
        total: mockTraces.length,
        thisWeek: 5,
      },
      pipelines: {
        running: mockPipelines.filter(
          (p) => p.status === "running" || p.status === "queued"
        ).length,
        completed: mockPipelines.filter((p) => p.status === "completed").length,
      },
      analyses: {
        completed: 89,
      },
    };
  },

  async getRecentActivity(): Promise<
    Array<{
      id: string;
      type: "study" | "trace" | "pipeline";
      action: string;
      subject: string;
      timestamp: string;
    }>
  > {
    await delay(200);
    return [
      {
        id: "activity-1",
        type: "trace",
        action: "uploaded",
        subject: "BRCA1_Sample_001.ab1",
        timestamp: "2024-03-29T10:30:00Z",
      },
      {
        id: "activity-2",
        type: "pipeline",
        action: "started",
        subject: "BRCA1 Alignment Run",
        timestamp: "2024-03-29T10:00:00Z",
      },
      {
        id: "activity-3",
        type: "study",
        action: "updated",
        subject: "BRCA1 Mutation Analysis",
        timestamp: "2024-03-28T16:45:00Z",
      },
      {
        id: "activity-4",
        type: "pipeline",
        action: "completed",
        subject: "Quality Check Batch",
        timestamp: "2024-03-28T09:15:00Z",
      },
      {
        id: "activity-5",
        type: "trace",
        action: "processed",
        subject: "COVID_Variant_Delta_01.fasta",
        timestamp: "2024-03-27T14:05:00Z",
      },
    ];
  },
};

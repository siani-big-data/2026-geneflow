import type { Pipeline, PipelineStatus, PipelineType } from "@/types";
import { mockUsers } from "./users";

export interface PipelineMetrics {
  cpu: number;
  memory: number;
  samplesProcessed: number;
  samplesTotal: number;
  eta?: string;
}

export interface PipelineWithMetrics extends Pipeline {
  metrics?: PipelineMetrics;
}

export const mockPipelines: PipelineWithMetrics[] = [
  {
    id: "PL-2026-0234",
    name: "Quality Control & Trimming",
    description: "Running QC and trimming for all samples in the study",
    status: "running",
    type: "quality-check",
    studyId: "study-1",
    studyName: "GF-2026-089",
    traceIds: Array.from({ length: 834 }, (_, i) => `trace-${i}`),
    progress: 67,
    startedAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
    createdBy: mockUsers[0],
    createdAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date().toISOString(),
    metrics: {
      cpu: 78,
      memory: 62,
      samplesProcessed: 834,
      samplesTotal: 1247,
      eta: "45 min",
    },
  },
  {
    id: "PL-2026-0233",
    name: "BWA Alignment",
    description: "Aligning reads against reference genome GRCh38",
    status: "running",
    type: "alignment",
    studyId: "study-2",
    studyName: "GF-2026-087",
    traceIds: Array.from({ length: 305 }, (_, i) => `trace-${i}`),
    progress: 89,
    startedAt: new Date(Date.now() - 5 * 60 * 60 * 1000).toISOString(),
    createdBy: mockUsers[0],
    createdAt: new Date(Date.now() - 5 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date().toISOString(),
    metrics: {
      cpu: 92,
      memory: 84,
      samplesProcessed: 305,
      samplesTotal: 342,
      eta: "12 min",
    },
  },
  {
    id: "PL-2026-0231",
    name: "Variant Calling - GATK",
    description: "Running GATK HaplotypeCaller for variant detection",
    status: "queued",
    type: "annotation",
    studyId: "study-3",
    studyName: "GF-2026-078",
    traceIds: Array.from({ length: 2341 }, (_, i) => `trace-${i}`),
    progress: 0,
    createdBy: mockUsers[1],
    createdAt: new Date(Date.now() - 30 * 60 * 1000).toISOString(),
    updatedAt: new Date().toISOString(),
    metrics: {
      cpu: 0,
      memory: 0,
      samplesProcessed: 0,
      samplesTotal: 2341,
      eta: "Waiting",
    },
  },
  {
    id: "PL-2026-0230",
    name: "RNA-Seq Quantification",
    description: "Gene expression quantification using STAR aligner",
    status: "running",
    type: "alignment",
    studyId: "study-4",
    studyName: "GF-2026-068",
    traceIds: Array.from({ length: 73 }, (_, i) => `trace-${i}`),
    progress: 34,
    startedAt: new Date(Date.now() - 1 * 60 * 60 * 1000).toISOString(),
    createdBy: mockUsers[0],
    createdAt: new Date(Date.now() - 1 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date().toISOString(),
    metrics: {
      cpu: 65,
      memory: 71,
      samplesProcessed: 73,
      samplesTotal: 214,
      eta: "2 hr 15 min",
    },
  },
  {
    id: "PL-2026-0228",
    name: "Annotation & Filtering",
    description: "Variant annotation using VEP and filtering by MAF",
    status: "running",
    type: "annotation",
    studyId: "study-5",
    studyName: "GF-2026-075",
    traceIds: Array.from({ length: 295 }, (_, i) => `trace-${i}`),
    progress: 52,
    startedAt: new Date(Date.now() - 3 * 60 * 60 * 1000).toISOString(),
    createdBy: mockUsers[1],
    createdAt: new Date(Date.now() - 3 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date().toISOString(),
    metrics: {
      cpu: 45,
      memory: 38,
      samplesProcessed: 295,
      samplesTotal: 567,
      eta: "1 hr 30 min",
    },
  },
  {
    id: "PL-2026-0226",
    name: "De Novo Assembly",
    description: "De novo genome assembly using SPAdes",
    status: "failed",
    type: "alignment",
    studyId: "study-3",
    studyName: "GF-2026-078",
    traceIds: Array.from({ length: 538 }, (_, i) => `trace-${i}`),
    progress: 23,
    startedAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
    completedAt: new Date(Date.now() - 5 * 60 * 60 * 1000).toISOString(),
    error: "Memory allocation failed: insufficient resources for contig assembly",
    createdBy: mockUsers[0],
    createdAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date(Date.now() - 5 * 60 * 60 * 1000).toISOString(),
    metrics: {
      cpu: 0,
      memory: 0,
      samplesProcessed: 538,
      samplesTotal: 2341,
    },
  },
  {
    id: "PL-2026-0225",
    name: "BLAST Search - NCBI",
    description: "Running BLAST against NCBI nucleotide database",
    status: "completed",
    type: "blast",
    studyId: "study-1",
    studyName: "GF-2026-089",
    traceIds: ["trace-1", "trace-2", "trace-3"],
    progress: 100,
    startedAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
    completedAt: new Date(Date.now() - 23 * 60 * 60 * 1000).toISOString(),
    createdBy: mockUsers[0],
    createdAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date(Date.now() - 23 * 60 * 60 * 1000).toISOString(),
    metrics: {
      cpu: 0,
      memory: 0,
      samplesProcessed: 156,
      samplesTotal: 156,
    },
  },
  {
    id: "PL-2026-0220",
    name: "Export to FASTA",
    description: "Exporting aligned sequences to FASTA format",
    status: "completed",
    type: "export",
    studyId: "study-2",
    studyName: "GF-2026-087",
    traceIds: ["trace-10", "trace-11"],
    progress: 100,
    startedAt: new Date(Date.now() - 48 * 60 * 60 * 1000).toISOString(),
    completedAt: new Date(Date.now() - 48 * 60 * 60 * 1000 + 5 * 60 * 1000).toISOString(),
    createdBy: mockUsers[1],
    createdAt: new Date(Date.now() - 48 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date(Date.now() - 48 * 60 * 60 * 1000 + 5 * 60 * 1000).toISOString(),
    metrics: {
      cpu: 0,
      memory: 0,
      samplesProcessed: 342,
      samplesTotal: 342,
    },
  },
];

// Resource usage data for charts
export const resourceUsageData = [
  { time: "10:00", cpu: 45, memory: 38 },
  { time: "11:00", cpu: 62, memory: 54 },
  { time: "12:00", cpu: 78, memory: 68 },
  { time: "13:00", cpu: 85, memory: 76 },
  { time: "14:00", cpu: 92, memory: 84 },
  { time: "15:00", cpu: 88, memory: 79 },
  { time: "16:00", cpu: 73, memory: 65 },
];

// Sample throughput data
export const throughputData = [
  { hour: "10am", samples: 245 },
  { hour: "11am", samples: 312 },
  { hour: "12pm", samples: 289 },
  { hour: "1pm", samples: 356 },
  { hour: "2pm", samples: 423 },
  { hour: "3pm", samples: 398 },
  { hour: "4pm", samples: 367 },
];

export function getPipelineById(id: string): PipelineWithMetrics | undefined {
  return mockPipelines.find((pipeline) => pipeline.id === id);
}

export function filterPipelines(filters: {
  studyId?: string;
  status?: PipelineStatus;
  type?: PipelineType;
  search?: string;
}): PipelineWithMetrics[] {
  return mockPipelines.filter((pipeline) => {
    if (filters.studyId && pipeline.studyId !== filters.studyId) return false;
    if (filters.status && pipeline.status !== filters.status) return false;
    if (filters.type && pipeline.type !== filters.type) return false;
    if (filters.search) {
      const search = filters.search.toLowerCase();
      return (
        pipeline.name.toLowerCase().includes(search) ||
        pipeline.description?.toLowerCase().includes(search)
      );
    }
    return true;
  });
}

export function getPipelineStats() {
  const running = mockPipelines.filter((p) => p.status === "running").length;
  const queued = mockPipelines.filter((p) => p.status === "queued").length;
  const completed = mockPipelines.filter((p) => p.status === "completed").length;
  const failed = mockPipelines.filter((p) => p.status === "failed").length;

  const runningPipelines = mockPipelines.filter((p) => p.status === "running");
  const avgCpu = runningPipelines.length > 0
    ? Math.round(runningPipelines.reduce((acc, p) => acc + (p.metrics?.cpu || 0), 0) / runningPipelines.length)
    : 0;
  const avgMemory = runningPipelines.length > 0
    ? Math.round(runningPipelines.reduce((acc, p) => acc + (p.metrics?.memory || 0), 0) / runningPipelines.length)
    : 0;

  const samplesProcessedToday = mockPipelines.reduce((acc, p) => acc + (p.metrics?.samplesProcessed || 0), 0);

  return {
    total: mockPipelines.length,
    running,
    queued,
    completed,
    failed,
    avgCpu,
    avgMemory,
    samplesProcessedToday,
  };
}

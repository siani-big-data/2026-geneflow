import type { Pipeline, PipelineStatus, PipelineType } from "@/types";
import { mockUsers } from "./users";

export const mockPipelines: Pipeline[] = [
  {
    id: "pipeline-1",
    name: "BRCA1 Alignment Run",
    description: "Aligning BRCA1 samples against reference genome GRCh38",
    status: "running",
    type: "alignment",
    studyId: "study-1",
    studyName: "BRCA1 Mutation Analysis",
    traceIds: ["trace-1", "trace-2", "trace-8"],
    progress: 67,
    startedAt: "2024-03-29T10:00:00Z",
    createdBy: mockUsers[0],
    createdAt: "2024-03-29T09:55:00Z",
    updatedAt: "2024-03-29T10:30:00Z",
  },
  {
    id: "pipeline-2",
    name: "COVID Variant BLAST Search",
    description: "Running BLAST against NCBI database for variant identification",
    status: "queued",
    type: "blast",
    studyId: "study-2",
    studyName: "COVID-19 Variant Sequencing",
    traceIds: ["trace-3"],
    progress: 0,
    createdBy: mockUsers[0],
    createdAt: "2024-03-29T11:00:00Z",
    updatedAt: "2024-03-29T11:00:00Z",
  },
  {
    id: "pipeline-3",
    name: "Quality Check Batch",
    description: "Quality assessment for newly uploaded traces",
    status: "completed",
    type: "quality-check",
    studyId: "study-1",
    studyName: "BRCA1 Mutation Analysis",
    traceIds: ["trace-1", "trace-2"],
    progress: 100,
    startedAt: "2024-03-28T09:00:00Z",
    completedAt: "2024-03-28T09:15:00Z",
    createdBy: mockUsers[0],
    createdAt: "2024-03-28T08:55:00Z",
    updatedAt: "2024-03-28T09:15:00Z",
  },
  {
    id: "pipeline-4",
    name: "Gene Annotation Pipeline",
    description: "Automated gene annotation using ensemble methods",
    status: "completed",
    type: "annotation",
    studyId: "study-3",
    studyName: "Rare Disease Gene Panel",
    traceIds: ["trace-5"],
    progress: 100,
    startedAt: "2024-02-10T11:15:00Z",
    completedAt: "2024-02-10T12:30:00Z",
    createdBy: mockUsers[0],
    createdAt: "2024-02-10T11:10:00Z",
    updatedAt: "2024-02-10T12:30:00Z",
  },
  {
    id: "pipeline-5",
    name: "Microbiome Analysis",
    description: "16S rRNA analysis for microbiome composition",
    status: "failed",
    type: "annotation",
    studyId: "study-4",
    studyName: "Microbiome Diversity Study",
    traceIds: ["trace-6", "trace-7"],
    progress: 23,
    startedAt: "2024-03-29T08:45:00Z",
    completedAt: "2024-03-29T09:00:00Z",
    error: "Insufficient quality scores in input traces",
    createdBy: mockUsers[1],
    createdAt: "2024-03-29T08:40:00Z",
    updatedAt: "2024-03-29T09:00:00Z",
  },
  {
    id: "pipeline-6",
    name: "Export to FASTA",
    description: "Converting AB1 traces to FASTA format for external analysis",
    status: "completed",
    type: "export",
    studyId: "study-1",
    studyName: "BRCA1 Mutation Analysis",
    traceIds: ["trace-1"],
    progress: 100,
    startedAt: "2024-03-27T16:00:00Z",
    completedAt: "2024-03-27T16:02:00Z",
    createdBy: mockUsers[0],
    createdAt: "2024-03-27T15:58:00Z",
    updatedAt: "2024-03-27T16:02:00Z",
  },
];

export function getPipelineById(id: string): Pipeline | undefined {
  return mockPipelines.find((pipeline) => pipeline.id === id);
}

export function filterPipelines(filters: {
  studyId?: string;
  status?: PipelineStatus;
  type?: PipelineType;
  search?: string;
}): Pipeline[] {
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

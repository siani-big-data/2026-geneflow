import type { User } from "./user";

export interface Trace {
  id: string;
  name: string;
  studyId: string;
  studyName: string;
  status: TraceStatus;
  fileType: TraceFileType;
  fileSize: number;
  sequenceLength?: number;
  qualityScore?: number;
  uploadedBy: User;
  createdAt: string;
  updatedAt: string;
  processedAt?: string;
}

export type TraceStatus = "pending" | "processing" | "completed" | "failed";

export type TraceFileType = "ab1" | "fasta" | "fastq" | "seq";

export interface TraceSequence {
  id: string;
  traceId: string;
  sequence: string;
  quality: number[];
  peaks?: TracePeak[];
  annotations: TraceAnnotation[];
}

export interface TracePeak {
  position: number;
  base: "A" | "T" | "G" | "C";
  quality: number;
  intensity: number;
}

export interface TraceAnnotation {
  id: string;
  traceId: string;
  startPosition: number;
  endPosition: number;
  type: AnnotationType;
  label: string;
  description?: string;
  createdBy: User;
  createdAt: string;
}

export type AnnotationType = "gene" | "mutation" | "primer" | "region" | "custom";

export interface TraceFilters {
  studyId?: string;
  status?: TraceStatus;
  fileType?: TraceFileType;
  search?: string;
}

export interface UploadTraceInput {
  studyId: string;
  file: File;
  name?: string;
}

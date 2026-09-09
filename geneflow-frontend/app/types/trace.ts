/**
 * Trace types matching GeneFlow.ApiNet2 backend responses.
 */

// =============================================================================
// TRACE SUMMARY (for list views)
// =============================================================================

export interface TraceSummary {
  id: string;
  studyId: string;
  name: string;
  description?: string;
  fileName: string;
  sizeBytes: number;
  format: string;
  formatId: number;
  status: string;
  statusId: number;
  averageQualityScore?: number;
  totalBases?: number;
  hasChromatogramData: boolean;
  processedAt?: string;
  createdAt: string;
  modifiedAt?: string;
}

// =============================================================================
// TRACE DETAIL (full response)
// =============================================================================

export interface Trace {
  id: string;
  studyId: string;
  uploadedBy: string;
  name: string;
  description?: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  format: string;
  formatId: number;
  status: string;
  statusId: number;
  qualityMetrics?: QualityMetrics;
  trims: TraceTrim[];
  activeTrimCount: number;
  hasChromatogramData: boolean;
  failureReason?: string;
  processedAt?: string;
  activeEditCount: number;
  annotationCount: number;
  createdAt: string;
  createdBy?: string;
  modifiedAt?: string;
  modifiedBy?: string;
}

export interface QualityMetrics {
  averageQuality: number;
  totalBases: number;
  goodQualityBases: number;
  lowQualityBases: number;
  ambiguousBases: number;
  gcContent: number;
}

// =============================================================================
// TRACE TRIM (multiple trims per trace)
// =============================================================================

export interface TraceTrim {
  id: string;
  trimType: string;
  trimTypeId: number;
  trimEnd: string;
  trimEndId: number;
  startPosition: number;
  endPosition: number;
  length: number;
  algorithm: string;
  reason?: string;
  appliedBy: string;
  appliedAt: string;
  isActive: boolean;
}

/** @deprecated Use TraceTrim instead - kept for backwards compatibility */
export interface TrimRegion {
  start5Prime: number;
  end5Prime: number;
  start3Prime: number;
  end3Prime: number;
  algorithm: string;
  trimmedBy?: string;
  trimmedAt: string;
  trimmedLength: number;
}

// =============================================================================
// TRACE COUNTS (stats by status)
// =============================================================================

export interface TraceCounts {
  total: number;
  uploaded: number;
  validating: number;
  processing: number;
  processed: number;
  failed: number;
  archived: number;
}

// =============================================================================
// TRACE SEQUENCE & ANNOTATIONS
// =============================================================================

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
  type: string;
  typeId: number;
  label: string;
  description?: string;
  strand: string;
  strandId: number;
  color: string;
  isShared: boolean;
  metadata?: Record<string, unknown>;
  createdAt: string;
  createdBy?: string;
  modifiedAt?: string;
  modifiedBy?: string;
}

// =============================================================================
// FILTERS & INPUTS
// =============================================================================

export interface TraceFilters {
  studyId?: string;
  statusId?: number;
  formatId?: number;
  searchTerm?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface UploadTraceInput {
  studyId: string;
  name: string;
  description?: string;
  file: File;
}

export interface UpdateTraceNameInput {
  name: string;
}

// =============================================================================
// TRIM INPUTS & RESPONSES
// =============================================================================

/** Input to add a new trim operation */
export interface AddTrimInput {
  startPosition: number;
  endPosition: number;
  trimEnd: "FivePrime" | "ThreePrime";
  reason?: string;
}

/** @deprecated Use AddTrimInput instead */
export interface ManualTrimInput {
  start5Prime: number;
  end5Prime: number;
  start3Prime: number;
  end3Prime: number;
}

export interface TrimmedSequence {
  traceId: string;
  originalSequence: string;
  trimmedSequence: string;
  appliedTrims: TraceTrim[];
  originalLength: number;
  trimmedLength: number;
  totalBasesTrimmed: number;
}

// =============================================================================
// TRIM ENUMS
// =============================================================================

export const TrimTypeId = {
  Manual: 1,
  AutoMott: 2,
  AutoWindow: 3,
} as const;

export const TrimTypeName: Record<number, string> = {
  1: "Manual",
  2: "AutoMott",
  3: "AutoWindow",
};

export const TrimEndId = {
  FivePrime: 1,
  ThreePrime: 2,
} as const;

export const TrimEndName: Record<number, string> = {
  1: "FivePrime",
  2: "ThreePrime",
};

export const TrimEndSymbol: Record<number, string> = {
  1: "5'",
  2: "3'",
};

// =============================================================================
// ANNOTATION INPUTS
// =============================================================================

export interface CreateAnnotationInput {
  typeId: number;
  label: string;
  description?: string;
  startPosition: number;
  endPosition: number;
  strandId?: number;
  color?: string;
  isShared?: boolean;
  metadata?: Record<string, unknown>;
}

export interface UpdateAnnotationInput {
  label: string;
  description?: string;
  startPosition: number;
  endPosition: number;
  strandId: number;
  color: string;
  isShared?: boolean;
  metadata?: Record<string, unknown>;
}

// =============================================================================
// ANNOTATION ENUMS
// =============================================================================

export const AnnotationTypeId = {
  Region: 1,
  Point: 2,
  Feature: 3,
  Custom: 4,
} as const;

export const AnnotationTypeName: Record<number, string> = {
  1: "Region",
  2: "Point",
  3: "Feature",
  4: "Custom",
};

export const AnnotationStrandId = {
  Plus: 1,
  Minus: 2,
  None: 3,
} as const;

export const AnnotationStrandSymbol: Record<number, string> = {
  1: "+",
  2: "-",
  3: ".",
};

// =============================================================================
// TRACE STATUS & FORMAT ENUMS
// =============================================================================

export const TraceStatus = {
  Uploaded: 1,
  Validating: 2,
  Processing: 3,
  Processed: 4,
  Failed: 5,
  Archived: 6,
} as const;

export const TraceStatusName: Record<number, string> = {
  1: "uploaded",
  2: "validating",
  3: "processing",
  4: "processed",
  5: "failed",
  6: "archived",
};

export const TraceFormat = {
  AB1: 1,
  FASTA: 2,
  FASTQ: 3,
  SEQ: 4,
  SCF: 5,
} as const;

export const TraceFormatName: Record<number, string> = {
  1: "ab1",
  2: "fasta",
  3: "fastq",
  4: "seq",
  5: "scf",
};

// =============================================================================
// TRACE MANIFEST & SEQUENCE PAGE (from datalake)
// =============================================================================

export interface TraceManifest {
  traceId: string;
  totalBases: number;
  chunkSize: number;
  chunkCount: number;
  hasQualityScores: boolean;
  hasChromatogram: boolean;
  format: string;
  processedAt: string;
}

export interface SequencePage {
  traceId: string;
  page: number;
  pageSize: number;
  totalBases: number;
  totalPages: number;
  bases: string;
  qualityScores?: number[];
  chromatogram?: ChromatogramChunk;
}

export interface ChromatogramChunk {
  // Backend sends PascalCase with "Channel" suffix
  aChannel?: number[];
  tChannel?: number[];
  gChannel?: number[];
  cChannel?: number[];
  peakPositions?: number[];
  // Legacy lowercase format (for backwards compatibility)
  a?: number[];
  t?: number[];
  g?: number[];
  c?: number[];
}

/**
 * Analysis types for GeneFlow Analysis module.
 */

// =============================================================================
// REQUESTS
// =============================================================================

export interface TrimmingRequest {
  algorithm?: string;
  qualityThreshold?: number;
  windowSize?: number;
  minLength?: number;
}

export interface HeterozygoteRequest {
  threshold?: number;
  minQuality?: number;
}

export interface MotifSearchRequest {
  pattern: string;
  caseSensitive?: boolean;
  searchBothStrands?: boolean;
  allowMismatches?: number;
}

export interface TranslationRequest {
  frame?: number;
  geneticCode?: string;
}

export interface ORFDetectionRequest {
  minLength?: number;
  startCodons?: string[];
  stopCodons?: string[];
  includeReverseStrand?: boolean;
}

export interface RestrictionAnalysisRequest {
  enzymes?: string[];
  minSiteLength?: number;
}

export interface AlignmentRequest {
  traceIds: string[];
  algorithm?: string;
}

// =============================================================================
// RESULTS
// =============================================================================

export interface TrimmingResult {
  algorithm: string;
  originalLength: number;
  trimmedLength: number;
  trimStart: number;
  trimEnd: number;
  trimmedSequence?: string;
}

export interface HeterozygoteCall {
  position: number;
  primaryBase: string;
  secondaryBase: string;
  primaryHeight: number;
  secondaryHeight: number;
  ratio: number;
}

export interface HeterozygoteResult {
  heterozygoteCount: number;
  calls: HeterozygoteCall[];
}

export interface MotifMatch {
  pattern: string;
  start: number;
  end: number;
  matchedSequence: string;
  strand: "+" | "-";
}

export interface MotifSearchResult {
  pattern: string;
  matchCount: number;
  matches: MotifMatch[];
}

export interface TranslationResult {
  frame: number;
  proteinLength: number;
  proteinSequence: string;
  stopCodonCount?: number;
  startCodonPositions?: number[];
  aminoAcidComposition?: Record<string, number>;
}

export interface ORF {
  start: number;
  end: number;
  frame: number;
  strand: "+" | "-";
  length: number;
  sequence?: string;
}

export interface ORFDetectionResult {
  totalOrfs: number;
  longestOrfLength: number;
  orfs: ORF[];
}

export interface RestrictionSite {
  enzyme: string;
  position: number;
  cutPosition: number;
  recognitionSequence: string;
  overhang?: string;
}

export interface RestrictionAnalysisResult {
  enzymeCount: number;
  totalSites: number;
  enzymesWithSites: string[];
  sites: RestrictionSite[];
}

// =============================================================================
// LIST RESPONSE
// =============================================================================

export type AnalysisType =
  | "trimming"
  | "heterozygote"
  | "motif"
  | "translation"
  | "orf"
  | "restriction";

export interface AnalysisResultsListResponse {
  traceId: string;
  availableTypes: string[];
}

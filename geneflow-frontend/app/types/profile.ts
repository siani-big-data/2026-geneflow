/**
 * Profile types for GeneFlow.
 *
 * Matches the backend API contracts from:
 * - ProfileResponse
 * - ProfileStatsResponse
 * - Update request types
 */

// =============================================================================
// PROFILE RESPONSE (from API)
// =============================================================================

export interface Profile {
  id: string;
  userId: string;
  firstName: string;
  lastName: string | null;
  fullName: string;
  initials: string;
  bio: string | null;
  location: string | null;
  professionalRole: string | null;
  institutionName: string | null;
  institutionDepartment: string | null;
  institutionDisplayName: string | null;
  researchField: string | null;
  orcidId: string | null;
  orcidUrl: string | null;
  website: string | null;
  photoUrl: string | null;
  photoThumbnailUrl: string | null;
  isComplete: boolean;
  createdAt: string;
  modifiedAt: string | null;
}

// =============================================================================
// PROFILE SUMMARY (for lists)
// =============================================================================

export interface ProfileSummary {
  id: string;
  userId: string;
  fullName: string;
  initials: string;
  professionalRole: string | null;
  institutionName: string | null;
  photoUrl: string | null;
  photoThumbnailUrl: string | null;
}

// =============================================================================
// PROFILE STATS
// =============================================================================

export interface ProfileStats {
  totalStudies: number;
  ownedStudies: number;
  totalTraces: number;
  totalAlignments: number;
  completedAlignments: number;
  lastActivityAt: string | null;
  memberSince: string;
}

// =============================================================================
// REQUEST TYPES
// =============================================================================

export interface UpdateProfileRequest {
  firstName: string;
  lastName?: string | null;
  bio?: string | null;
  location?: string | null;
  professionalRole?: string | null;
  institutionName?: string | null;
  institutionDepartment?: string | null;
  researchField?: string | null;
}

export interface CreateProfileRequest {
  firstName: string;
  lastName?: string | null;
  bio?: string | null;
  location?: string | null;
  professionalRole?: string | null;
  institutionName?: string | null;
  institutionDepartment?: string | null;
  researchField?: string | null;
  orcidId?: string | null;
  website?: string | null;
}

export interface UpdateResearchIdentifiersRequest {
  orcidId?: string | null;
  website?: string | null;
}

export interface UpdateProfilePhotoRequest {
  photoUrl: string;
  thumbnailUrl?: string | null;
}

// =============================================================================
// RESEARCH FIELDS (Smart Enum values from backend)
// =============================================================================

export const RESEARCH_FIELDS = [
  { id: 1, name: "Genomics", label: "Genomics" },
  { id: 2, name: "Proteomics", label: "Proteomics" },
  { id: 3, name: "Transcriptomics", label: "Transcriptomics" },
  { id: 4, name: "Bioinformatics", label: "Bioinformatics" },
  { id: 5, name: "MolecularBiology", label: "Molecular Biology" },
  { id: 6, name: "CellBiology", label: "Cell Biology" },
  { id: 7, name: "Genetics", label: "Genetics" },
  { id: 8, name: "Biochemistry", label: "Biochemistry" },
  { id: 9, name: "Microbiology", label: "Microbiology" },
  { id: 10, name: "Immunology", label: "Immunology" },
  { id: 11, name: "Neuroscience", label: "Neuroscience" },
  { id: 12, name: "PlantBiology", label: "Plant Biology" },
  { id: 13, name: "MarineBiology", label: "Marine Biology" },
  { id: 14, name: "Ecology", label: "Ecology" },
  { id: 15, name: "EvolutionaryBiology", label: "Evolutionary Biology" },
  { id: 16, name: "ComputationalBiology", label: "Computational Biology" },
  { id: 17, name: "SyntheticBiology", label: "Synthetic Biology" },
  { id: 18, name: "Other", label: "Other" },
] as const;

export type ResearchFieldName = (typeof RESEARCH_FIELDS)[number]["name"];

/**
 * Get the display label for a research field name.
 */
export function getResearchFieldLabel(name: string | null): string {
  if (!name) return "";
  const field = RESEARCH_FIELDS.find((f) => f.name === name);
  return field?.label ?? name;
}

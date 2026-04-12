// =============================================================================
// STUDY TYPES - Aligned with GeneFlow.ApiNet2 Backend API
// =============================================================================

// Study Status (matches backend StudyStatus enumeration)
export type StudyStatus = "draft" | "active" | "completed" | "published" | "archived";
export type StudyStatusId = 1 | 2 | 3 | 4 | 5;

// Study Role (matches backend StudyRole enumeration)
export type StudyRole = "owner" | "admin" | "editor" | "viewer";
export type StudyRoleId = 1 | 2 | 3 | 4;

// Invitation Status (matches backend InvitationStatus enumeration)
export type InvitationStatus = "pending" | "accepted" | "declined" | "expired" | "cancelled";

// Research Field (matches backend ResearchField enumeration)
export interface ResearchField {
  id: number;
  name: string;
  description?: string;
}

// =============================================================================
// STUDY SETTINGS & METRICS
// =============================================================================

export interface StudySettings {
  allowPublicComments: boolean;
  allowDataDownload: boolean;
  requireApprovalToJoin: boolean;
}

export interface StudyMetrics {
  viewsCount: number;
  starsCount: number;
}

// =============================================================================
// STUDY MEMBER
// =============================================================================

export interface StudyMember {
  userId: string;
  userName: string;
  userEmail: string;
  userAvatarUrl?: string;
  roleId: StudyRoleId;
  roleName: StudyRole;
  joinedAt: string;
  invitedBy?: string;
}

// =============================================================================
// STUDY PAPER
// =============================================================================

export interface StudyPaper {
  id: string;
  title: string;
  authors?: string;
  doi?: string;
  abstract?: string;
  journal?: string;
  publicationYear?: number;
  fileId?: string;
  fileName?: string;
  fileSizeBytes?: number;
  uploadedBy: string;
  uploadedAt: string;
}

// =============================================================================
// STUDY RESPONSES (from API)
// =============================================================================

/** Full study response with all details */
export interface Study {
  id: string;
  ownerId: string;
  title: string;
  description?: string;
  researchFieldId: number;
  researchFieldName: string;
  statusId: StudyStatusId;
  statusName: StudyStatus;
  institution?: string;
  principalInvestigator?: string;
  isFeatured: boolean;
  settings: StudySettings;
  metrics: StudyMetrics;
  tags: string[];
  members: StudyMember[];
  papers: StudyPaper[];
  createdAt: string;
  modifiedAt?: string;
}

/** Study summary for list views */
export interface StudySummary {
  id: string;
  ownerId: string;
  title: string;
  description?: string;
  researchFieldId: number;
  researchFieldName: string;
  statusId: StudyStatusId;
  statusName: StudyStatus;
  institution?: string;
  principalInvestigator?: string;
  isFeatured: boolean;
  memberCount: number;
  paperCount: number;
  viewsCount: number;
  starsCount: number;
  tags: string[];
  createdAt: string;
}

/** Study statistics response */
export interface StudyStats {
  viewsCount: number;
  starsCount: number;
  isStarredByCurrentUser: boolean;
}

// =============================================================================
// STUDY INVITATIONS
// =============================================================================

export interface StudyInvitation {
  id: string;
  studyId: string;
  studyTitle: string;
  email: string;
  roleId: StudyRoleId;
  roleName: StudyRole;
  status: InvitationStatus;
  token: string;
  invitedById: string;
  invitedByName: string;
  message?: string;
  expiresAt: string;
  respondedAt?: string;
  createdAt: string;
}

// =============================================================================
// REQUEST INPUTS
// =============================================================================

export interface CreateStudyInput {
  title: string;
  description?: string;
  researchFieldId: number;
  institution?: string;
  principalInvestigator?: string;
  tags?: string[];
}

export interface UpdateStudyInput {
  title?: string;
  description?: string;
  researchFieldId?: number;
  institution?: string;
  principalInvestigator?: string;
  tags?: string[];
}

export interface UpdateStudySettingsInput {
  allowPublicComments?: boolean;
  allowDataDownload?: boolean;
  requireApprovalToJoin?: boolean;
}

export interface AddStudyMemberInput {
  userId: string;
  roleId: StudyRoleId;
}

export interface ChangeMemberRoleInput {
  newRoleId: StudyRoleId;
}

export interface AddStudyPaperInput {
  title: string;
  authors?: string;
  doi?: string;
  abstract?: string;
  journal?: string;
  publicationYear?: number;
}

export interface SendInvitationInput {
  email: string;
  roleId: StudyRoleId;
  message?: string;
}

// =============================================================================
// FILTERS & QUERIES
// =============================================================================

export interface StudyFilters {
  status?: StudyStatus;
  researchFieldId?: number;
  search?: string;
  isFeatured?: boolean;
  tags?: string[];
}

// Legacy compatibility (deprecated - use new types)
export type StudyVisibility = "private" | "team" | "public";
export type StudyMemberRole = StudyRole;

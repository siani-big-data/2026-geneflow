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
// CURRENT USER PERMISSIONS
// =============================================================================

/** Current user's permissions within a study */
export interface CurrentUserPermissions {
  /** Whether the current user is a member of the study */
  isMember: boolean;
  /** The user's role ID (null if not a member) */
  roleId: StudyRoleId | null;
  /** The user's role name (null if not a member) */
  roleName: StudyRole | null;
  /** Can manage members (invite, remove, change roles) */
  canManageMembers: boolean;
  /** Can edit study metadata (title, description, etc.) */
  canEditStudy: boolean;
  /** Can edit study content (traces, annotations, etc.) */
  canEditContent: boolean;
  /** Can change study status (draft, active, published, etc.) */
  canChangeStatus: boolean;
  /** Can delete the study */
  canDeleteStudy: boolean;
  /** Can transfer ownership to another member */
  canTransferOwnership: boolean;
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
  readmeMarkdown?: string;
  isFeatured: boolean;
  settings: StudySettings;
  metrics: StudyMetrics;
  tags: string[];
  members: StudyMember[];
  papers: StudyPaper[];
  currentUserPermissions: CurrentUserPermissions;
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
  /**
   * Optional owner selector (Phase 7 Orgs). When omitted the backend defaults
   * to the requesting user. When provided, the study is created under the
   * given owner.
   */
  ownerType?: "User" | "Org";
  ownerHandle?: string;
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

export interface UpdateReadmeInput {
  markdown: string | null;
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

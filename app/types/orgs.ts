/**
 * Organizations — frontend types mirroring backend DTOs (Phase 7).
 */

export type OrgRole = "Owner" | "Admin" | "Member";

export type OrgVisibility = "Public" | "Private";

/**
 * Org invitation lifecycle states (Pascal case to mirror the backend enum).
 * Imported from `@/types/orgs` to avoid colliding with the legacy lowercase
 * `InvitationStatus` exported from `study.ts` via the `@/types` barrel.
 */
export type OrgInvitationStatus =
  | "Pending"
  | "Accepted"
  | "Declined"
  | "Expired"
  | "Revoked";

/** Discriminator for an entity that can own a study. */
export type StudyOwnerType = "User" | "Org";

/** Lightweight reference to a study owner (user or org). */
export interface OwnerRef {
  type: StudyOwnerType;
  handle: string;
  displayName: string;
  avatarUrl?: string;
}

/** Full org response from the API. */
export interface Org {
  id: string;
  handle: string;
  name: string;
  description?: string;
  avatarUrl?: string;
  websiteUrl?: string;
  location?: string;
  visibility: OrgVisibility;
  memberCount: number;
  /** Current viewer's role within this org, if any. */
  myRole?: OrgRole;
  createdAt: string;
}

export interface OrgMember {
  userId: string;
  userName?: string | null;
  avatarUrl?: string | null;
  role: OrgRole;
  joinedAt: string;
}

export interface OrgInvitation {
  token: string;
  orgHandle: string;
  orgName: string;
  invitedEmail: string;
  role: OrgRole;
  expiresAt: string;
  status: OrgInvitationStatus;
}

// =============================================================================
// REQUEST INPUTS
// =============================================================================

export interface CreateOrgInput {
  handle: string;
  name: string;
  description?: string;
}

export interface InviteOrgMemberInput {
  email: string;
  role: OrgRole;
}

export interface CreateOrgInvitationResponse {
  token: string;
  expiresAt: string;
}

export interface ChangeOrgMemberRoleInput {
  role: OrgRole;
}

export interface TransferStudyOwnershipInput {
  toOwnerType: StudyOwnerType;
  toOwnerHandle: string;
}

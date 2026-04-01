import type { User } from "./user";

export interface Study {
  id: string;
  name: string;
  description?: string;
  status: StudyStatus;
  visibility: StudyVisibility;
  owner: User;
  members: StudyMember[];
  tracesCount: number;
  createdAt: string;
  updatedAt: string;
}

export type StudyStatus = "draft" | "active" | "completed" | "archived";

export type StudyVisibility = "private" | "team" | "public";

export interface StudyMember {
  user: User;
  role: StudyMemberRole;
  joinedAt: string;
}

export type StudyMemberRole = "owner" | "editor" | "viewer";

export interface StudyInvitation {
  id: string;
  studyId: string;
  email: string;
  role: StudyMemberRole;
  status: InvitationStatus;
  invitedBy: User;
  createdAt: string;
  expiresAt: string;
}

export type InvitationStatus = "pending" | "accepted" | "declined" | "expired";

export interface CreateStudyInput {
  name: string;
  description?: string;
  visibility: StudyVisibility;
}

export interface UpdateStudyInput {
  name?: string;
  description?: string;
  status?: StudyStatus;
  visibility?: StudyVisibility;
}

export interface StudyFilters {
  status?: StudyStatus;
  visibility?: StudyVisibility;
  search?: string;
}

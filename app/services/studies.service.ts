/**
 * Studies Service - Real API Integration
 *
 * Connects to GeneFlow.ApiNet2 backend API for study management.
 */

import { api } from "@/lib/api-client";
import type {
  Study,
  StudySummary,
  StudyStats,
  StudyMember,
  StudyPaper,
  StudyInvitation,
  StudyFilters,
  CreateStudyInput,
  UpdateStudyInput,
  UpdateStudySettingsInput,
  AddStudyMemberInput,
  ChangeMemberRoleInput,
  AddStudyPaperInput,
  SendInvitationInput,
  ResearchField,
} from "@/types";

// =============================================================================
// API RESPONSE TYPES
// =============================================================================

interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// =============================================================================
// STUDY CRUD OPERATIONS
// =============================================================================

export const studiesService = {
  /**
   * Create a new study.
   */
  async create(input: CreateStudyInput): Promise<Study> {
    return api.post<Study>("/api/v1/studies", input);
  },

  /**
   * Get a study by ID.
   */
  async getById(studyId: string): Promise<Study> {
    return api.get<Study>(`/api/v1/studies/${studyId}`);
  },

  /**
   * Update a study.
   */
  async update(studyId: string, input: UpdateStudyInput): Promise<Study> {
    return api.put<Study>(`/api/v1/studies/${studyId}`, input);
  },

  /**
   * Delete a study (soft delete).
   */
  async delete(studyId: string): Promise<void> {
    return api.delete<void>(`/api/v1/studies/${studyId}`);
  },

  /**
   * Change study status.
   */
  async changeStatus(studyId: string, statusId: number): Promise<Study> {
    return api.patch<Study>(`/api/v1/studies/${studyId}/status`, { statusId });
  },

  /**
   * Update study settings.
   */
  async updateSettings(
    studyId: string,
    settings: UpdateStudySettingsInput
  ): Promise<Study> {
    return api.patch<Study>(`/api/v1/studies/${studyId}/settings`, settings);
  },

  // ===========================================================================
  // STUDY QUERIES
  // ===========================================================================

  /**
   * Get current user's studies (paginated).
   */
  async getMine(
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedResponse<StudySummary>> {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });
    return api.get<PagedResponse<StudySummary>>(`/api/v1/studies/mine?${params}`);
  },

  /**
   * Get public studies with filters (paginated).
   */
  async getPublic(
    filters?: StudyFilters,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedResponse<StudySummary>> {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });

    if (filters?.status) params.append("status", filters.status);
    if (filters?.researchFieldId)
      params.append("researchFieldId", filters.researchFieldId.toString());
    if (filters?.search) params.append("search", filters.search);
    if (filters?.isFeatured !== undefined)
      params.append("isFeatured", filters.isFeatured.toString());

    return api.get<PagedResponse<StudySummary>>(`/api/v1/studies/public?${params}`);
  },

  /**
   * Get featured studies.
   */
  async getFeatured(
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedResponse<StudySummary>> {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });
    return api.get<PagedResponse<StudySummary>>(`/api/v1/studies/featured?${params}`);
  },

  /**
   * Get available research fields.
   */
  async getResearchFields(): Promise<ResearchField[]> {
    return api.get<ResearchField[]>("/api/v1/studies/research-fields");
  },

  // ===========================================================================
  // STUDY MEMBERS
  // ===========================================================================

  /**
   * Get study members.
   */
  async getMembers(studyId: string): Promise<StudyMember[]> {
    return api.get<StudyMember[]>(`/api/v1/studies/${studyId}/members`);
  },

  /**
   * Add a member to a study.
   */
  async addMember(
    studyId: string,
    input: AddStudyMemberInput
  ): Promise<StudyMember> {
    return api.post<StudyMember>(`/api/v1/studies/${studyId}/members`, input);
  },

  /**
   * Remove a member from a study.
   */
  async removeMember(studyId: string, userId: string): Promise<void> {
    return api.delete<void>(`/api/v1/studies/${studyId}/members/${userId}`);
  },

  /**
   * Change a member's role.
   */
  async changeMemberRole(
    studyId: string,
    userId: string,
    input: ChangeMemberRoleInput
  ): Promise<StudyMember> {
    return api.patch<StudyMember>(
      `/api/v1/studies/${studyId}/members/${userId}/role`,
      input
    );
  },

  /**
   * Leave a study.
   */
  async leaveStudy(studyId: string): Promise<void> {
    return api.post<void>(`/api/v1/studies/${studyId}/members/leave`);
  },

  /**
   * Transfer study ownership.
   */
  async transferOwnership(
    studyId: string,
    newOwnerId: string
  ): Promise<Study> {
    return api.post<Study>(`/api/v1/studies/${studyId}/members/transfer-ownership`, {
      newOwnerId,
    });
  },

  // ===========================================================================
  // STUDY PAPERS
  // ===========================================================================

  /**
   * Get study papers.
   */
  async getPapers(studyId: string): Promise<StudyPaper[]> {
    return api.get<StudyPaper[]>(`/api/v1/studies/${studyId}/papers`);
  },

  /**
   * Add a paper to a study.
   */
  async addPaper(
    studyId: string,
    input: AddStudyPaperInput
  ): Promise<StudyPaper> {
    return api.post<StudyPaper>(`/api/v1/studies/${studyId}/papers`, input);
  },

  /**
   * Remove a paper from a study.
   */
  async removePaper(studyId: string, paperId: string): Promise<void> {
    return api.delete<void>(`/api/v1/studies/${studyId}/papers/${paperId}`);
  },

  // ===========================================================================
  // STUDY STARS & VIEWS
  // ===========================================================================

  /**
   * Get study statistics (views, stars).
   */
  async getStats(studyId: string): Promise<StudyStats> {
    return api.get<StudyStats>(`/api/v1/studies/${studyId}/stats`);
  },

  /**
   * Check if the current user has starred a study.
   */
  async isStarred(studyId: string): Promise<{ isStarred: boolean }> {
    return api.get<{ isStarred: boolean }>(`/api/v1/studies/${studyId}/stars`);
  },

  /**
   * Star a study.
   */
  async star(studyId: string): Promise<void> {
    return api.post<void>(`/api/v1/studies/${studyId}/stars`);
  },

  /**
   * Unstar a study.
   */
  async unstar(studyId: string): Promise<void> {
    return api.delete<void>(`/api/v1/studies/${studyId}/stars`);
  },

  /**
   * Record a study view (automatically called when viewing a study).
   */
  async recordView(studyId: string): Promise<void> {
    return api.post<void>(`/api/v1/studies/${studyId}/views`);
  },

  // ===========================================================================
  // STUDY INVITATIONS
  // ===========================================================================

  /**
   * Send an invitation to join a study.
   */
  async sendInvitation(
    studyId: string,
    input: SendInvitationInput
  ): Promise<StudyInvitation> {
    return api.post<StudyInvitation>(
      `/api/v1/studies/${studyId}/invitations`,
      input
    );
  },

  /**
   * Get invitations for a study.
   */
  async getStudyInvitations(
    studyId: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedResponse<StudyInvitation>> {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });
    return api.get<PagedResponse<StudyInvitation>>(
      `/api/v1/studies/${studyId}/invitations?${params}`
    );
  },

  /**
   * Cancel a pending invitation.
   */
  async cancelInvitation(studyId: string, invitationId: string): Promise<void> {
    return api.delete<void>(
      `/api/v1/studies/${studyId}/invitations/${invitationId}`
    );
  },

  /**
   * Resend an invitation.
   */
  async resendInvitation(
    studyId: string,
    invitationId: string
  ): Promise<StudyInvitation> {
    return api.post<StudyInvitation>(
      `/api/v1/studies/${studyId}/invitations/${invitationId}/resend`
    );
  },

  /**
   * Get current user's pending invitations.
   */
  async getMyInvitations(
    email: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedResponse<StudyInvitation>> {
    const params = new URLSearchParams({
      email,
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });
    return api.get<PagedResponse<StudyInvitation>>(`/api/v1/invitations?${params}`);
  },

  /**
   * Get an invitation by token.
   */
  async getInvitationByToken(token: string): Promise<StudyInvitation> {
    return api.get<StudyInvitation>(`/api/v1/invitations/${token}`);
  },

  /**
   * Accept an invitation.
   */
  async acceptInvitation(token: string): Promise<Study> {
    return api.post<Study>(`/api/v1/invitations/${token}/accept`);
  },

  /**
   * Decline an invitation.
   */
  async declineInvitation(token: string): Promise<void> {
    return api.post<void>(`/api/v1/invitations/${token}/decline`);
  },

  // ===========================================================================
  // FEATURE STUDY (Admin only)
  // ===========================================================================

  /**
   * Feature or unfeature a study (admin only).
   */
  async setFeatured(studyId: string, isFeatured: boolean): Promise<Study> {
    return api.patch<Study>(`/api/v1/studies/${studyId}/feature`, { isFeatured });
  },
};

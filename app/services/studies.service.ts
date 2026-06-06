/**
 * Studies Service - Real API Integration
 *
 * Connects to GeneFlow.ApiNet2 backend API for study management.
 */

import { api, tokenStorage } from "@/lib/api-client";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5145";

/**
 * Parses an RFC 6266 / RFC 5987 Content-Disposition header to extract the
 * file name. Handles both `filename="..."` and `filename*=UTF-8''...`.
 */
function parseContentDispositionFilename(header: string | null): string | null {
  if (!header) return null;
  const utf8Match = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(header);
  if (utf8Match) {
    try {
      return decodeURIComponent(utf8Match[1].trim().replace(/^"|"$/g, ""));
    } catch {
      // fall through
    }
  }
  const asciiMatch = /filename="?([^";]+)"?/i.exec(header);
  return asciiMatch ? asciiMatch[1].trim() : null;
}
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
  PagedResponse,
} from "@/types";

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
  async changeStatus(studyId: string, newStatusId: number): Promise<Study> {
    return api.patch<Study>(`/api/v1/studies/${studyId}/status`, { newStatusId });
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

  /**
   * Update the study README markdown.
   * Pass `null` to clear it.
   */
  async updateReadme(studyId: string, markdown: string | null): Promise<Study> {
    return api.put<Study>(`/api/v1/studies/${studyId}/readme`, { markdown });
  },

  /**
   * Duplicate a study.
   * Creates a copy with the current user as owner.
   */
  async duplicate(studyId: string): Promise<Study> {
    return api.post<Study>(`/api/v1/studies/${studyId}/duplicate`, {});
  },

  /**
   * Export a study as a ZIP archive (metadata + traces + papers + members).
   * Returns the binary blob plus the filename advertised by the server via
   * Content-Disposition. Falls back to a generated name when absent.
   */
  async exportStudy(
    studyId: string,
  ): Promise<{ blob: Blob; filename: string }> {
    const accessToken = tokenStorage.getAccessToken();
    const response = await fetch(
      `${API_BASE_URL}/api/v1/studies/${studyId}/export`,
      {
        method: "GET",
        credentials: "include",
        headers: accessToken
          ? { Authorization: `Bearer ${accessToken}` }
          : undefined,
      },
    );

    if (!response.ok) {
      throw new Error(
        `Failed to export study (${response.status} ${response.statusText})`,
      );
    }

    const blob = await response.blob();
    const filename =
      parseContentDispositionFilename(
        response.headers.get("content-disposition"),
      ) ?? `study-${studyId}.zip`;

    return { blob, filename };
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
  async getFeatured(limit: number = 10): Promise<StudySummary[]> {
    const params = new URLSearchParams({
      limit: limit.toString(),
    });
    return api.get<StudySummary[]>(`/api/v1/studies/featured?${params}`);
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

"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { studiesService } from "@/services";
import type {
  StudyFilters,
  CreateStudyInput,
  UpdateStudyInput,
  UpdateStudySettingsInput,
  AddStudyMemberInput,
  ChangeMemberRoleInput,
  AddStudyPaperInput,
  SendInvitationInput,
} from "@/types";

// =============================================================================
// STUDY QUERIES
// =============================================================================

/**
 * Fetch current user's studies (paginated).
 */
export function useMyStudies(page = 1, pageSize = 10) {
  return useQuery({
    queryKey: ["studies", "mine", page, pageSize],
    queryFn: () => studiesService.getMine(page, pageSize),
  });
}

/**
 * Fetch public studies with optional filters (paginated).
 */
export function usePublicStudies(
  filters?: StudyFilters,
  page = 1,
  pageSize = 10
) {
  return useQuery({
    queryKey: ["studies", "public", filters, page, pageSize],
    queryFn: () => studiesService.getPublic(filters, page, pageSize),
  });
}

/**
 * Fetch featured studies.
 */
export function useFeaturedStudies(page = 1, pageSize = 10) {
  return useQuery({
    queryKey: ["studies", "featured", page, pageSize],
    queryFn: () => studiesService.getFeatured(page, pageSize),
  });
}

/**
 * Fetch a single study by ID.
 */
export function useStudy(studyId: string) {
  return useQuery({
    queryKey: ["study", studyId],
    queryFn: () => studiesService.getById(studyId),
    enabled: !!studyId,
  });
}

/**
 * Fetch available research fields.
 */
export function useResearchFields() {
  return useQuery({
    queryKey: ["studies", "research-fields"],
    queryFn: () => studiesService.getResearchFields(),
    staleTime: 1000 * 60 * 60, // 1 hour - research fields rarely change
  });
}

// =============================================================================
// STUDY MUTATIONS
// =============================================================================

/**
 * Create a new study.
 */
export function useCreateStudy() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreateStudyInput) => studiesService.create(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["studies"] });
    },
  });
}

/**
 * Update a study.
 */
export function useUpdateStudy(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: UpdateStudyInput) =>
      studiesService.update(studyId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["study", studyId] });
      queryClient.invalidateQueries({ queryKey: ["studies"] });
    },
  });
}

/**
 * Delete a study.
 */
export function useDeleteStudy() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (studyId: string) => studiesService.delete(studyId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["studies"] });
    },
  });
}

/**
 * Change study status.
 */
export function useChangeStudyStatus(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (statusId: number) =>
      studiesService.changeStatus(studyId, statusId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["study", studyId] });
      queryClient.invalidateQueries({ queryKey: ["studies"] });
    },
  });
}

/**
 * Update study settings.
 */
export function useUpdateStudySettings(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (settings: UpdateStudySettingsInput) =>
      studiesService.updateSettings(studyId, settings),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["study", studyId] });
    },
  });
}

// =============================================================================
// STUDY MEMBERS
// =============================================================================

/**
 * Fetch study members.
 */
export function useStudyMembers(studyId: string) {
  return useQuery({
    queryKey: ["study", studyId, "members"],
    queryFn: () => studiesService.getMembers(studyId),
    enabled: !!studyId,
  });
}

/**
 * Add a member to a study.
 */
export function useAddStudyMember(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: AddStudyMemberInput) =>
      studiesService.addMember(studyId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["study", studyId, "members"] });
      queryClient.invalidateQueries({ queryKey: ["study", studyId] });
    },
  });
}

/**
 * Remove a member from a study.
 */
export function useRemoveStudyMember(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: string) => studiesService.removeMember(studyId, userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["study", studyId, "members"] });
      queryClient.invalidateQueries({ queryKey: ["study", studyId] });
    },
  });
}

/**
 * Change a member's role.
 */
export function useChangeMemberRole(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      userId,
      input,
    }: {
      userId: string;
      input: ChangeMemberRoleInput;
    }) => studiesService.changeMemberRole(studyId, userId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["study", studyId, "members"] });
    },
  });
}

/**
 * Leave a study.
 */
export function useLeaveStudy() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (studyId: string) => studiesService.leaveStudy(studyId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["studies"] });
    },
  });
}

/**
 * Transfer study ownership.
 */
export function useTransferOwnership(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (newOwnerId: string) =>
      studiesService.transferOwnership(studyId, newOwnerId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["study", studyId] });
      queryClient.invalidateQueries({ queryKey: ["study", studyId, "members"] });
    },
  });
}

// =============================================================================
// STUDY PAPERS
// =============================================================================

/**
 * Fetch study papers.
 */
export function useStudyPapers(studyId: string) {
  return useQuery({
    queryKey: ["study", studyId, "papers"],
    queryFn: () => studiesService.getPapers(studyId),
    enabled: !!studyId,
  });
}

/**
 * Add a paper to a study.
 */
export function useAddStudyPaper(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: AddStudyPaperInput) =>
      studiesService.addPaper(studyId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["study", studyId, "papers"] });
      queryClient.invalidateQueries({ queryKey: ["study", studyId] });
    },
  });
}

/**
 * Remove a paper from a study.
 */
export function useRemoveStudyPaper(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (paperId: string) =>
      studiesService.removePaper(studyId, paperId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["study", studyId, "papers"] });
      queryClient.invalidateQueries({ queryKey: ["study", studyId] });
    },
  });
}

// =============================================================================
// STUDY STARS & VIEWS
// =============================================================================

/**
 * Fetch study statistics (views, stars).
 */
export function useStudyStats(studyId: string) {
  return useQuery({
    queryKey: ["study", studyId, "stats"],
    queryFn: () => studiesService.getStats(studyId),
    enabled: !!studyId,
  });
}

/**
 * Check if current user has starred a study.
 */
export function useIsStudyStarred(studyId: string) {
  return useQuery({
    queryKey: ["study", studyId, "starred"],
    queryFn: () => studiesService.isStarred(studyId),
    enabled: !!studyId,
  });
}

/**
 * Star a study.
 */
export function useStarStudy() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (studyId: string) => studiesService.star(studyId),
    onSuccess: (_, studyId) => {
      queryClient.invalidateQueries({ queryKey: ["study", studyId, "starred"] });
      queryClient.invalidateQueries({ queryKey: ["study", studyId, "stats"] });
      queryClient.invalidateQueries({ queryKey: ["study", studyId] });
    },
  });
}

/**
 * Unstar a study.
 */
export function useUnstarStudy() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (studyId: string) => studiesService.unstar(studyId),
    onSuccess: (_, studyId) => {
      queryClient.invalidateQueries({ queryKey: ["study", studyId, "starred"] });
      queryClient.invalidateQueries({ queryKey: ["study", studyId, "stats"] });
      queryClient.invalidateQueries({ queryKey: ["study", studyId] });
    },
  });
}

/**
 * Record a study view.
 */
export function useRecordStudyView() {
  return useMutation({
    mutationFn: (studyId: string) => studiesService.recordView(studyId),
  });
}

// =============================================================================
// STUDY INVITATIONS
// =============================================================================

/**
 * Fetch invitations for a study.
 */
export function useStudyInvitations(studyId: string, page = 1, pageSize = 10) {
  return useQuery({
    queryKey: ["study", studyId, "invitations", page, pageSize],
    queryFn: () => studiesService.getStudyInvitations(studyId, page, pageSize),
    enabled: !!studyId,
  });
}

/**
 * Fetch current user's pending invitations.
 */
export function useMyInvitations(email: string, page = 1, pageSize = 10) {
  return useQuery({
    queryKey: ["invitations", "mine", email, page, pageSize],
    queryFn: () => studiesService.getMyInvitations(email, page, pageSize),
    enabled: !!email,
  });
}

/**
 * Get invitation by token.
 */
export function useInvitationByToken(token: string) {
  return useQuery({
    queryKey: ["invitation", token],
    queryFn: () => studiesService.getInvitationByToken(token),
    enabled: !!token,
  });
}

/**
 * Send an invitation to join a study.
 */
export function useSendInvitation(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: SendInvitationInput) =>
      studiesService.sendInvitation(studyId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["study", studyId, "invitations"],
      });
    },
  });
}

/**
 * Cancel an invitation.
 */
export function useCancelInvitation(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (invitationId: string) =>
      studiesService.cancelInvitation(studyId, invitationId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["study", studyId, "invitations"],
      });
    },
  });
}

/**
 * Resend an invitation.
 */
export function useResendInvitation(studyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (invitationId: string) =>
      studiesService.resendInvitation(studyId, invitationId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["study", studyId, "invitations"],
      });
    },
  });
}

/**
 * Accept an invitation.
 */
export function useAcceptInvitation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (token: string) => studiesService.acceptInvitation(token),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["studies"] });
      queryClient.invalidateQueries({ queryKey: ["invitations"] });
    },
  });
}

/**
 * Decline an invitation.
 */
export function useDeclineInvitation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (token: string) => studiesService.declineInvitation(token),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["invitations"] });
    },
  });
}

// =============================================================================
// LEGACY COMPATIBILITY (deprecated - use new hooks above)
// =============================================================================

/**
 * @deprecated Use useMyStudies or usePublicStudies instead
 */
export function useStudies(filters?: StudyFilters, page = 1, limit = 10) {
  return usePublicStudies(filters, page, limit);
}

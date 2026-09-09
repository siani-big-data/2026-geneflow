"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { orgInvitationsService } from "@/services";
import { orgsKeys } from "./use-orgs";
import type {
  CreateOrgInvitationResponse,
  InviteOrgMemberInput,
  OrgInvitation,
} from "@/types";

// ============= QUERY KEYS =============
export const orgInvitationsKeys = {
  all: ["org-invitations"] as const,
  mine: () => [...orgInvitationsKeys.all, "mine"] as const,
};

// ============= QUERIES =============

/** Fetch current user's pending org invitations. */
export function useMyInvitations() {
  return useQuery<OrgInvitation[]>({
    queryKey: orgInvitationsKeys.mine(),
    queryFn: () => orgInvitationsService.listMine(),
  });
}

// ============= MUTATIONS =============

/** Invite a new member to an org by email. */
export function useInviteOrgMember(handle: string) {
  return useMutation<CreateOrgInvitationResponse, Error, InviteOrgMemberInput>({
    mutationFn: (input) => orgInvitationsService.invite(handle, input),
  });
}

/** Accept a pending invitation by token. */
export function useAcceptInvitation() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (token: string) => orgInvitationsService.accept(token),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: orgsKeys.mine() });
      qc.invalidateQueries({ queryKey: orgInvitationsKeys.mine() });
    },
  });
}

/** Decline a pending invitation by token. */
export function useDeclineInvitation() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (token: string) => orgInvitationsService.decline(token),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: orgInvitationsKeys.mine() });
    },
  });
}

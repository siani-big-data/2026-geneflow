/**
 * Organization Invitations Service — Phase 7.
 */

import { api } from "@/lib/api-client";
import type {
  Org,
  OrgInvitation,
  InviteOrgMemberInput,
  CreateOrgInvitationResponse,
} from "@/types";

export const orgInvitationsService = {
  /**
   * Invite an email to join an org with a given role. Returns the invitation
   * token + expiration so the inviter can share the accept link.
   */
  async invite(
    handle: string,
    input: InviteOrgMemberInput,
  ): Promise<CreateOrgInvitationResponse> {
    return api.post<CreateOrgInvitationResponse>(
      `/api/v1/orgs/${handle}/invitations`,
      input,
    );
  },

  /**
   * Accept an invitation by token. Returns the org the user just joined.
   */
  async accept(token: string): Promise<Org> {
    return api.post<Org>(`/api/v1/invitations/${token}/accept`);
  },

  /**
   * Decline an invitation by token.
   */
  async decline(token: string): Promise<void> {
    return api.post<void>(`/api/v1/invitations/${token}/decline`);
  },

  /**
   * List the current user's pending invitations.
   */
  async listMine(): Promise<OrgInvitation[]> {
    return api.get<OrgInvitation[]>("/api/v1/me/invitations");
  },
};

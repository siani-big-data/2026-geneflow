/**
 * Organizations Service — Phase 7.
 *
 * Connects to GeneFlow.ApiNet2 backend API for org management.
 */

import { api } from "@/lib/api-client";
import type {
  Org,
  OrgMember,
  OrgMembership,
  OrgRole,
  CreateOrgInput,
  ChangeOrgMemberRoleInput,
  Study,
  PagedResponse,
} from "@/types";

export const orgsService = {
  /**
   * Create a new organization.
   */
  async create(input: CreateOrgInput): Promise<Org> {
    return api.post<Org>("/api/v1/orgs", input);
  },

  /**
   * Get an organization by its handle.
   */
  async getByHandle(handle: string): Promise<Org> {
    return api.get<Org>(`/api/v1/orgs/${handle}`);
  },

  /**
   * List orgs the current user is a member of.
   */
  async listMine(): Promise<OrgMembership[]> {
    return api.get<OrgMembership[]>("/api/v1/me/orgs");
  },

  /**
   * List members of an org.
   */
  async listMembers(handle: string): Promise<OrgMember[]> {
    return api.get<OrgMember[]>(`/api/v1/orgs/${handle}/members`);
  },

  /**
   * Change a member's role within the org.
   */
  async changeMemberRole(
    handle: string,
    userId: string,
    role: OrgRole,
  ): Promise<void> {
    const body: ChangeOrgMemberRoleInput = { role };
    return api.patch<void>(
      `/api/v1/orgs/${handle}/members/${userId}`,
      body,
    );
  },

  /**
   * Remove a member from the org.
   */
  async removeMember(handle: string, userId: string): Promise<void> {
    return api.delete<void>(`/api/v1/orgs/${handle}/members/${userId}`);
  },

  /**
   * List studies owned by an organisation.
   * Members see every non-deleted study; visitors see Published only.
   */
  async listStudies(
    handle: string,
    pageNumber: number = 1,
    pageSize: number = 20,
  ): Promise<PagedResponse<Study>> {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });
    return api.get<PagedResponse<Study>>(
      `/api/v1/orgs/${handle}/studies?${params}`,
    );
  },
};

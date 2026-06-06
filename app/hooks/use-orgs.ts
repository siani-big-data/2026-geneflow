"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { orgsService } from "@/services";
import { useRouter } from "@/lib/navigation";
import type { CreateOrgInput, Org, OrgMembership, OrgRole } from "@/types";

// ============= QUERY KEYS =============
export const orgsKeys = {
  all: ["orgs"] as const,
  mine: () => [...orgsKeys.all, "mine"] as const,
  detail: (handle: string) => [...orgsKeys.all, "detail", handle] as const,
  members: (handle: string) => [...orgsKeys.all, "members", handle] as const,
  studies: (handle: string, page: number) =>
    [...orgsKeys.all, "studies", handle, page] as const,
};

// ============= QUERIES =============

/** Fetch a single org by handle. */
export function useOrg(handle: string) {
  return useQuery<Org>({
    queryKey: orgsKeys.detail(handle),
    queryFn: () => orgsService.getByHandle(handle),
    enabled: !!handle,
  });
}

/** Fetch the current user's orgs. */
export function useMyOrgs() {
  return useQuery<OrgMembership[]>({
    queryKey: orgsKeys.mine(),
    queryFn: () => orgsService.listMine(),
  });
}

/** Fetch members of an org. */
export function useOrgMembers(handle: string) {
  return useQuery({
    queryKey: orgsKeys.members(handle),
    queryFn: () => orgsService.listMembers(handle),
    enabled: !!handle,
  });
}

/** Fetch studies owned by an org (paged). */
export function useOrgStudies(
  handle: string,
  pageNumber: number = 1,
  pageSize: number = 20,
) {
  return useQuery({
    queryKey: orgsKeys.studies(handle, pageNumber),
    queryFn: () => orgsService.listStudies(handle, pageNumber, pageSize),
    enabled: !!handle,
  });
}

// ============= MUTATIONS =============

/** Create a new org. On success, invalidates `mine` and routes to /orgs/{handle}. */
export function useCreateOrg() {
  const qc = useQueryClient();
  const router = useRouter();

  return useMutation({
    mutationFn: (input: CreateOrgInput) => orgsService.create(input),
    onSuccess: (org) => {
      qc.invalidateQueries({ queryKey: orgsKeys.mine() });
      router.push(`/orgs/${org.handle}` as never);
    },
  });
}

/** Change a member's role within an org. */
export function useChangeOrgMemberRole(handle: string) {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, role }: { userId: string; role: OrgRole }) =>
      orgsService.changeMemberRole(handle, userId, role),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: orgsKeys.members(handle) });
    },
  });
}

/** Remove a member from an org. */
export function useRemoveOrgMember(handle: string) {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (userId: string) => orgsService.removeMember(handle, userId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: orgsKeys.members(handle) });
    },
  });
}

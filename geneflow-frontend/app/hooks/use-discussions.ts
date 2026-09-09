"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { discussionsService } from "@/services/discussions.service";
import type {
  CreateDiscussionInput,
  Discussion,
} from "@/types/discussions";
import type { PagedResponse } from "@/types/common";

// ============= QUERY KEYS =============
export const discussionKeys = {
  all: ["discussions"] as const,
  lists: () => [...discussionKeys.all, "list"] as const,
  list: (studyId: string, page: number, pageSize: number) =>
    [...discussionKeys.lists(), studyId, page, pageSize] as const,
  details: () => [...discussionKeys.all, "detail"] as const,
  detail: (discussionId: string) =>
    [...discussionKeys.details(), discussionId] as const,
};

// ============= QUERIES =============
export function useStudyDiscussions(
  studyId: string,
  page = 1,
  pageSize = 20,
) {
  return useQuery<PagedResponse<Discussion>>({
    queryKey: discussionKeys.list(studyId, page, pageSize),
    queryFn: () => discussionsService.list(studyId, page, pageSize),
    enabled: !!studyId,
  });
}

export function useDiscussion(discussionId: string | null | undefined) {
  return useQuery<Discussion>({
    queryKey: discussionKeys.detail(discussionId ?? ""),
    queryFn: () => discussionsService.get(discussionId!),
    enabled: !!discussionId,
  });
}

// ============= MUTATIONS =============
export function useCreateDiscussion(studyId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateDiscussionInput) =>
      discussionsService.create(studyId, input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: discussionKeys.lists() });
    },
  });
}

export function useLockDiscussion() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      discussionId,
      lock,
    }: {
      discussionId: string;
      lock: boolean;
    }) => discussionsService.lock(discussionId, lock),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: discussionKeys.detail(data.id) });
      qc.invalidateQueries({ queryKey: discussionKeys.lists() });
    },
  });
}

"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { socialService } from "@/services/social.service";
import type { PinnedStudy } from "@/types/social";

export const pinnedKeys = {
  all: ["pinned-studies"] as const,
  mine: () => [...pinnedKeys.all, "mine"] as const,
  byUser: (userId: string) => [...pinnedKeys.all, "user", userId] as const,
};

export function useMyPinnedStudies() {
  return useQuery({
    queryKey: pinnedKeys.mine(),
    queryFn: () => socialService.getMyPinned(),
  });
}

export function useUserPinnedStudies(userId: string | undefined) {
  return useQuery({
    queryKey: pinnedKeys.byUser(userId ?? ""),
    queryFn: () => socialService.getUserPinned(userId!),
    enabled: !!userId,
  });
}

export function useUpdateMyPinnedStudies() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (studyIds: string[]) => socialService.updateMyPinned(studyIds),
    onMutate: async (studyIds) => {
      await queryClient.cancelQueries({ queryKey: pinnedKeys.mine() });
      const previous = queryClient.getQueryData<PinnedStudy[]>(pinnedKeys.mine());

      const optimistic: PinnedStudy[] = studyIds.map((id, idx) => ({
        studyId: id,
        order: idx,
        pinnedAt: new Date().toISOString(),
      }));
      queryClient.setQueryData<PinnedStudy[]>(pinnedKeys.mine(), optimistic);

      return { previous };
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previous) {
        queryClient.setQueryData<PinnedStudy[]>(pinnedKeys.mine(), ctx.previous);
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: pinnedKeys.all });
    },
  });
}

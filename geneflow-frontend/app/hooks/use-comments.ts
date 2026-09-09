"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { discussionsService } from "@/services/discussions.service";
import { discussionKeys } from "./use-discussions";

export function useCreateComment(discussionId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: string) =>
      discussionsService.createComment(discussionId, { body }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: discussionKeys.detail(discussionId) });
    },
  });
}

export function useEditComment(discussionId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      commentId,
      body,
    }: {
      commentId: string;
      body: string;
    }) => discussionsService.editComment(commentId, { body }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: discussionKeys.detail(discussionId) });
    },
  });
}

export function useDeleteComment(discussionId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (commentId: string) =>
      discussionsService.deleteComment(commentId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: discussionKeys.detail(discussionId) });
    },
  });
}

"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { discussionsService } from "@/services/discussions.service";
import { discussionKeys } from "./use-discussions";
import type { Discussion } from "@/types/discussions";

interface ReactionVars {
  commentId: string;
  emoji: string;
}

/**
 * Optimistic toggle: flips `reactedByMe` immediately in the discussion cache,
 * then reconciles with the server response.
 */
function patchReaction(
  draft: Discussion | undefined,
  commentId: string,
  emoji: string,
  add: boolean,
): Discussion | undefined {
  if (!draft?.comments) return draft;
  return {
    ...draft,
    comments: draft.comments.map((c) => {
      if (c.id !== commentId) return c;
      const existing = c.reactions.find((r) => r.emoji === emoji);
      let reactions = c.reactions;
      if (existing) {
        reactions = c.reactions
          .map((r) =>
            r.emoji === emoji
              ? {
                  ...r,
                  count: r.count + (add ? 1 : -1),
                  reactedByMe: add,
                }
              : r,
          )
          .filter((r) => r.count > 0);
      } else if (add) {
        reactions = [...c.reactions, { emoji, count: 1, reactedByMe: true }];
      }
      return { ...c, reactions };
    }),
  };
}

export function useAddReaction(discussionId: string) {
  const qc = useQueryClient();
  const key = discussionKeys.detail(discussionId);

  return useMutation({
    mutationFn: ({ commentId, emoji }: ReactionVars) =>
      discussionsService.addReaction(commentId, emoji),
    onMutate: async ({ commentId, emoji }) => {
      await qc.cancelQueries({ queryKey: key });
      const previous = qc.getQueryData<Discussion>(key);
      qc.setQueryData<Discussion | undefined>(key, (old) =>
        patchReaction(old, commentId, emoji, true),
      );
      return { previous };
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previous) qc.setQueryData(key, ctx.previous);
    },
    onSettled: () => {
      qc.invalidateQueries({ queryKey: key });
    },
  });
}

export function useRemoveReaction(discussionId: string) {
  const qc = useQueryClient();
  const key = discussionKeys.detail(discussionId);

  return useMutation({
    mutationFn: ({ commentId, emoji }: ReactionVars) =>
      discussionsService.removeReaction(commentId, emoji),
    onMutate: async ({ commentId, emoji }) => {
      await qc.cancelQueries({ queryKey: key });
      const previous = qc.getQueryData<Discussion>(key);
      qc.setQueryData<Discussion | undefined>(key, (old) =>
        patchReaction(old, commentId, emoji, false),
      );
      return { previous };
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previous) qc.setQueryData(key, ctx.previous);
    },
    onSettled: () => {
      qc.invalidateQueries({ queryKey: key });
    },
  });
}

"use client";

import { useTranslations } from "next-intl";
import {
  useAddReaction,
  useRemoveReaction,
} from "@/hooks";
import type { ReactionSummary } from "@/types/discussions";

const REACTION_PALETTE = ["👍", "❤️", "🎉", "🚀", "👀", "😄"] as const;

interface ReactionsBarProps {
  discussionId: string;
  commentId: string;
  reactions: ReactionSummary[];
  disabled?: boolean;
}

export function ReactionsBar({
  discussionId,
  commentId,
  reactions,
  disabled,
}: ReactionsBarProps) {
  const t = useTranslations("reactions");
  const add = useAddReaction(discussionId);
  const remove = useRemoveReaction(discussionId);

  const byEmoji = new Map(reactions.map((r) => [r.emoji, r]));

  const toggle = (emoji: string) => {
    const existing = byEmoji.get(emoji);
    if (existing?.reactedByMe) {
      remove.mutate({ commentId, emoji });
    } else {
      add.mutate({ commentId, emoji });
    }
  };

  return (
    <div className="mt-2 flex flex-wrap items-center gap-1">
      {reactions
        .filter((r) => r.count > 0)
        .map((r) => (
          <button
            key={r.emoji}
            type="button"
            onClick={() => toggle(r.emoji)}
            disabled={disabled}
            aria-label={t("toggle", { emoji: r.emoji })}
            className={`inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs transition ${
              r.reactedByMe
                ? "border-primary bg-primary/10 text-primary"
                : "border-border bg-muted/40 hover:bg-muted"
            }`}
          >
            <span aria-hidden>{r.emoji}</span>
            <span className="tabular-nums">{r.count}</span>
          </button>
        ))}

      {!disabled && (
        <div className="relative inline-flex">
          <details className="group">
            <summary
              className="inline-flex cursor-pointer items-center rounded-full border border-dashed border-border px-2 py-0.5 text-xs text-muted-foreground hover:text-foreground"
              aria-label={t("addReaction")}
            >
              +
            </summary>
            <div className="absolute left-0 top-full z-10 mt-1 flex gap-1 rounded-md border bg-popover p-1 shadow-md">
              {REACTION_PALETTE.map((emoji) => (
                <button
                  key={emoji}
                  type="button"
                  onClick={() => toggle(emoji)}
                  className="rounded p-1 text-base hover:bg-muted"
                  aria-label={t("toggle", { emoji })}
                >
                  {emoji}
                </button>
              ))}
            </div>
          </details>
        </div>
      )}
    </div>
  );
}

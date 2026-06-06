"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Pencil, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  useCreateComment,
  useEditComment,
  useDeleteComment,
} from "@/hooks";
import { useAuthStore, selectUser } from "@/stores/auth-store";
import type { Comment } from "@/types/discussions";
import { CommentForm } from "./comment-form";
import { ReactionsBar } from "./reactions-bar";

interface CommentThreadProps {
  discussionId: string;
  comments: Comment[];
  locked: boolean;
}

export function CommentThread({
  discussionId,
  comments,
  locked,
}: CommentThreadProps) {
  const t = useTranslations("comments");
  const user = useAuthStore(selectUser);
  const userId = user?.id;

  const createComment = useCreateComment(discussionId);
  const editComment = useEditComment(discussionId);
  const deleteComment = useDeleteComment(discussionId);

  const [editingId, setEditingId] = useState<string | null>(null);

  return (
    <div className="flex flex-col gap-4">
      <ul className="flex flex-col gap-3">
        {comments.length === 0 && (
          <li className="rounded-md border border-dashed p-6 text-center text-sm text-muted-foreground">
            {t("empty")}
          </li>
        )}
        {comments.map((c) => {
          const isOwner = !!userId && c.authorId === userId;
          const isEditing = editingId === c.id;
          return (
            <li
              key={c.id}
              className="rounded-md border bg-card p-3 text-sm shadow-sm"
            >
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 text-xs text-muted-foreground">
                    <span className="font-medium text-foreground">
                      {c.authorId}
                    </span>
                    <span>{new Date(c.createdAt).toLocaleString()}</span>
                    {c.editedAt && (
                      <span className="italic">{t("edited")}</span>
                    )}
                  </div>

                  {isEditing ? (
                    <div className="mt-2">
                      <CommentForm
                        initialValue={c.bodyMarkdown}
                        submitting={editComment.isPending}
                        submitLabel={t("form.save")}
                        autoFocus
                        onSubmit={async (body) => {
                          await editComment.mutateAsync({
                            commentId: c.id,
                            body,
                          });
                          setEditingId(null);
                        }}
                        onCancel={() => setEditingId(null)}
                      />
                    </div>
                  ) : c.isDeleted ? (
                    <p className="mt-1 italic text-muted-foreground">
                      {t("deleted")}
                    </p>
                  ) : (
                    <p className="mt-1 whitespace-pre-wrap break-words text-foreground">
                      {c.bodyMarkdown}
                    </p>
                  )}

                  {!c.isDeleted && !isEditing && (
                    <ReactionsBar
                      discussionId={discussionId}
                      commentId={c.id}
                      reactions={c.reactions}
                      disabled={locked}
                    />
                  )}
                </div>

                {isOwner && !c.isDeleted && !isEditing && !locked && (
                  <div className="flex shrink-0 items-center gap-1">
                    <Button
                      type="button"
                      size="icon"
                      variant="ghost"
                      onClick={() => setEditingId(c.id)}
                      aria-label={t("actions.edit")}
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </Button>
                    <Button
                      type="button"
                      size="icon"
                      variant="ghost"
                      onClick={() => {
                        if (confirm(t("actions.confirmDelete"))) {
                          deleteComment.mutate(c.id);
                        }
                      }}
                      aria-label={t("actions.delete")}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                )}
              </div>
            </li>
          );
        })}
      </ul>

      {!locked && (
        <div className="rounded-md border bg-muted/30 p-3">
          <CommentForm
            submitting={createComment.isPending}
            onSubmit={async (body) => {
              await createComment.mutateAsync(body);
            }}
            submitLabel={t("form.post")}
          />
        </div>
      )}
      {locked && (
        <p className="rounded-md border border-dashed p-3 text-center text-xs text-muted-foreground">
          {t("locked")}
        </p>
      )}
    </div>
  );
}

"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Loader2 } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useCreateDiscussion } from "@/hooks";

interface NewDiscussionDialogProps {
  studyId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated?: (discussionId: string) => void;
}

export function NewDiscussionDialog({
  studyId,
  open,
  onOpenChange,
  onCreated,
}: NewDiscussionDialogProps) {
  const t = useTranslations("discussions");
  const create = useCreateDiscussion(studyId);

  const [title, setTitle] = useState("");
  const [category, setCategory] = useState("");
  const [error, setError] = useState<string | null>(null);

  const reset = () => {
    setTitle("");
    setCategory("");
    setError(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = title.trim();
    if (!trimmed) {
      setError(t("validation.titleRequired"));
      return;
    }
    try {
      const created = await create.mutateAsync({
        title: trimmed,
        category: category.trim() || null,
      });
      reset();
      onOpenChange(false);
      onCreated?.(created.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("validation.unknown"));
    }
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) reset();
        onOpenChange(o);
      }}
    >
      <DialogContent className="sm:max-w-[480px]">
        <DialogHeader>
          <DialogTitle>{t("new.title")}</DialogTitle>
          <DialogDescription>{t("new.description")}</DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2">
          <div className="space-y-1">
            <label className="text-xs font-medium" htmlFor="discussion-title">
              {t("new.titleLabel")}
            </label>
            <Input
              id="discussion-title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              maxLength={200}
              placeholder={t("new.titlePlaceholder")}
              autoFocus
            />
          </div>

          <div className="space-y-1">
            <label
              className="text-xs font-medium"
              htmlFor="discussion-category"
            >
              {t("new.categoryLabel")}
            </label>
            <Input
              id="discussion-category"
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              maxLength={50}
              placeholder={t("new.categoryPlaceholder")}
            />
          </div>

          {error && <p className="text-xs text-destructive">{error}</p>}

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onOpenChange(false)}
              disabled={create.isPending}
            >
              {t("new.cancel")}
            </Button>
            <Button type="submit" size="sm" disabled={create.isPending}>
              {create.isPending && (
                <Loader2 className="mr-1.5 h-3 w-3 animate-spin" aria-hidden />
              )}
              {t("new.submit")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

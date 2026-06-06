"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { BookText, Pencil, Save, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Markdown } from "@/components/ui/markdown";
import { MarkdownEditor } from "@/components/ui/markdown-editor";
import { useUpdateReadme } from "@/hooks/use-studies";

const MAX_README_LENGTH = 100_000;

export interface ReadmeSectionProps {
  studyId: string;
  readmeMarkdown: string | null | undefined;
  canEdit: boolean;
}

/**
 * Renders the study README. Members with edit permission can switch to an
 * inline editor with live preview.
 */
export function ReadmeSection({
  studyId,
  readmeMarkdown,
  canEdit,
}: ReadmeSectionProps) {
  const t = useTranslations("organization.readme");
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(readmeMarkdown ?? "");
  const updateReadme = useUpdateReadme(studyId);

  // Keep the local draft in sync when the upstream README changes (e.g. when
  // another tab updates it and the query cache refreshes).
  useEffect(() => {
    if (!editing) {
      setDraft(readmeMarkdown ?? "");
    }
  }, [readmeMarkdown, editing]);

  const hasContent = !!readmeMarkdown && readmeMarkdown.trim().length > 0;
  const dirty = draft !== (readmeMarkdown ?? "");

  const handleSave = async () => {
    const next = draft.trim().length === 0 ? null : draft;
    await updateReadme.mutateAsync(next);
    setEditing(false);
  };

  const handleCancel = () => {
    setDraft(readmeMarkdown ?? "");
    setEditing(false);
  };

  return (
    <div className="rounded-xl border border-border bg-card p-6">
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="flex items-center gap-2 text-lg font-semibold text-foreground">
          <BookText className="h-5 w-5 text-muted-foreground" />
          {t("title")}
        </h2>
        {canEdit && !editing && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => setEditing(true)}
          >
            <Pencil className="mr-1.5 h-3.5 w-3.5" />
            {hasContent ? t("edit") : t("add")}
          </Button>
        )}
      </div>

      {editing ? (
        <>
          <MarkdownEditor
            value={draft}
            onChange={setDraft}
            placeholder={t("placeholder")}
            maxLength={MAX_README_LENGTH}
            editLabel={t("editTab")}
            previewLabel={t("previewTab")}
            emptyPreviewLabel={t("emptyPreview")}
          />
          {updateReadme.isError && (
            <p className="mt-2 text-sm text-destructive">
              {updateReadme.error instanceof Error
                ? updateReadme.error.message
                : t("saveError")}
            </p>
          )}
          <div className="mt-3 flex items-center justify-end gap-2">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={handleCancel}
              disabled={updateReadme.isPending}
            >
              <X className="mr-1.5 h-3.5 w-3.5" />
              {t("cancel")}
            </Button>
            <Button
              type="button"
              size="sm"
              onClick={handleSave}
              disabled={updateReadme.isPending || !dirty}
            >
              <Save className="mr-1.5 h-3.5 w-3.5" />
              {updateReadme.isPending ? t("saving") : t("save")}
            </Button>
          </div>
        </>
      ) : hasContent ? (
        <Markdown source={readmeMarkdown!} />
      ) : (
        <p className="text-sm italic text-muted-foreground">{t("empty")}</p>
      )}
    </div>
  );
}

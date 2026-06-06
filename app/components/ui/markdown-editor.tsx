"use client";

import { useState } from "react";
import { Eye, Pencil } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Markdown } from "@/components/ui/markdown";
import { cn } from "@/lib/utils";

export interface MarkdownEditorProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  rows?: number;
  maxLength?: number;
  className?: string;
  /** Translated label for the "Edit" tab. */
  editLabel?: string;
  /** Translated label for the "Preview" tab. */
  previewLabel?: string;
  /** Translated empty-state label shown in preview when value is blank. */
  emptyPreviewLabel?: string;
}

/**
 * Two-tab markdown editor: a plain Textarea for editing and a Markdown render
 * for preview. Keeps things lightweight (no monaco) and consistent with the
 * platform's design tokens.
 */
export function MarkdownEditor({
  value,
  onChange,
  placeholder,
  rows = 16,
  maxLength,
  className,
  editLabel = "Edit",
  previewLabel = "Preview",
  emptyPreviewLabel = "Nothing to preview.",
}: MarkdownEditorProps) {
  const [mode, setMode] = useState<"edit" | "preview">("edit");

  return (
    <div className={cn("flex flex-col gap-3", className)}>
      <div className="flex items-center gap-2 border-b border-border pb-2">
        <Button
          type="button"
          variant={mode === "edit" ? "secondary" : "ghost"}
          size="sm"
          onClick={() => setMode("edit")}
        >
          <Pencil className="mr-1.5 h-3.5 w-3.5" />
          {editLabel}
        </Button>
        <Button
          type="button"
          variant={mode === "preview" ? "secondary" : "ghost"}
          size="sm"
          onClick={() => setMode("preview")}
        >
          <Eye className="mr-1.5 h-3.5 w-3.5" />
          {previewLabel}
        </Button>
        {maxLength && (
          <span className="ml-auto text-xs text-muted-foreground">
            {value.length.toLocaleString()} / {maxLength.toLocaleString()}
          </span>
        )}
      </div>

      {mode === "edit" ? (
        <Textarea
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          rows={rows}
          maxLength={maxLength}
          className="font-mono text-sm"
        />
      ) : (
        <div className="min-h-[200px] rounded-md border border-border bg-background p-4">
          {value.trim().length === 0 ? (
            <p className="text-sm italic text-muted-foreground">
              {emptyPreviewLabel}
            </p>
          ) : (
            <Markdown source={value} />
          )}
        </div>
      )}
    </div>
  );
}

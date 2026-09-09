"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";

interface CommentFormProps {
  initialValue?: string;
  submitting?: boolean;
  onSubmit: (body: string) => void | Promise<void>;
  onCancel?: () => void;
  submitLabel?: string;
  placeholder?: string;
  autoFocus?: boolean;
}

export function CommentForm({
  initialValue = "",
  submitting = false,
  onSubmit,
  onCancel,
  submitLabel,
  placeholder,
  autoFocus,
}: CommentFormProps) {
  const t = useTranslations("comments");
  const [body, setBody] = useState(initialValue);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = body.trim();
    if (!trimmed) {
      setError(t("validation.bodyRequired"));
      return;
    }
    setError(null);
    await onSubmit(trimmed);
    setBody("");
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-2">
      <Textarea
        value={body}
        onChange={(e) => setBody(e.target.value)}
        placeholder={placeholder ?? t("form.placeholder")}
        rows={3}
        autoFocus={autoFocus}
        disabled={submitting}
      />
      {error && <p className="text-xs text-destructive">{error}</p>}
      <div className="flex justify-end gap-2">
        {onCancel && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onCancel}
            disabled={submitting}
          >
            {t("form.cancel")}
          </Button>
        )}
        <Button type="submit" size="sm" disabled={submitting}>
          {submitting && (
            <Loader2 className="mr-1.5 h-3 w-3 animate-spin" aria-hidden />
          )}
          {submitLabel ?? t("form.submit")}
        </Button>
      </div>
    </form>
  );
}

"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
import { Loader2 } from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Textarea,
} from "@/components/ui";
import { useCreateOrg } from "@/hooks/use-orgs";
import { ApiClientError } from "@/lib/api-client";

const HANDLE_RE = /^[a-z0-9-]{2,39}$/;

export interface CreateOrgDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/**
 * Modal dialog for creating a new organization. Live-validates the handle
 * against /^[a-z0-9-]{2,39}$/ and surfaces server-side "handle taken"
 * errors inline.
 */
export function CreateOrgDialog({ open, onOpenChange }: CreateOrgDialogProps) {
  const t = useTranslations("orgs.create");
  const tCommon = useTranslations("common");
  const createOrg = useCreateOrg();

  const [handle, setHandle] = React.useState("");
  const [name, setName] = React.useState("");
  const [description, setDescription] = React.useState("");
  const [serverError, setServerError] = React.useState<string | null>(null);

  const handleValid = HANDLE_RE.test(handle);
  const showHandleError = handle.length > 0 && !handleValid;
  const canSubmit = handleValid && name.trim().length > 0 && !createOrg.isPending;

  const reset = React.useCallback(() => {
    setHandle("");
    setName("");
    setDescription("");
    setServerError(null);
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    setServerError(null);
    try {
      await createOrg.mutateAsync({
        handle: handle.trim(),
        name: name.trim(),
        description: description.trim() || undefined,
      });
      reset();
      onOpenChange(false);
    } catch (err) {
      if (err instanceof ApiClientError) {
        if (err.status === 409 || err.code === "HANDLE_TAKEN") {
          setServerError(t("errors.handleTaken"));
          return;
        }
        setServerError(err.message);
        return;
      }
      setServerError((err as Error).message);
    }
  };

  const handleClose = (next: boolean) => {
    if (!next) reset();
    onOpenChange(next);
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-[480px]">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>{t("title")}</DialogTitle>
            <DialogDescription>{t("nameHelp")}</DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-1.5">
              <label
                htmlFor="org-handle"
                className="text-sm font-medium text-foreground"
              >
                {t("handle")}
              </label>
              <Input
                id="org-handle"
                value={handle}
                onChange={(e) => setHandle(e.target.value.toLowerCase())}
                placeholder="acme-bio"
                autoComplete="off"
                aria-invalid={showHandleError}
              />
              <p
                className={`text-xs ${showHandleError ? "text-red-500" : "text-muted-foreground"}`}
              >
                {showHandleError ? t("errors.handleInvalid") : t("handleHelp")}
              </p>
            </div>

            <div className="space-y-1.5">
              <label
                htmlFor="org-name"
                className="text-sm font-medium text-foreground"
              >
                {t("name")}
              </label>
              <Input
                id="org-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Acme Biotech"
                autoComplete="off"
              />
            </div>

            <div className="space-y-1.5">
              <label
                htmlFor="org-description"
                className="text-sm font-medium text-foreground"
              >
                {t("description")}
              </label>
              <Textarea
                id="org-description"
                rows={3}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>

            {serverError && (
              <p className="rounded-md bg-red-500/10 px-3 py-2 text-sm text-red-500">
                {serverError}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={() => handleClose(false)}
              disabled={createOrg.isPending}
            >
              {t("cancel") || tCommon("cancel")}
            </Button>
            <Button type="submit" disabled={!canSubmit}>
              {createOrg.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                t("submit")
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

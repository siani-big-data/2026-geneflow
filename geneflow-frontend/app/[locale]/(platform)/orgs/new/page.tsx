"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
import { Loader2 } from "lucide-react";
import { Button, Card, CardContent, CardHeader, CardTitle, Input, Textarea } from "@/components/ui";
import { useCreateOrg } from "@/hooks/use-orgs";
import { ApiClientError } from "@/lib/api-client";

const HANDLE_RE = /^[a-z0-9-]{2,39}$/;

/**
 * Inline Create-Organization form. On success, the `useCreateOrg` mutation
 * routes us to `/orgs/{handle}` automatically.
 */
export default function NewOrgPage() {
  const t = useTranslations("orgs.create");
  const tCommon = useTranslations("common");
  const createOrg = useCreateOrg();

  const [handle, setHandle] = React.useState("");
  const [name, setName] = React.useState("");
  const [description, setDescription] = React.useState("");
  const [serverError, setServerError] = React.useState<string | null>(null);

  const handleValid = HANDLE_RE.test(handle);
  const showHandleError = handle.length > 0 && !handleValid;
  const canSubmit =
    handleValid && name.trim().length > 0 && !createOrg.isPending;

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

  return (
    <div className="mx-auto max-w-2xl">
      <Card>
        <CardHeader>
          <CardTitle>{t("title")}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-5">
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
                {showHandleError
                  ? t("errors.handleInvalid")
                  : t("handleHelp")}
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
              <p className="text-xs text-muted-foreground">{t("nameHelp")}</p>
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

            <div className="flex justify-end gap-2 pt-2">
              <Button type="submit" disabled={!canSubmit}>
                {createOrg.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  t("submit") || tCommon("create")
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

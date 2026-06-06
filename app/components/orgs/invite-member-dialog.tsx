"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
import { Check, Copy, Loader2, Mail } from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui";
import { useInviteOrgMember } from "@/hooks/use-org-invitations";
import type { OrgRole, CreateOrgInvitationResponse } from "@/types";

export interface InviteMemberDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  orgHandle: string;
}

/**
 * Dialog to send an invitation to an org. On success, surfaces the generated
 * accept link so the inviter can copy and share it manually (email delivery
 * is the backend's responsibility, but we still show the token for OOB use).
 */
export function InviteMemberDialog({
  open,
  onOpenChange,
  orgHandle,
}: InviteMemberDialogProps) {
  const t = useTranslations("orgs.invitations");
  const tRoles = useTranslations("orgs.roles");
  const tCommon = useTranslations("common");
  const invite = useInviteOrgMember(orgHandle);

  const [email, setEmail] = React.useState("");
  const [role, setRole] = React.useState<OrgRole>("Member");
  const [result, setResult] =
    React.useState<CreateOrgInvitationResponse | null>(null);
  const [copied, setCopied] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const acceptUrl = result
    ? `${typeof window !== "undefined" ? window.location.origin : ""}/invitations/${result.token}`
    : "";

  const reset = () => {
    setEmail("");
    setRole("Member");
    setResult(null);
    setCopied(false);
    setError(null);
  };

  const handleClose = (next: boolean) => {
    if (!next) reset();
    onOpenChange(next);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      const res = await invite.mutateAsync({ email: email.trim(), role });
      setResult(res);
    } catch (err) {
      setError((err as Error).message);
    }
  };

  const handleCopy = async () => {
    if (!acceptUrl) return;
    try {
      await navigator.clipboard.writeText(acceptUrl);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // ignore
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-[480px]">
        {result ? (
          <>
            <DialogHeader>
              <DialogTitle>{t("sentSuccess")}</DialogTitle>
              <DialogDescription>{t("copyLink")}</DialogDescription>
            </DialogHeader>
            <div className="space-y-3 py-4">
              <div className="flex items-center gap-2 rounded-lg border border-border bg-muted/30 p-2.5">
                <code className="flex-1 truncate text-xs text-foreground">
                  {acceptUrl}
                </code>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={handleCopy}
                >
                  {copied ? (
                    <>
                      <Check className="h-4 w-4" />
                      <span className="ml-1">{t("copied")}</span>
                    </>
                  ) : (
                    <>
                      <Copy className="h-4 w-4" />
                      <span className="ml-1">{t("copyLink")}</span>
                    </>
                  )}
                </Button>
              </div>
              <p className="text-xs text-muted-foreground">
                {t("expiresIn")}:{" "}
                {new Date(result.expiresAt).toLocaleString()}
              </p>
            </div>
            <DialogFooter>
              <Button onClick={() => handleClose(false)}>
                {tCommon("done")}
              </Button>
            </DialogFooter>
          </>
        ) : (
          <form onSubmit={handleSubmit}>
            <DialogHeader>
              <DialogTitle>{t("title")}</DialogTitle>
              <DialogDescription>{t("invitedToOrg")}</DialogDescription>
            </DialogHeader>
            <div className="space-y-5 py-4">
              <div className="space-y-2">
                <label
                  htmlFor="invite-email"
                  className="text-sm font-medium"
                >
                  {t("emailAddress")}
                </label>
                <div className="relative">
                  <Mail className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <input
                    id="invite-email"
                    type="email"
                    required
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder={t("emailPlaceholder")}
                    className="w-full rounded-lg border border-border bg-background py-2.5 pl-10 pr-3.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                  />
                </div>
              </div>
              <div className="space-y-2">
                <label
                  htmlFor="invite-role"
                  className="text-sm font-medium"
                >
                  {t("role")}
                </label>
                <select
                  id="invite-role"
                  value={role}
                  onChange={(e) => setRole(e.target.value as OrgRole)}
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  <option value="Owner">{tRoles("owner")}</option>
                  <option value="Admin">{tRoles("admin")}</option>
                  <option value="Member">{tRoles("member")}</option>
                </select>
              </div>
              {error && (
                <p className="rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-500">
                  {error}
                </p>
              )}
            </div>
            <DialogFooter>
              <button
                type="button"
                onClick={() => handleClose(false)}
                className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
              >
                {tCommon("cancel")}
              </button>
              <button
                type="submit"
                disabled={!email || invite.isPending}
                className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white disabled:opacity-50"
              >
                {invite.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  t("sendInvitation")
                )}
              </button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}

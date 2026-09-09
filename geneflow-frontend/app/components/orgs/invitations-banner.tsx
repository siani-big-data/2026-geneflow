"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
import { Mail } from "lucide-react";
import { Link } from "@/lib/navigation";
import { useMyInvitations } from "@/hooks/use-org-invitations";

/**
 * Teal-accent banner shown at the top of the dashboard when the current user
 * has one or more pending org invitations. Hidden when there are none.
 */
export function InvitationsBanner() {
  const t = useTranslations("orgs.invitations");
  const { data } = useMyInvitations();

  const pending = React.useMemo(
    () => (data ?? []).filter((i) => i.status === "Pending"),
    [data],
  );

  if (pending.length === 0) return null;

  return (
    <Link
      href="/invitations"
      className="group flex items-center gap-3 rounded-xl border border-teal/40 bg-gradient-to-r from-teal/10 to-blue-deep/5 px-4 py-3 text-sm shadow-sm transition-all hover:from-teal/15 hover:to-blue-deep/10"
    >
      <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-teal/15 text-teal">
        <Mail className="h-4 w-4" />
      </span>
      <span className="flex-1">
        <span className="font-medium text-foreground">
          {t("banner")}
        </span>
        <span className="ml-1 text-muted-foreground">
          ({pending.length})
        </span>
      </span>
      <span className="text-xs font-medium text-teal transition-transform group-hover:translate-x-0.5">
        →
      </span>
    </Link>
  );
}

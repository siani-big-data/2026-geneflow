"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
import { Loader2 } from "lucide-react";
import { InvitationCard } from "@/components/orgs";
import { useMyInvitations } from "@/hooks/use-org-invitations";

export default function InvitationsPage() {
  const t = useTranslations("orgs.invitations");
  const { data, isLoading } = useMyInvitations();

  const pending = React.useMemo(
    () => (data ?? []).filter((i) => i.status === "Pending"),
    [data],
  );

  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-2xl font-semibold text-foreground">{t("title")}</h1>
      </header>

      {isLoading ? (
        <div className="flex h-48 items-center justify-center">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      ) : pending.length === 0 ? (
        <div className="rounded-lg border border-border bg-card p-12 text-center text-sm text-muted-foreground">
          {t("empty")}
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {pending.map((inv) => (
            <InvitationCard key={inv.token} invitation={inv} />
          ))}
        </div>
      )}
    </div>
  );
}

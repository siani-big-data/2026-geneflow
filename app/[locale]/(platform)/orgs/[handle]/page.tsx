"use client";

import * as React from "react";
import { useParams } from "next/navigation";
import { Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";
import { OrgHeader } from "@/components/orgs";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui";
import { useOrg } from "@/hooks/use-orgs";

export default function OrgOverviewPage() {
  const params = useParams<{ handle: string }>();
  const handle = params?.handle ?? "";
  const { data: org, isLoading, error } = useOrg(handle);
  const t = useTranslations("orgs.header");

  if (isLoading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-teal" />
      </div>
    );
  }

  if (error || !org) {
    return (
      <div className="rounded-lg border border-border bg-card p-8 text-center text-sm text-muted-foreground">
        Failed to load organization.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <OrgHeader org={org} />

      {org.description && (
        <Card>
          <CardContent className="pt-6">
            <p className="text-sm leading-relaxed text-foreground">
              {org.description}
            </p>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t("overview")}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            {/* Studies-by-org listing arrives in a follow-up phase. */}
            No studies yet — coming soon.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

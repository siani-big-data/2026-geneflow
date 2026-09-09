"use client";

import * as React from "react";
import { useParams } from "next/navigation";
import { Loader2, Star, Eye, Users } from "lucide-react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import { OrgHeader } from "@/components/orgs";
import { Card, CardContent, CardHeader, CardTitle, Badge } from "@/components/ui";
import { useOrg, useOrgStudies } from "@/hooks/use-orgs";

export default function OrgOverviewPage() {
  const params = useParams<{ handle: string }>();
  const handle = params?.handle ?? "";
  const { data: org, isLoading, error } = useOrg(handle);
  const { data: studiesPage, isLoading: studiesLoading } = useOrgStudies(handle);
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

  const studies = studiesPage?.items ?? [];

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
          {studiesLoading ? (
            <div className="flex h-24 items-center justify-center">
              <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
            </div>
          ) : studies.length === 0 ? (
            <p className="text-sm text-muted-foreground">No studies yet.</p>
          ) : (
            <ul className="divide-y divide-border">
              {studies.map((study) => (
                <li key={study.id} className="py-3 first:pt-0 last:pb-0">
                  <Link
                    href={`/studies/${study.id}` as never}
                    className="flex items-start justify-between gap-4 rounded-md p-2 -m-2 transition hover:bg-muted/40"
                  >
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <h3 className="truncate text-sm font-medium text-foreground">
                          {study.title}
                        </h3>
                        <Badge
                          variant={
                            study.statusName === "published" ? "default" : "outline"
                          }
                          className="text-[10px]"
                        >
                          {study.statusName}
                        </Badge>
                      </div>
                      {study.description && (
                        <p className="mt-1 line-clamp-2 text-xs text-muted-foreground">
                          {study.description}
                        </p>
                      )}
                      <div className="mt-2 flex items-center gap-3 text-[11px] text-muted-foreground">
                        <span className="inline-flex items-center gap-1">
                          <Users className="h-3 w-3" />
                          {study.members?.length ?? 0}
                        </span>
                        <span className="inline-flex items-center gap-1">
                          <Star className="h-3 w-3" />
                          {study.metrics?.starsCount ?? 0}
                        </span>
                        <span className="inline-flex items-center gap-1">
                          <Eye className="h-3 w-3" />
                          {study.metrics?.viewsCount ?? 0}
                        </span>
                      </div>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

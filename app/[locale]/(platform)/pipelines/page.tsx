"use client";

import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import { PageHeader } from "@/components/layout";
import { useMyStudies } from "@/hooks";
import { ArrowRight, Loader2, Workflow } from "lucide-react";

export default function PipelinesPage() {
  const t = useTranslations("pipelines");
  const { data, isLoading } = useMyStudies(1, 50);

  const studies = data?.items ?? [];

  return (
    <div className="space-y-6">
      <PageHeader title={t("title")} description={t("description")} />

      {isLoading ? (
        <div className="flex h-64 items-center justify-center">
          <Loader2 className="h-8 w-8 animate-spin text-teal" />
        </div>
      ) : studies.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-border bg-muted/20 py-16 text-center">
          <div className="mb-4 rounded-full bg-teal/10 p-4">
            <Workflow className="h-8 w-8 text-teal" />
          </div>
          <h3 className="mb-1 text-lg font-medium text-foreground">
            {t("noStudies")}
          </h3>
          <p className="mb-6 max-w-sm text-sm text-muted-foreground">
            {t("noStudiesDesc")}
          </p>
          <Link
            href="/studies"
            className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90"
          >
            {t("goToStudies")}
          </Link>
        </div>
      ) : (
        <>
          <p className="text-sm text-muted-foreground">{t("selectStudy")}</p>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {studies.map((study) => (
              <Link
                key={study.id}
                href={`/studies/${study.id}`}
                className="group flex items-center gap-4 rounded-xl border border-border bg-card p-4 transition-all hover:border-teal hover:shadow-sm"
              >
                <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-lg bg-teal/10">
                  <Workflow className="h-5 w-5 text-teal" />
                </div>
                <div className="min-w-0 flex-1">
                  <h3 className="truncate font-medium text-foreground">
                    {study.title}
                  </h3>
                  <p className="truncate text-xs text-muted-foreground">
                    {study.description ?? t("noDescription")}
                  </p>
                </div>
                <ArrowRight className="h-4 w-4 flex-shrink-0 text-muted-foreground transition-transform group-hover:translate-x-1 group-hover:text-teal" />
              </Link>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

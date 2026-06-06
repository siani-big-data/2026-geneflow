"use client";

/**
 * Home — unified GitHub-style landing page.
 *
 * Composition:
 *  - Header with greeting.
 *  - Compact KPI strip (4 cards) for at-a-glance state.
 *  - Two-column body:
 *      · Main column (2/3): `MyActivityPanel` — cursor-paginated feed.
 *      · Side column   (1/3): "Your studies" shortcut + recent pipeline runs.
 */

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { CheckCircle2, Clock, FileText, Loader2, Users } from "lucide-react";
import { Card, CardContent } from "@/components/ui";
import { usageService } from "@/services";
import { useAuthStore } from "@/stores/auth-store";
import {
  MyActivityPanel,
  RecentPipelinesPanel,
  RecentStudiesPanel,
} from "@/components/dashboard";
import type { DashboardStats } from "@/types";

export default function DashboardPage() {
  const t = useTranslations("dashboard");
  const { profile } = useAuthStore();

  const displayName = profile?.firstName || profile?.fullName || "User";

  // -- KPI strip ------------------------------------------------------------

  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [isLoadingStats, setIsLoadingStats] = useState(true);

  useEffect(() => {
    let cancelled = false;
    async function fetchStats() {
      setIsLoadingStats(true);
      try {
        const data = await usageService.getDashboardStats();
        if (!cancelled) setStats(data);
      } catch (err) {
        console.error("Failed to fetch dashboard stats:", err);
        if (!cancelled) {
          setStats({
            activeStudies: 0,
            processedTraces: 0,
            pendingTraces: 0,
            teamActivity: 0,
            alignmentsCompleted: 0,
          });
        }
      } finally {
        if (!cancelled) setIsLoadingStats(false);
      }
    }
    fetchStats();
    return () => {
      cancelled = true;
    };
  }, []);

  const formatNumber = (n: number) => n.toLocaleString();

  const metrics = [
    {
      label: t("metrics.activeStudies"),
      value: isLoadingStats ? "-" : formatNumber(stats?.activeStudies ?? 0),
      icon: FileText,
      color: "#0d9488",
      bgColor: "bg-teal/10",
      isLoading: isLoadingStats,
    },
    {
      label: t("metrics.processedTraces"),
      value: isLoadingStats ? "-" : formatNumber(stats?.processedTraces ?? 0),
      icon: CheckCircle2,
      color: "#10b981",
      bgColor: "bg-emerald-500/10",
      isLoading: isLoadingStats,
    },
    {
      label: t("metrics.pendingTraces"),
      value: isLoadingStats ? "-" : formatNumber(stats?.pendingTraces ?? 0),
      icon: Clock,
      color: "#f59e0b",
      bgColor: "bg-amber-500/10",
      isLoading: isLoadingStats,
    },
    {
      label: t("metrics.teamActivity"),
      value: isLoadingStats ? "-" : formatNumber(stats?.teamActivity ?? 0),
      icon: Users,
      color: "#1e40af",
      bgColor: "bg-blue-deep/10",
      isLoading: isLoadingStats,
    },
  ];

  // -- Render ---------------------------------------------------------------

  return (
    <div className="space-y-8">
      <header className="flex items-start justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold text-foreground">
            {t("title")}
          </h1>
          <p className="text-sm text-muted-foreground">
            {t("welcome", { name: displayName })}
          </p>
        </div>
      </header>

      {/* Compact KPI strip ------------------------------------------------ */}
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        {metrics.map((metric) => {
          const Icon = metric.icon;
          return (
            <Card key={metric.label} className="shadow-sm">
              <CardContent className="p-4">
                <div className="mb-3 flex items-start justify-between">
                  <div className={`rounded-lg p-2 ${metric.bgColor}`}>
                    <Icon className="h-4 w-4" style={{ color: metric.color }} />
                  </div>
                </div>
                <p className="text-xs font-medium text-muted-foreground">
                  {metric.label}
                </p>
                {metric.isLoading ? (
                  <Loader2 className="mt-1 h-5 w-5 animate-spin text-muted-foreground" />
                ) : (
                  <p className="text-2xl font-semibold tracking-tight text-foreground">
                    {metric.value}
                  </p>
                )}
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Activity feed (main) + side shortcuts -------------------------- */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/*
          `flex` on the grid item gives the panel an explicit height
          reference, so the Card's internal `h-full` resolves against the
          row height (which is dictated by the taller side column).
        */}
        <section className="flex lg:col-span-2">
          {/*
            Activity is the protagonist of the home page. We ask for a
            generous first page (server clamps at MaxLimit=100) and the
            panel itself fills the available vertical space, matching the
            stacked Recent Studies + Recent Pipelines height.
          */}
          <MyActivityPanel pageSize={50} className="w-full" />
        </section>

        <aside className="space-y-6 lg:col-span-1">
          <RecentStudiesPanel />
          <RecentPipelinesPanel />
        </aside>
      </div>
    </div>
  );
}

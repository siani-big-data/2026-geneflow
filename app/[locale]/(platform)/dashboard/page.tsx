"use client";

import { useTranslations } from "next-intl";
import { Card, CardContent } from "@/components/ui";
import { StatusBadge } from "@/components/shared";
import { Link } from "@/lib/navigation";
import {
  FileText,
  CheckCircle2,
  Clock,
  Users,
  TrendingUp,
  Activity,
  ArrowUpRight,
  Zap,
} from "lucide-react";
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";

const activityData = [
  { time: "00:00", traces: 234, quality: 96 },
  { time: "04:00", traces: 189, quality: 95 },
  { time: "08:00", traces: 456, quality: 97 },
  { time: "12:00", traces: 678, quality: 96 },
  { time: "16:00", traces: 589, quality: 98 },
  { time: "20:00", traces: 423, quality: 97 },
  { time: "24:00", traces: 312, quality: 96 },
];

const recentStudies = [
  {
    id: "GF-2026-089",
    name: "GWAS - Type 2 Diabetes Cohort",
    pi: "Dr. Sarah Martinez",
    samples: 1247,
    traces: 8934,
    progress: 73,
    status: "processing" as const,
    updated: "12 min ago",
    quality: 97.2,
  },
  {
    id: "GF-2026-087",
    name: "Whole Exome Sequencing - Rare Disease",
    pi: "Dr. James Wong",
    samples: 342,
    traces: 2456,
    progress: 45,
    status: "active" as const,
    updated: "1 hour ago",
    quality: 96.8,
  },
  {
    id: "GF-2026-085",
    name: "Cancer Biomarkers - RNA-Seq Analysis",
    pi: "Dr. Emily Chen",
    samples: 856,
    traces: 6123,
    progress: 100,
    status: "completed" as const,
    updated: "3 hours ago",
    quality: 98.1,
  },
  {
    id: "GF-2026-082",
    name: "BRCA1/2 Targeted Panel Sequencing",
    pi: "Dr. Michael Park",
    samples: 125,
    traces: 892,
    progress: 89,
    status: "pending" as const,
    updated: "5 hours ago",
    quality: 97.5,
  },
];

const pipelineStatus = [
  {
    id: "PL-234",
    name: "Quality Control & Trimming",
    study: "GF-2026-089",
    status: "running" as const,
    progress: 67,
    samples: "834/1247",
    eta: "45 min",
  },
  {
    id: "PL-233",
    name: "Base Calling & Alignment",
    study: "GF-2026-087",
    status: "running" as const,
    progress: 89,
    samples: "305/342",
    eta: "12 min",
  },
  {
    id: "PL-231",
    name: "Variant Annotation",
    study: "GF-2026-085",
    status: "completed" as const,
    progress: 100,
    samples: "856/856",
    eta: "—",
  },
];

const recentActivityData: Array<{
  user: string;
  study: string;
  timeKey: string;
  timeCount: number;
  type: "upload" | "review" | "pipeline" | "export" | "create";
  actionParams: Record<string, string | number | Date>;
}> = [
  {
    user: "Dr. Sarah Martinez",
    study: "GF-2026-089",
    timeKey: "minutesAgo",
    timeCount: 5,
    type: "upload",
    actionParams: { count: 245 },
  },
  {
    user: "Dr. James Wong",
    study: "GF-2026-087",
    timeKey: "minutesAgo",
    timeCount: 23,
    type: "review",
    actionParams: {},
  },
  {
    user: "Dr. Emily Chen",
    study: "GF-2026-085",
    timeKey: "hourAgo",
    timeCount: 1,
    type: "pipeline",
    actionParams: {},
  },
  {
    user: "Dr. Michael Park",
    study: "GF-2026-082",
    timeKey: "hoursAgo",
    timeCount: 2,
    type: "export",
    actionParams: {},
  },
  {
    user: "Dr. Lisa Anderson",
    study: "GF-2026-090",
    timeKey: "hoursAgo",
    timeCount: 3,
    type: "create",
    actionParams: {},
  },
];

export default function DashboardPage() {
  const t = useTranslations("dashboard");

  const metrics = [
    {
      label: t("metrics.activeStudies"),
      value: "18",
      change: t("metrics.thisWeek", { count: 3 }),
      trend: "up",
      icon: FileText,
      color: "#0d9488",
      bgColor: "bg-teal/10",
    },
    {
      label: t("metrics.processedTraces"),
      value: "12,847",
      change: t("metrics.today", { count: "1,234" }),
      trend: "up",
      icon: CheckCircle2,
      color: "#10b981",
      bgColor: "bg-emerald-500/10",
    },
    {
      label: t("metrics.pendingTraces"),
      value: "2,341",
      change: t("metrics.processing"),
      trend: "neutral",
      icon: Clock,
      color: "#f59e0b",
      bgColor: "bg-amber-500/10",
    },
    {
      label: t("metrics.teamActivity"),
      value: "24",
      change: t("metrics.activeNow"),
      trend: "up",
      icon: Users,
      color: "#1e40af",
      bgColor: "bg-blue-deep/10",
    },
  ];

  return (
    <div className="space-y-8">
      {/* Page Header */}
      <div className="flex items-start justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold text-foreground">{t("title")}</h1>
          <p className="text-sm text-muted-foreground">
            {t("welcome", { name: "Dr. Martinez" })}
          </p>
        </div>
      </div>

      {/* Metrics Grid */}
      <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-4">
        {metrics.map((metric) => {
          const Icon = metric.icon;
          return (
            <Card key={metric.label} className="shadow-sm">
              <CardContent className="p-6">
                <div className="mb-4 flex items-start justify-between">
                  <div className={`rounded-lg p-3 ${metric.bgColor}`}>
                    <Icon className="h-5 w-5" style={{ color: metric.color }} />
                  </div>
                  {metric.trend === "up" && (
                    <div className="flex items-center gap-1 rounded-md border border-emerald-500/20 bg-emerald-500/10 px-2.5 py-1">
                      <TrendingUp className="h-3 w-3 text-emerald-500" />
                    </div>
                  )}
                </div>
                <div className="space-y-1.5">
                  <p className="text-sm font-medium text-muted-foreground">
                    {metric.label}
                  </p>
                  <p className="text-3xl font-semibold tracking-tight text-foreground">
                    {metric.value}
                  </p>
                  <p className="text-xs text-muted-foreground">{metric.change}</p>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Activity Chart */}
      <Card className="shadow-sm">
        <CardContent className="p-6">
          <div className="mb-6 flex items-center justify-between">
            <div>
              <h2 className="text-base font-semibold text-foreground">
                {t("chart.title")}
              </h2>
              <p className="mt-1 text-sm text-muted-foreground">
                {t("chart.description")}
              </p>
            </div>
            <div className="flex items-center gap-2">
              <div className="flex items-center gap-2 rounded-lg bg-muted/50 px-3 py-1.5">
                <div className="h-2 w-2 rounded-full bg-teal" />
                <span className="text-xs text-muted-foreground">
                  {t("chart.tracesProcessed")}
                </span>
              </div>
              <div className="flex items-center gap-2 rounded-lg bg-muted/50 px-3 py-1.5">
                <div className="h-2 w-2 rounded-full bg-blue-deep" />
                <span className="text-xs text-muted-foreground">{t("chart.qualityScore")}</span>
              </div>
            </div>
          </div>
          <ResponsiveContainer width="100%" height={300}>
            <AreaChart data={activityData}>
              <defs>
                <linearGradient id="colorTraces" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#0d9488" stopOpacity={0.3} />
                  <stop offset="95%" stopColor="#0d9488" stopOpacity={0} />
                </linearGradient>
                <linearGradient id="colorQuality" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#1e40af" stopOpacity={0.3} />
                  <stop offset="95%" stopColor="#1e40af" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" vertical={false} />
              <XAxis
                dataKey="time"
                tick={{ fill: "#64748b", fontSize: 12 }}
                axisLine={{ stroke: "#e5e7eb" }}
              />
              <YAxis
                yAxisId="left"
                tick={{ fill: "#64748b", fontSize: 12 }}
                axisLine={{ stroke: "#e5e7eb" }}
              />
              <YAxis
                yAxisId="right"
                orientation="right"
                tick={{ fill: "#64748b", fontSize: 12 }}
                axisLine={{ stroke: "#e5e7eb" }}
                domain={[90, 100]}
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: "#ffffff",
                  border: "1px solid #e5e7eb",
                  borderRadius: "8px",
                  fontSize: "12px",
                  boxShadow: "0 4px 6px -1px rgb(0 0 0 / 0.1)",
                }}
              />
              <Area
                yAxisId="left"
                type="monotone"
                dataKey="traces"
                stroke="#0d9488"
                strokeWidth={2}
                fillOpacity={1}
                fill="url(#colorTraces)"
              />
              <Area
                yAxisId="right"
                type="monotone"
                dataKey="quality"
                stroke="#1e40af"
                strokeWidth={2}
                fillOpacity={1}
                fill="url(#colorQuality)"
              />
            </AreaChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      {/* Two Column Layout */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Recent Studies - Takes 2 columns */}
        <Card className="overflow-hidden shadow-sm lg:col-span-2">
          <div className="border-b border-border p-6">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-base font-semibold text-foreground">
                  {t("recentStudies.title")}
                </h2>
                <p className="mt-1 text-sm text-muted-foreground">
                  {t("recentStudies.description")}
                </p>
              </div>
              <Link
                href="/studies"
                className="flex items-center gap-1.5 text-sm font-medium text-teal transition-colors duration-200 hover:text-teal/80"
              >
                {t("recentStudies.viewAll")}
                <ArrowUpRight className="h-4 w-4" />
              </Link>
            </div>
          </div>
          <div className="divide-y divide-border">
            {recentStudies.map((study) => (
              <Link
                key={study.id}
                href={`/studies/${study.id}`}
                className="group block p-6 transition-all duration-200 hover:bg-muted/30"
              >
                <div className="mb-3 flex items-start justify-between">
                  <div className="flex-1">
                    <div className="mb-1.5 flex items-center gap-3">
                      <span className="text-sm font-semibold tracking-tight text-blue-deep">
                        {study.id}
                      </span>
                      <StatusBadge status={study.status} />
                    </div>
                    <h3 className="mb-1 font-semibold text-foreground transition-colors duration-200 group-hover:text-teal">
                      {study.name}
                    </h3>
                    <p className="text-sm font-medium text-muted-foreground">
                      {study.pi}
                    </p>
                  </div>
                </div>

                <div className="mb-4 mt-4 grid grid-cols-4 gap-4">
                  <div>
                    <p className="mb-1 text-xs font-medium text-muted-foreground">
                      {t("recentStudies.samples")}
                    </p>
                    <p className="text-sm font-semibold text-foreground">
                      {study.samples}
                    </p>
                  </div>
                  <div>
                    <p className="mb-1 text-xs font-medium text-muted-foreground">
                      {t("recentStudies.traces")}
                    </p>
                    <p className="text-sm font-semibold text-foreground">
                      {study.traces.toLocaleString()}
                    </p>
                  </div>
                  <div>
                    <p className="mb-1 text-xs font-medium text-muted-foreground">
                      {t("recentStudies.quality")}
                    </p>
                    <p className="text-sm font-semibold text-emerald-500">
                      {study.quality}%
                    </p>
                  </div>
                  <div>
                    <p className="mb-1 text-xs font-medium text-muted-foreground">
                      {t("recentStudies.updated")}
                    </p>
                    <p className="text-sm font-semibold text-foreground">
                      {study.updated}
                    </p>
                  </div>
                </div>

                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-medium text-muted-foreground">
                      {t("recentStudies.progress")}
                    </span>
                    <span className="text-xs font-semibold text-foreground">
                      {study.progress}%
                    </span>
                  </div>
                  <div className="h-2 overflow-hidden rounded-full bg-muted">
                    <div
                      className="h-full rounded-full bg-gradient-to-r from-teal to-teal/80 transition-all duration-500"
                      style={{ width: `${study.progress}%` }}
                    />
                  </div>
                </div>
              </Link>
            ))}
          </div>
        </Card>

        {/* Pipeline Status - Takes 1 column */}
        <Card className="overflow-hidden shadow-sm">
          <div className="border-b border-border p-6">
            <h2 className="text-base font-semibold text-foreground">
              {t("pipelines.title")}
            </h2>
            <p className="mt-1 text-sm text-muted-foreground">
              {t("pipelines.description")}
            </p>
          </div>
          <div className="divide-y divide-border">
            {pipelineStatus.map((pipeline) => (
              <div
                key={pipeline.id}
                className="group p-5 transition-all duration-200 hover:bg-muted/30"
              >
                <div className="mb-3 flex items-start justify-between">
                  <div className="flex-1">
                    <div className="mb-1.5 flex items-center gap-2">
                      <span
                        className={`inline-flex items-center gap-1.5 rounded-lg border px-2.5 py-1 text-xs font-medium transition-all duration-200 ${
                          pipeline.status === "running"
                            ? "border-teal/20 bg-teal/10 text-teal"
                            : "border-emerald-500/20 bg-emerald-500/10 text-emerald-500"
                        }`}
                      >
                        {pipeline.status === "running" ? (
                          <Activity className="h-3.5 w-3.5" />
                        ) : (
                          <CheckCircle2 className="h-3.5 w-3.5" />
                        )}
                        {pipeline.status === "running" ? t("pipelines.running") : t("pipelines.completed")}
                      </span>
                    </div>
                    <h3 className="mb-1 text-sm font-semibold text-foreground transition-colors duration-200 group-hover:text-teal">
                      {pipeline.name}
                    </h3>
                    <p className="text-xs font-medium text-muted-foreground">
                      {pipeline.study}
                    </p>
                  </div>
                </div>

                <div className="space-y-3">
                  <div className="flex items-center justify-between text-xs">
                    <span className="font-medium text-muted-foreground">
                      {t("pipelines.samples")}: {pipeline.samples}
                    </span>
                    <span className="font-medium text-muted-foreground">
                      {t("pipelines.eta")}: {pipeline.eta}
                    </span>
                  </div>
                  <div className="space-y-2">
                    <div className="flex items-center justify-between text-xs">
                      <span className="font-medium text-muted-foreground">
                        {t("recentStudies.progress")}
                      </span>
                      <span className="font-semibold text-foreground">
                        {pipeline.progress}%
                      </span>
                    </div>
                    <div className="h-2 overflow-hidden rounded-full bg-muted">
                      <div
                        className="h-full rounded-full bg-teal transition-all duration-500"
                        style={{ width: `${pipeline.progress}%` }}
                      />
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
          <div className="border-t border-border bg-muted/20 p-4">
            <Link
              href="/pipelines"
              className="flex w-full items-center justify-center gap-2 rounded-lg border border-border px-4 py-2.5 text-sm font-medium text-foreground transition-all duration-200 hover:bg-card hover:shadow-sm active:scale-[0.98]"
            >
              <Activity className="h-4 w-4" />
              {t("pipelines.viewAll")}
            </Link>
          </div>
        </Card>
      </div>

      {/* Recent Analysis Activity */}
      <Card className="overflow-hidden shadow-sm">
        <div className="border-b border-border p-6">
          <h2 className="text-base font-semibold text-foreground">
            {t("recentActivity.title")}
          </h2>
          <p className="mt-1 text-sm text-muted-foreground">
            {t("recentActivity.description")}
          </p>
        </div>
        <div className="divide-y divide-border">
          {recentActivityData.map((activity, index) => (
            <div
              key={index}
              className="p-5 text-left transition-colors hover:bg-muted/20"
            >
              <div className="flex items-start gap-4">
                <div className="flex-shrink-0">
                  <div className="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-teal/20 to-blue-deep/20">
                    <div className="flex h-6 w-6 items-center justify-center rounded-full bg-gradient-to-br from-teal to-blue-deep">
                      {activity.type === "upload" && (
                        <Zap className="h-3.5 w-3.5 text-white" />
                      )}
                      {activity.type === "review" && (
                        <CheckCircle2 className="h-3.5 w-3.5 text-white" />
                      )}
                      {activity.type === "pipeline" && (
                        <Activity className="h-3.5 w-3.5 text-white" />
                      )}
                      {activity.type === "export" && (
                        <ArrowUpRight className="h-3.5 w-3.5 text-white" />
                      )}
                      {activity.type === "create" && (
                        <FileText className="h-3.5 w-3.5 text-white" />
                      )}
                    </div>
                  </div>
                </div>
                <div className="min-w-0 flex-1">
                  <p className="text-sm text-foreground">
                    <span className="font-medium">{activity.user}</span>{" "}
                    <span className="text-muted-foreground">
                      {t(`recentActivity.actions.${activity.type}`, activity.actionParams)}
                    </span>
                  </p>
                  <div className="mt-1 flex items-center gap-2">
                    <span className="text-xs font-medium text-blue-deep">
                      {activity.study}
                    </span>
                    <span className="text-xs text-muted-foreground">•</span>
                    <span className="text-xs text-muted-foreground">
                      {t(`time.${activity.timeKey}`, { count: activity.timeCount })}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}

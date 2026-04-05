"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { PageHeader } from "@/components/layout";
import { Button } from "@/components/ui";
import { StatusBadge } from "@/components/shared";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Plus,
  Play,
  Pause,
  RotateCcw,
  Activity,
  Cpu,
  HardDrive,
  Zap,
  RefreshCw,
} from "lucide-react";
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import {
  mockPipelines,
  resourceUsageData,
  throughputData,
  getPipelineStats,
  type PipelineWithMetrics,
} from "@/mocks/data";
import { cn } from "@/lib/utils";

function formatTimeAgo(dateString?: string): string {
  if (!dateString) return "—";
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / (1000 * 60));
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60));

  if (diffMins < 60) return `${diffMins} min ago`;
  if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? "s" : ""} ago`;
  return `${Math.floor(diffHours / 24)} day${Math.floor(diffHours / 24) > 1 ? "s" : ""} ago`;
}

export default function PipelinesPage() {
  const t = useTranslations("pipelines");
  const tCommon = useTranslations("common");
  const [pipelines, setPipelines] = useState<PipelineWithMetrics[]>(mockPipelines);
  const [newPipelineOpen, setNewPipelineOpen] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const stats = getPipelineStats();

  const activePipelines = pipelines.filter(
    (p) => p.status === "running" || p.status === "queued" || p.status === "failed"
  );

  const handleRefresh = () => {
    setIsRefreshing(true);
    setTimeout(() => {
      setPipelines([...mockPipelines]);
      setIsRefreshing(false);
    }, 1000);
  };

  const handlePausePipeline = (id: string) => {
    setPipelines((prev) =>
      prev.map((p) =>
        p.id === id ? { ...p, status: "queued" as const } : p
      )
    );
  };

  const handleResumePipeline = (id: string) => {
    setPipelines((prev) =>
      prev.map((p) =>
        p.id === id ? { ...p, status: "running" as const } : p
      )
    );
  };

  const handleRetryPipeline = (id: string) => {
    setPipelines((prev) =>
      prev.map((p) =>
        p.id === id ? { ...p, status: "running" as const, error: undefined } : p
      )
    );
  };

  const handleCreatePipeline = () => {
    console.log("Creating new pipeline");
    setNewPipelineOpen(false);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={t("title")}
        description={t("description")}
      >
        <Button variant="outline" onClick={handleRefresh} disabled={isRefreshing}>
          <RefreshCw className={cn("h-4 w-4", isRefreshing && "animate-spin")} />
          {isRefreshing ? t("refreshing") : t("refresh")}
        </Button>
        <Button onClick={() => setNewPipelineOpen(true)}>
          <Plus className="h-4 w-4" />
          {t("newPipeline")}
        </Button>
      </PageHeader>

      {/* System Metrics */}
      <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
        <div className="rounded-lg border border-border bg-card p-5">
          <div className="mb-3 flex items-center justify-between">
            <div className="rounded-lg bg-teal/10 p-2">
              <Activity className="h-5 w-5 text-teal" />
            </div>
            <span className="text-xs text-muted-foreground">{t("metrics.active")}</span>
          </div>
          <p className="text-2xl font-semibold text-foreground">{stats.running}</p>
          <p className="mt-1 text-sm text-muted-foreground">{t("metrics.runningPipelines")}</p>
        </div>

        <div className="rounded-lg border border-border bg-card p-5">
          <div className="mb-3 flex items-center justify-between">
            <div className="rounded-lg bg-blue-deep/10 p-2">
              <Cpu className="h-5 w-5 text-blue-deep" />
            </div>
            <span className="text-xs text-muted-foreground">{t("metrics.avg")}</span>
          </div>
          <p className="text-2xl font-semibold text-foreground">{stats.avgCpu}%</p>
          <p className="mt-1 text-sm text-muted-foreground">{t("metrics.cpuUtilization")}</p>
        </div>

        <div className="rounded-lg border border-border bg-card p-5">
          <div className="mb-3 flex items-center justify-between">
            <div className="rounded-lg bg-violet-500/10 p-2">
              <HardDrive className="h-5 w-5 text-violet-500" />
            </div>
            <span className="text-xs text-muted-foreground">{t("metrics.avg")}</span>
          </div>
          <p className="text-2xl font-semibold text-foreground">{stats.avgMemory}%</p>
          <p className="mt-1 text-sm text-muted-foreground">{t("metrics.memoryUsage")}</p>
        </div>

        <div className="rounded-lg border border-border bg-card p-5">
          <div className="mb-3 flex items-center justify-between">
            <div className="rounded-lg bg-emerald-500/10 p-2">
              <Zap className="h-5 w-5 text-emerald-500" />
            </div>
            <span className="text-xs text-muted-foreground">{t("metrics.today")}</span>
          </div>
          <p className="text-2xl font-semibold text-foreground">
            {stats.samplesProcessedToday.toLocaleString()}
          </p>
          <p className="mt-1 text-sm text-muted-foreground">{t("metrics.samplesProcessed")}</p>
        </div>
      </div>

      {/* Resource Charts */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <div className="rounded-lg border border-border bg-card p-6">
          <div className="mb-6 space-y-1">
            <h2 className="text-base font-semibold text-foreground">
              {t("charts.resourceUtilization")}
            </h2>
            <p className="text-sm text-muted-foreground">
              {t("charts.resourceDescription")}
            </p>
          </div>
          <ResponsiveContainer width="100%" height={240}>
            <LineChart data={resourceUsageData}>
              <CartesianGrid
                strokeDasharray="3 3"
                stroke="hsl(var(--border))"
                vertical={false}
              />
              <XAxis
                dataKey="time"
                tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
                axisLine={{ stroke: "hsl(var(--border))" }}
              />
              <YAxis
                tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
                axisLine={{ stroke: "hsl(var(--border))" }}
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: "hsl(var(--card))",
                  border: "1px solid hsl(var(--border))",
                  borderRadius: "6px",
                  fontSize: "12px",
                }}
              />
              <Line
                type="monotone"
                dataKey="cpu"
                stroke="#1e40af"
                strokeWidth={2}
                dot={false}
                name="CPU %"
              />
              <Line
                type="monotone"
                dataKey="memory"
                stroke="#0d9488"
                strokeWidth={2}
                dot={false}
                name="Memory %"
              />
            </LineChart>
          </ResponsiveContainer>
          <div className="mt-4 flex items-center justify-center gap-6">
            <div className="flex items-center gap-2">
              <div className="h-3 w-3 rounded-full bg-blue-deep" />
              <span className="text-xs text-muted-foreground">{t("charts.cpu")}</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="h-3 w-3 rounded-full bg-teal" />
              <span className="text-xs text-muted-foreground">{t("charts.memory")}</span>
            </div>
          </div>
        </div>

        <div className="rounded-lg border border-border bg-card p-6">
          <div className="mb-6 space-y-1">
            <h2 className="text-base font-semibold text-foreground">
              {t("charts.sampleThroughput")}
            </h2>
            <p className="text-sm text-muted-foreground">
              {t("charts.throughputDescription")}
            </p>
          </div>
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={throughputData}>
              <CartesianGrid
                strokeDasharray="3 3"
                stroke="hsl(var(--border))"
                vertical={false}
              />
              <XAxis
                dataKey="hour"
                tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
                axisLine={{ stroke: "hsl(var(--border))" }}
              />
              <YAxis
                tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
                axisLine={{ stroke: "hsl(var(--border))" }}
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: "hsl(var(--card))",
                  border: "1px solid hsl(var(--border))",
                  borderRadius: "6px",
                  fontSize: "12px",
                }}
              />
              <Bar dataKey="samples" fill="#0d9488" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Active Pipelines */}
      <div className="rounded-lg border border-border bg-card">
        <div className="border-b border-border p-6">
          <h2 className="text-base font-semibold text-foreground">
            {t("activePipelines.title")}
          </h2>
          <p className="mt-1 text-sm text-muted-foreground">
            {t("activePipelines.description")}
          </p>
        </div>
        <div className="divide-y divide-border">
          {activePipelines.map((pipeline) => (
            <div
              key={pipeline.id}
              className="p-6 transition-colors hover:bg-muted/20"
            >
              <div className="mb-4 flex items-start justify-between">
                <div className="flex-1">
                  <div className="mb-1 flex items-center gap-3">
                    <span className="text-sm font-medium text-blue-deep">
                      {pipeline.id}
                    </span>
                    <StatusBadge
                      status={
                        pipeline.status === "running"
                          ? "active"
                          : pipeline.status === "queued"
                            ? "pending"
                            : pipeline.status === "failed"
                              ? "failed"
                              : "completed"
                      }
                    />
                  </div>
                  <h3 className="mb-1 font-medium text-foreground">
                    {pipeline.name}
                  </h3>
                  <p className="text-sm text-muted-foreground">
                    {t("table.study")}: {pipeline.studyName}
                  </p>
                </div>
                <div className="flex gap-2">
                  {pipeline.status === "running" && (
                    <button
                      onClick={() => handlePausePipeline(pipeline.id)}
                      className="rounded-md border border-border p-2 text-foreground transition-colors hover:bg-muted"
                      aria-label={`Pause pipeline ${pipeline.name}`}
                    >
                      <Pause className="h-4 w-4" />
                    </button>
                  )}
                  {pipeline.status === "failed" && (
                    <button
                      onClick={() => handleRetryPipeline(pipeline.id)}
                      className="rounded-md border border-border p-2 text-foreground transition-colors hover:bg-muted"
                      aria-label={`Retry pipeline ${pipeline.name}`}
                    >
                      <RotateCcw className="h-4 w-4" />
                    </button>
                  )}
                  {pipeline.status === "queued" && (
                    <button
                      onClick={() => handleResumePipeline(pipeline.id)}
                      className="rounded-md border border-border p-2 text-foreground transition-colors hover:bg-muted"
                      aria-label={`Start pipeline ${pipeline.name}`}
                    >
                      <Play className="h-4 w-4" />
                    </button>
                  )}
                </div>
              </div>

              <div className="mb-4 grid grid-cols-2 gap-4 md:grid-cols-5">
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("table.samples")}</p>
                  <p className="text-sm font-medium text-foreground">
                    {pipeline.metrics?.samplesProcessed || 0}/
                    {pipeline.metrics?.samplesTotal || 0}
                  </p>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("table.started")}</p>
                  <p className="text-sm font-medium text-foreground">
                    {formatTimeAgo(pipeline.startedAt)}
                  </p>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("table.eta")}</p>
                  <p className="text-sm font-medium text-foreground">
                    {pipeline.metrics?.eta || "—"}
                  </p>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("table.cpu")}</p>
                  <p className="text-sm font-medium text-foreground">
                    {pipeline.metrics?.cpu || 0}%
                  </p>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("table.memory")}</p>
                  <p className="text-sm font-medium text-foreground">
                    {pipeline.metrics?.memory || 0}%
                  </p>
                </div>
              </div>

              {pipeline.error && (
                <div className="mb-4 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-500">
                  {pipeline.error}
                </div>
              )}

              <div className="space-y-2">
                <div className="flex items-center justify-between text-xs">
                  <span className="text-muted-foreground">{t("table.progress")}</span>
                  <span className="font-medium text-foreground">
                    {pipeline.progress}%
                  </span>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-muted">
                  <div
                    className={cn(
                      "h-full rounded-full transition-all",
                      pipeline.status === "failed" ? "bg-red-500" : "bg-teal"
                    )}
                    style={{ width: `${pipeline.progress}%` }}
                  />
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* New Pipeline Dialog */}
      <Dialog open={newPipelineOpen} onOpenChange={setNewPipelineOpen}>
        <DialogContent className="sm:max-w-[540px]">
          <DialogHeader>
            <DialogTitle>{t("create.title")}</DialogTitle>
            <DialogDescription>
              {t("create.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-5 py-4">
            <div className="space-y-2">
              <label htmlFor="pipeline-name" className="text-sm font-medium text-foreground">
                {t("create.pipelineName")}
              </label>
              <input
                id="pipeline-name"
                type="text"
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                placeholder={t("create.pipelineNamePlaceholder")}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="pipeline-type" className="text-sm font-medium text-foreground">
                {t("create.pipelineType")}
              </label>
              <select
                id="pipeline-type"
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
              >
                <option value="">{t("create.selectType")}</option>
                <option value="qc">{t("create.types.qc")}</option>
                <option value="variant">{t("create.types.variant")}</option>
                <option value="alignment">{t("create.types.alignment")}</option>
                <option value="assembly">{t("create.types.assembly")}</option>
                <option value="annotation">{t("create.types.annotation")}</option>
              </select>
            </div>

            <div className="space-y-2">
              <label htmlFor="study-select" className="text-sm font-medium text-foreground">
                {t("create.targetStudy")}
              </label>
              <select
                id="study-select"
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
              >
                <option value="">{t("create.selectStudy")}</option>
                <option value="GF-2026-089">GF-2026-089 - Type 2 Diabetes GWAS</option>
                <option value="GF-2026-087">GF-2026-087 - Rare Disease Panel</option>
                <option value="GF-2026-085">GF-2026-085 - Cancer Biomarkers</option>
              </select>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label htmlFor="priority" className="text-sm font-medium text-foreground">
                  {t("create.priority")}
                </label>
                <select
                  id="priority"
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                >
                  <option value="normal">{t("create.priorityNormal")}</option>
                  <option value="high">{t("create.priorityHigh")}</option>
                  <option value="urgent">{t("create.priorityUrgent")}</option>
                </select>
              </div>
              <div className="space-y-2">
                <label htmlFor="notifications" className="text-sm font-medium text-foreground">
                  {t("create.notifications")}
                </label>
                <select
                  id="notifications"
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                >
                  <option value="completion">{t("create.onCompletion")}</option>
                  <option value="error">{t("create.onErrorOnly")}</option>
                  <option value="none">{t("create.none")}</option>
                </select>
              </div>
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => setNewPipelineOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium text-foreground hover:bg-muted"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleCreatePipeline}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90"
            >
              {t("create.submit")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

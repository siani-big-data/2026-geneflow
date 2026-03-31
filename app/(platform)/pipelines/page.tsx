"use client";

import { useState } from "react";
import { PageHeader } from "@/components/layout";
import { Button } from "@/components/ui";
import { StatusBadge } from "@/components/shared";
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
  const [pipelines] = useState<PipelineWithMetrics[]>(mockPipelines);
  const stats = getPipelineStats();

  const activePipelines = pipelines.filter(
    (p) => p.status === "running" || p.status === "queued" || p.status === "failed"
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title="Pipeline Monitor"
        description="Real-time monitoring of data processing pipelines"
      >
        <Button variant="outline">
          <RefreshCw className="h-4 w-4" />
          Refresh
        </Button>
        <Button>
          <Plus className="h-4 w-4" />
          New Pipeline
        </Button>
      </PageHeader>

      {/* System Metrics */}
      <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
        <div className="rounded-lg border border-border bg-card p-5">
          <div className="mb-3 flex items-center justify-between">
            <div className="rounded-lg bg-teal/10 p-2">
              <Activity className="h-5 w-5 text-teal" />
            </div>
            <span className="text-xs text-muted-foreground">Active</span>
          </div>
          <p className="text-2xl font-semibold text-foreground">{stats.running}</p>
          <p className="mt-1 text-sm text-muted-foreground">Running pipelines</p>
        </div>

        <div className="rounded-lg border border-border bg-card p-5">
          <div className="mb-3 flex items-center justify-between">
            <div className="rounded-lg bg-blue-deep/10 p-2">
              <Cpu className="h-5 w-5 text-blue-deep" />
            </div>
            <span className="text-xs text-muted-foreground">Avg</span>
          </div>
          <p className="text-2xl font-semibold text-foreground">{stats.avgCpu}%</p>
          <p className="mt-1 text-sm text-muted-foreground">CPU utilization</p>
        </div>

        <div className="rounded-lg border border-border bg-card p-5">
          <div className="mb-3 flex items-center justify-between">
            <div className="rounded-lg bg-violet-500/10 p-2">
              <HardDrive className="h-5 w-5 text-violet-500" />
            </div>
            <span className="text-xs text-muted-foreground">Avg</span>
          </div>
          <p className="text-2xl font-semibold text-foreground">{stats.avgMemory}%</p>
          <p className="mt-1 text-sm text-muted-foreground">Memory usage</p>
        </div>

        <div className="rounded-lg border border-border bg-card p-5">
          <div className="mb-3 flex items-center justify-between">
            <div className="rounded-lg bg-emerald-500/10 p-2">
              <Zap className="h-5 w-5 text-emerald-500" />
            </div>
            <span className="text-xs text-muted-foreground">Today</span>
          </div>
          <p className="text-2xl font-semibold text-foreground">
            {stats.samplesProcessedToday.toLocaleString()}
          </p>
          <p className="mt-1 text-sm text-muted-foreground">Samples processed</p>
        </div>
      </div>

      {/* Resource Charts */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <div className="rounded-lg border border-border bg-card p-6">
          <div className="mb-6 space-y-1">
            <h3 className="text-base font-semibold text-foreground">
              Resource Utilization
            </h3>
            <p className="text-sm text-muted-foreground">
              CPU and memory usage over time
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
              <span className="text-xs text-muted-foreground">CPU</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="h-3 w-3 rounded-full bg-teal" />
              <span className="text-xs text-muted-foreground">Memory</span>
            </div>
          </div>
        </div>

        <div className="rounded-lg border border-border bg-card p-6">
          <div className="mb-6 space-y-1">
            <h3 className="text-base font-semibold text-foreground">
              Sample Throughput
            </h3>
            <p className="text-sm text-muted-foreground">
              Samples processed per hour
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
          <h3 className="text-base font-semibold text-foreground">
            Active Pipelines
          </h3>
          <p className="mt-1 text-sm text-muted-foreground">
            Currently running and queued processing jobs
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
                  <h4 className="mb-1 font-medium text-foreground">
                    {pipeline.name}
                  </h4>
                  <p className="text-sm text-muted-foreground">
                    Study: {pipeline.studyName}
                  </p>
                </div>
                <div className="flex gap-2">
                  {pipeline.status === "running" && (
                    <button className="rounded-md border border-border p-2 text-foreground transition-colors hover:bg-muted">
                      <Pause className="h-4 w-4" />
                    </button>
                  )}
                  {pipeline.status === "failed" && (
                    <button className="rounded-md border border-border p-2 text-foreground transition-colors hover:bg-muted">
                      <RotateCcw className="h-4 w-4" />
                    </button>
                  )}
                  {pipeline.status === "queued" && (
                    <button className="rounded-md border border-border p-2 text-foreground transition-colors hover:bg-muted">
                      <Play className="h-4 w-4" />
                    </button>
                  )}
                </div>
              </div>

              <div className="mb-4 grid grid-cols-2 gap-4 md:grid-cols-5">
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">Samples</p>
                  <p className="text-sm font-medium text-foreground">
                    {pipeline.metrics?.samplesProcessed || 0}/
                    {pipeline.metrics?.samplesTotal || 0}
                  </p>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">Started</p>
                  <p className="text-sm font-medium text-foreground">
                    {formatTimeAgo(pipeline.startedAt)}
                  </p>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">ETA</p>
                  <p className="text-sm font-medium text-foreground">
                    {pipeline.metrics?.eta || "—"}
                  </p>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">CPU</p>
                  <p className="text-sm font-medium text-foreground">
                    {pipeline.metrics?.cpu || 0}%
                  </p>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">Memory</p>
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
                  <span className="text-muted-foreground">Progress</span>
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
    </div>
  );
}

"use client";

import { PageHeader } from "@/components/layout";
import { Card, CardContent, CardHeader, CardTitle, Skeleton } from "@/components/ui";
import { useDashboardOverview, useRecentActivity, useRunningPipelines } from "@/hooks";
import { formatRelativeTime } from "@/lib/utils";
import { Beaker, Waves, Activity, BarChart3, FileText, Play, CheckCircle } from "lucide-react";
import Link from "next/link";
import { Progress } from "@/components/ui";

export default function DashboardPage() {
  const { data: overview, isLoading: overviewLoading } = useDashboardOverview();
  const { data: activity, isLoading: activityLoading } = useRecentActivity();
  const { data: runningPipelines } = useRunningPipelines();

  return (
    <div className="space-y-6">
      <PageHeader
        title="Dashboard"
        description="Welcome back, Dr. Sarah Martinez"
      />

      {/* Stats Grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <StatsCard
          title="Active Studies"
          value={overview?.studies.active}
          subtitle={`${overview?.studies.total} total`}
          icon={Beaker}
          loading={overviewLoading}
          href="/studies"
        />
        <StatsCard
          title="Total Traces"
          value={overview?.traces.total}
          subtitle={`${overview?.traces.thisWeek} this week`}
          icon={Waves}
          loading={overviewLoading}
          href="/traces"
        />
        <StatsCard
          title="Running Pipelines"
          value={overview?.pipelines.running}
          subtitle={`${overview?.pipelines.completed} completed`}
          icon={Activity}
          loading={overviewLoading}
          href="/pipelines"
        />
        <StatsCard
          title="Analyses"
          value={overview?.analyses.completed}
          subtitle="completed"
          icon={BarChart3}
          loading={overviewLoading}
          href="/analysis"
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Running Pipelines */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <Activity className="h-5 w-5 text-teal" />
              Running Pipelines
            </CardTitle>
          </CardHeader>
          <CardContent>
            {runningPipelines && runningPipelines.length > 0 ? (
              <div className="space-y-4">
                {runningPipelines.slice(0, 3).map((pipeline) => (
                  <div key={pipeline.id} className="space-y-2">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium">{pipeline.name}</span>
                      <span className="text-xs text-muted-foreground">
                        {pipeline.progress}%
                      </span>
                    </div>
                    <Progress value={pipeline.progress} className="h-2" />
                    <p className="text-xs text-muted-foreground">
                      {pipeline.studyName}
                    </p>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">
                No pipelines currently running.
              </p>
            )}
          </CardContent>
        </Card>

        {/* Recent Activity */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Recent Activity</CardTitle>
          </CardHeader>
          <CardContent>
            {activityLoading ? (
              <div className="space-y-3">
                {[1, 2, 3, 4].map((i) => (
                  <div key={i} className="flex items-center gap-3">
                    <Skeleton className="h-8 w-8 rounded-full" />
                    <div className="flex-1 space-y-1">
                      <Skeleton className="h-4 w-3/4" />
                      <Skeleton className="h-3 w-1/4" />
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="space-y-3">
                {activity?.map((item) => (
                  <div key={item.id} className="flex items-center gap-3">
                    <div className="flex h-8 w-8 items-center justify-center rounded-full bg-muted">
                      <ActivityIcon type={item.type} action={item.action} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm">
                        <span className="capitalize">{item.action}</span>{" "}
                        <span className="font-medium">{item.subject}</span>
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {formatRelativeTime(item.timestamp)}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function StatsCard({
  title,
  value,
  subtitle,
  icon: Icon,
  loading,
  href,
}: {
  title: string;
  value?: number;
  subtitle: string;
  icon: React.ElementType;
  loading: boolean;
  href: string;
}) {
  return (
    <Link href={href}>
      <Card className="transition-shadow hover:shadow-md">
        <CardContent className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-muted-foreground">{title}</p>
              {loading ? (
                <Skeleton className="mt-2 h-8 w-16" />
              ) : (
                <p className="mt-2 text-3xl font-semibold">{value}</p>
              )}
              <p className="mt-1 text-xs text-muted-foreground">{subtitle}</p>
            </div>
            <div className="rounded-full bg-teal/10 p-3">
              <Icon className="h-5 w-5 text-teal" />
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}

function ActivityIcon({ type, action }: { type: string; action: string }) {
  if (action === "completed") {
    return <CheckCircle className="h-4 w-4 text-emerald-500" />;
  }
  if (action === "started" || action === "processing") {
    return <Play className="h-4 w-4 text-blue-500" />;
  }
  if (type === "trace") {
    return <FileText className="h-4 w-4 text-muted-foreground" />;
  }
  if (type === "pipeline") {
    return <Activity className="h-4 w-4 text-muted-foreground" />;
  }
  return <Beaker className="h-4 w-4 text-muted-foreground" />;
}

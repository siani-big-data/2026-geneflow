"use client";

import { use } from "react";
import Link from "next/link";
import { PageHeader } from "@/components/layout";
import { Button, Card, CardContent, CardHeader, CardTitle, Tabs, TabsList, TabsTrigger, TabsContent, Skeleton, Avatar, AvatarFallback } from "@/components/ui";
import { StatusBadge, EmptyState } from "@/components/shared";
import { useStudy, useStudyTraces } from "@/hooks";
import { formatDate, formatBytes } from "@/lib/utils";
import { ArrowLeft, Upload, Settings, Waves, Users, Calendar, Globe, Lock, UsersRound } from "lucide-react";

export default function StudyDetailPage({
  params,
}: {
  params: Promise<{ studyId: string }>;
}) {
  const { studyId } = use(params);
  const { data: study, isLoading: studyLoading } = useStudy(studyId);
  const { data: traces, isLoading: tracesLoading } = useStudyTraces(studyId);

  if (studyLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-96" />
        <div className="grid gap-4 md:grid-cols-3">
          <Skeleton className="h-32" />
          <Skeleton className="h-32" />
          <Skeleton className="h-32" />
        </div>
      </div>
    );
  }

  if (!study) {
    return (
      <EmptyState
        title="Study not found"
        description="The study you're looking for doesn't exist or has been removed."
      >
        <Link href="/studies">
          <Button variant="outline">
            <ArrowLeft className="h-4 w-4" />
            Back to Studies
          </Button>
        </Link>
      </EmptyState>
    );
  }

  const visibilityIcon = {
    private: Lock,
    team: UsersRound,
    public: Globe,
  }[study.visibility];
  const VisibilityIcon = visibilityIcon;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link href="/studies">
          <Button variant="ghost" size="icon">
            <ArrowLeft className="h-4 w-4" />
          </Button>
        </Link>
        <PageHeader title={study.name} description={study.description}>
          <div className="flex items-center gap-2">
            <Button variant="outline">
              <Settings className="h-4 w-4" />
              Settings
            </Button>
            <Button>
              <Upload className="h-4 w-4" />
              Upload Traces
            </Button>
          </div>
        </PageHeader>
      </div>

      {/* Study Info Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="rounded-full bg-muted p-2">
              <Waves className="h-4 w-4 text-muted-foreground" />
            </div>
            <div>
              <p className="text-2xl font-semibold">{study.tracesCount}</p>
              <p className="text-sm text-muted-foreground">Traces</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="rounded-full bg-muted p-2">
              <Users className="h-4 w-4 text-muted-foreground" />
            </div>
            <div>
              <p className="text-2xl font-semibold">{study.members.length}</p>
              <p className="text-sm text-muted-foreground">Members</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="rounded-full bg-muted p-2">
              <VisibilityIcon className="h-4 w-4 text-muted-foreground" />
            </div>
            <div>
              <p className="text-lg font-semibold capitalize">{study.visibility}</p>
              <p className="text-sm text-muted-foreground">Visibility</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <StatusBadge status={study.status} />
            <div>
              <p className="text-sm text-muted-foreground">
                Created {formatDate(study.createdAt)}
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="traces" className="space-y-4">
        <TabsList>
          <TabsTrigger value="traces">Traces</TabsTrigger>
          <TabsTrigger value="members">Members</TabsTrigger>
          <TabsTrigger value="activity">Activity</TabsTrigger>
        </TabsList>

        <TabsContent value="traces">
          <Card>
            <CardHeader>
              <CardTitle>Traces</CardTitle>
            </CardHeader>
            <CardContent>
              {tracesLoading ? (
                <div className="space-y-2">
                  {[1, 2, 3].map((i) => (
                    <Skeleton key={i} className="h-12 w-full" />
                  ))}
                </div>
              ) : traces && traces.length > 0 ? (
                <div className="divide-y">
                  {traces.map((trace) => (
                    <Link
                      key={trace.id}
                      href={`/traces/${trace.id}`}
                      className="flex items-center justify-between py-3 hover:bg-muted/50 -mx-2 px-2 rounded"
                    >
                      <div className="flex items-center gap-3">
                        <Waves className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <p className="font-medium">{trace.name}</p>
                          <p className="text-sm text-muted-foreground">
                            {formatBytes(trace.fileSize)} · {trace.fileType.toUpperCase()}
                          </p>
                        </div>
                      </div>
                      <StatusBadge status={trace.status} />
                    </Link>
                  ))}
                </div>
              ) : (
                <EmptyState
                  icon={Waves}
                  title="No traces yet"
                  description="Upload your first trace to get started"
                >
                  <Button>
                    <Upload className="h-4 w-4" />
                    Upload Traces
                  </Button>
                </EmptyState>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="members">
          <Card>
            <CardHeader>
              <CardTitle>Team Members</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="divide-y">
                {study.members.map((member) => (
                  <div
                    key={member.user.id}
                    className="flex items-center justify-between py-3"
                  >
                    <div className="flex items-center gap-3">
                      <Avatar>
                        <AvatarFallback>
                          {member.user.name
                            .split(" ")
                            .map((n) => n[0])
                            .join("")}
                        </AvatarFallback>
                      </Avatar>
                      <div>
                        <p className="font-medium">{member.user.name}</p>
                        <p className="text-sm text-muted-foreground">
                          {member.user.email}
                        </p>
                      </div>
                    </div>
                    <span className="text-sm capitalize text-muted-foreground">
                      {member.role}
                    </span>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="activity">
          <Card>
            <CardHeader>
              <CardTitle>Recent Activity</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">
                Activity timeline coming soon.
              </p>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}

"use client";

import { use } from "react";
import Link from "next/link";
import { PageHeader } from "@/components/layout";
import { Button, Card, CardContent, CardHeader, CardTitle, Skeleton } from "@/components/ui";
import { StatusBadge, EmptyState } from "@/components/shared";
import { useTrace } from "@/hooks";
import { formatDate, formatBytes } from "@/lib/utils";
import { ArrowLeft, Download, Share2, Trash2, FileText, Calendar, HardDrive, Activity, Dna } from "lucide-react";

export default function TraceDetailPage({
  params,
}: {
  params: Promise<{ traceId: string }>;
}) {
  const { traceId } = use(params);
  const { data: trace, isLoading } = useTrace(traceId);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-64" />
        <div className="grid gap-4 md:grid-cols-4">
          <Skeleton className="h-24" />
          <Skeleton className="h-24" />
          <Skeleton className="h-24" />
          <Skeleton className="h-24" />
        </div>
        <Skeleton className="h-96" />
      </div>
    );
  }

  if (!trace) {
    return (
      <EmptyState
        title="Trace not found"
        description="The trace you're looking for doesn't exist or has been removed."
      >
        <Link href="/traces">
          <Button variant="outline">
            <ArrowLeft className="h-4 w-4" />
            Back to Traces
          </Button>
        </Link>
      </EmptyState>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link href="/traces">
          <Button variant="ghost" size="icon">
            <ArrowLeft className="h-4 w-4" />
          </Button>
        </Link>
        <PageHeader
          title={trace.name}
          description={
            <span className="flex items-center gap-2">
              <Link
                href={`/studies/${trace.studyId}`}
                className="text-teal hover:underline"
              >
                {trace.studyName}
              </Link>
              <span>·</span>
              <StatusBadge status={trace.status} />
            </span>
          }
        >
          <div className="flex items-center gap-2">
            <Button variant="outline" size="icon">
              <Share2 className="h-4 w-4" />
            </Button>
            <Button variant="outline" size="icon">
              <Trash2 className="h-4 w-4" />
            </Button>
            <Button>
              <Download className="h-4 w-4" />
              Download
            </Button>
          </div>
        </PageHeader>
      </div>

      {/* Trace Info Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="rounded-full bg-muted p-2">
              <FileText className="h-4 w-4 text-muted-foreground" />
            </div>
            <div>
              <p className="text-lg font-semibold">{trace.fileType.toUpperCase()}</p>
              <p className="text-sm text-muted-foreground">File Type</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="rounded-full bg-muted p-2">
              <HardDrive className="h-4 w-4 text-muted-foreground" />
            </div>
            <div>
              <p className="text-lg font-semibold">{formatBytes(trace.fileSize)}</p>
              <p className="text-sm text-muted-foreground">File Size</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="rounded-full bg-muted p-2">
              <Dna className="h-4 w-4 text-muted-foreground" />
            </div>
            <div>
              <p className="text-lg font-semibold">
                {trace.sequenceLength?.toLocaleString() ?? "-"}
              </p>
              <p className="text-sm text-muted-foreground">Sequence Length</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="rounded-full bg-muted p-2">
              <Activity className="h-4 w-4 text-muted-foreground" />
            </div>
            <div>
              <p className="text-lg font-semibold">
                {trace.qualityScore ? `${trace.qualityScore}%` : "-"}
              </p>
              <p className="text-sm text-muted-foreground">Quality Score</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Sequence Viewer Placeholder */}
      <Card>
        <CardHeader>
          <CardTitle>Sequence Viewer</CardTitle>
        </CardHeader>
        <CardContent>
          {trace.status === "completed" ? (
            <div className="rounded-lg bg-muted/50 p-8 text-center">
              <Dna className="mx-auto h-12 w-12 text-muted-foreground" />
              <p className="mt-4 text-lg font-medium">Sequence Visualization</p>
              <p className="mt-2 text-sm text-muted-foreground">
                Interactive sequence viewer will be displayed here.
              </p>
              <div className="mt-6 font-mono text-xs text-muted-foreground bg-muted p-4 rounded overflow-x-auto">
                ATCGATCGATCGATCGATCGATCGATCGATCGATCGATCG...
              </div>
            </div>
          ) : trace.status === "processing" ? (
            <div className="rounded-lg bg-muted/50 p-8 text-center">
              <Activity className="mx-auto h-12 w-12 text-blue-500 animate-pulse" />
              <p className="mt-4 text-lg font-medium">Processing...</p>
              <p className="mt-2 text-sm text-muted-foreground">
                This trace is currently being processed. Please check back shortly.
              </p>
            </div>
          ) : trace.status === "failed" ? (
            <div className="rounded-lg bg-destructive/10 p-8 text-center">
              <p className="text-lg font-medium text-destructive">Processing Failed</p>
              <p className="mt-2 text-sm text-muted-foreground">
                There was an error processing this trace. Please try uploading again.
              </p>
            </div>
          ) : (
            <div className="rounded-lg bg-muted/50 p-8 text-center">
              <p className="text-lg font-medium">Pending Processing</p>
              <p className="mt-2 text-sm text-muted-foreground">
                This trace is queued for processing.
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Metadata */}
      <Card>
        <CardHeader>
          <CardTitle>Details</CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="grid gap-4 md:grid-cols-2">
            <div>
              <dt className="text-sm font-medium text-muted-foreground">Uploaded by</dt>
              <dd className="mt-1">{trace.uploadedBy.name}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-muted-foreground">Upload date</dt>
              <dd className="mt-1">{formatDate(trace.createdAt)}</dd>
            </div>
            {trace.processedAt && (
              <div>
                <dt className="text-sm font-medium text-muted-foreground">Processed date</dt>
                <dd className="mt-1">{formatDate(trace.processedAt)}</dd>
              </div>
            )}
            <div>
              <dt className="text-sm font-medium text-muted-foreground">Study</dt>
              <dd className="mt-1">
                <Link
                  href={`/studies/${trace.studyId}`}
                  className="text-teal hover:underline"
                >
                  {trace.studyName}
                </Link>
              </dd>
            </div>
          </dl>
        </CardContent>
      </Card>
    </div>
  );
}

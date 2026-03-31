"use client";

import { useState } from "react";
import Link from "next/link";
import { PageHeader } from "@/components/layout";
import { Button, Card, CardContent, Input, Skeleton } from "@/components/ui";
import { StatusBadge, EmptyState } from "@/components/shared";
import { useTraces } from "@/hooks";
import { formatDate, formatBytes } from "@/lib/utils";
import { Upload, Search, Waves, FileText } from "lucide-react";

export default function TracesPage() {
  const [search, setSearch] = useState("");
  const { data, isLoading } = useTraces({ search: search || undefined });

  return (
    <div className="space-y-6">
      <PageHeader
        title="Traces"
        description="View and manage your genetic sequences"
      >
        <Button>
          <Upload className="h-4 w-4" />
          Upload Traces
        </Button>
      </PageHeader>

      {/* Search */}
      <div className="relative max-w-md">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          placeholder="Search traces..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-10"
        />
      </div>

      {/* Traces Table */}
      {isLoading ? (
        <Card>
          <CardContent className="p-0">
            <div className="divide-y">
              {[1, 2, 3, 4, 5].map((i) => (
                <div key={i} className="flex items-center gap-4 p-4">
                  <Skeleton className="h-10 w-10 rounded" />
                  <div className="flex-1 space-y-2">
                    <Skeleton className="h-4 w-1/3" />
                    <Skeleton className="h-3 w-1/4" />
                  </div>
                  <Skeleton className="h-6 w-20" />
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      ) : data?.data.length === 0 ? (
        <EmptyState
          icon={Waves}
          title="No traces found"
          description={
            search
              ? "Try adjusting your search terms"
              : "Upload your first trace to get started"
          }
        >
          {!search && (
            <Button>
              <Upload className="h-4 w-4" />
              Upload Traces
            </Button>
          )}
        </EmptyState>
      ) : (
        <Card>
          <CardContent className="p-0">
            <div className="divide-y">
              {data?.data.map((trace) => (
                <Link
                  key={trace.id}
                  href={`/traces/${trace.id}`}
                  className="flex items-center gap-4 p-4 hover:bg-muted/50 transition-colors"
                >
                  <div className="flex h-10 w-10 items-center justify-center rounded bg-muted">
                    <FileText className="h-5 w-5 text-muted-foreground" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-medium truncate">{trace.name}</p>
                    <p className="text-sm text-muted-foreground">
                      {trace.studyName}
                    </p>
                  </div>
                  <div className="hidden md:block text-sm text-muted-foreground">
                    {trace.fileType.toUpperCase()}
                  </div>
                  <div className="hidden md:block text-sm text-muted-foreground">
                    {formatBytes(trace.fileSize)}
                  </div>
                  <div className="hidden lg:block text-sm text-muted-foreground">
                    {trace.qualityScore ? `${trace.qualityScore}%` : "-"}
                  </div>
                  <div className="hidden lg:block text-sm text-muted-foreground">
                    {formatDate(trace.createdAt)}
                  </div>
                  <StatusBadge status={trace.status} />
                </Link>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Pagination info */}
      {data && data.total > 0 && (
        <p className="text-sm text-muted-foreground">
          Showing {data.data.length} of {data.total} traces
        </p>
      )}
    </div>
  );
}

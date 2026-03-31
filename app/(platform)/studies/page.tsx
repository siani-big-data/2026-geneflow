"use client";

import { useState } from "react";
import Link from "next/link";
import { PageHeader } from "@/components/layout";
import { Button, Card, CardContent, Input, Skeleton } from "@/components/ui";
import { StatusBadge, EmptyState } from "@/components/shared";
import { useStudies } from "@/hooks";
import { formatDate, formatNumber } from "@/lib/utils";
import { Plus, Search, Beaker, Users, Waves } from "lucide-react";

export default function StudiesPage() {
  const [search, setSearch] = useState("");
  const { data, isLoading } = useStudies({ search: search || undefined });

  return (
    <div className="space-y-6">
      <PageHeader
        title="Studies"
        description="Manage your genomic research projects"
      >
        <Button>
          <Plus className="h-4 w-4" />
          New Study
        </Button>
      </PageHeader>

      {/* Search */}
      <div className="relative max-w-md">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          placeholder="Search studies..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-10"
        />
      </div>

      {/* Studies Grid */}
      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <Card key={i}>
              <CardContent className="p-6">
                <Skeleton className="h-6 w-3/4" />
                <Skeleton className="mt-2 h-4 w-full" />
                <Skeleton className="mt-1 h-4 w-2/3" />
                <div className="mt-4 flex gap-4">
                  <Skeleton className="h-4 w-16" />
                  <Skeleton className="h-4 w-16" />
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : data?.data.length === 0 ? (
        <EmptyState
          icon={Beaker}
          title="No studies found"
          description={
            search
              ? "Try adjusting your search terms"
              : "Create your first study to get started"
          }
        >
          {!search && (
            <Button>
              <Plus className="h-4 w-4" />
              New Study
            </Button>
          )}
        </EmptyState>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {data?.data.map((study) => (
            <Link key={study.id} href={`/studies/${study.id}`}>
              <Card className="h-full transition-shadow hover:shadow-md">
                <CardContent className="p-6">
                  <div className="flex items-start justify-between">
                    <h3 className="font-semibold text-foreground line-clamp-1">
                      {study.name}
                    </h3>
                    <StatusBadge status={study.status} />
                  </div>
                  {study.description && (
                    <p className="mt-2 text-sm text-muted-foreground line-clamp-2">
                      {study.description}
                    </p>
                  )}
                  <div className="mt-4 flex items-center gap-4 text-sm text-muted-foreground">
                    <span className="flex items-center gap-1">
                      <Waves className="h-4 w-4" />
                      {formatNumber(study.tracesCount)} traces
                    </span>
                    <span className="flex items-center gap-1">
                      <Users className="h-4 w-4" />
                      {study.members.length}
                    </span>
                  </div>
                  <p className="mt-3 text-xs text-muted-foreground">
                    Updated {formatDate(study.updatedAt)}
                  </p>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}

      {/* Pagination info */}
      {data && data.total > 0 && (
        <p className="text-sm text-muted-foreground">
          Showing {data.data.length} of {data.total} studies
        </p>
      )}
    </div>
  );
}

"use client";

import { useTranslations } from "next-intl";
import { Badge } from "@/components/ui";
import { cn } from "@/lib/utils";

type StatusType =
  | "draft"
  | "active"
  | "completed"
  | "archived"
  | "pending"
  | "processing"
  | "failed"
  | "queued"
  | "running"
  | "cancelled";

interface StatusBadgeProps {
  status: string; // Accepts any case, will be normalized internally
  className?: string;
}

const statusVariants: Record<
  StatusType,
  "default" | "secondary" | "destructive" | "outline" | "success" | "warning" | "info"
> = {
  draft: "secondary",
  active: "success",
  completed: "default",
  archived: "outline",
  pending: "warning",
  processing: "info",
  failed: "destructive",
  queued: "secondary",
  running: "info",
  cancelled: "outline",
};

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const t = useTranslations("common");
  // Normalize status to lowercase for translation lookup
  const normalizedStatus = status.toLowerCase() as StatusType;
  const variant = statusVariants[normalizedStatus] || "secondary";

  return (
    <Badge variant={variant} className={cn(className)}>
      {t(`status.${normalizedStatus}`)}
    </Badge>
  );
}

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
  status: StatusType;
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
  const variant = statusVariants[status];

  return (
    <Badge variant={variant} className={cn(className)}>
      {t(`status.${status}`)}
    </Badge>
  );
}

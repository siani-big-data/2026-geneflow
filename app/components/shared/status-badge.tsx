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

const statusConfig: Record<
  StatusType,
  { label: string; variant: "default" | "secondary" | "destructive" | "outline" | "success" | "warning" | "info" }
> = {
  draft: { label: "Draft", variant: "secondary" },
  active: { label: "Active", variant: "success" },
  completed: { label: "Completed", variant: "default" },
  archived: { label: "Archived", variant: "outline" },
  pending: { label: "Pending", variant: "warning" },
  processing: { label: "Processing", variant: "info" },
  failed: { label: "Failed", variant: "destructive" },
  queued: { label: "Queued", variant: "secondary" },
  running: { label: "Running", variant: "info" },
  cancelled: { label: "Cancelled", variant: "outline" },
};

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const config = statusConfig[status];

  return (
    <Badge variant={config.variant} className={cn(className)}>
      {config.label}
    </Badge>
  );
}

import { PageHeader } from "@/components/layout";
import { Button } from "@/components/ui";
import { Plus } from "lucide-react";

export default function PipelinesPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Pipelines"
        description="Execute and monitor bioinformatics workflows"
      >
        <Button>
          <Plus className="h-4 w-4" />
          New Pipeline
        </Button>
      </PageHeader>
      <div className="rounded-lg border bg-card p-6 shadow-sm">
        <p className="text-sm text-muted-foreground">
          Pipelines list will be displayed here.
        </p>
      </div>
    </div>
  );
}

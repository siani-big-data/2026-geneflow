import { PageHeader } from "@/components/layout";
import { Button } from "@/components/ui";
import { Plus } from "lucide-react";

export default function StudiesPage() {
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
      <div className="rounded-lg border bg-card p-6 shadow-sm">
        <p className="text-sm text-muted-foreground">
          Studies list will be displayed here.
        </p>
      </div>
    </div>
  );
}

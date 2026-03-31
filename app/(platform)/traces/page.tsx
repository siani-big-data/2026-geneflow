import { PageHeader } from "@/components/layout";
import { Button } from "@/components/ui";
import { Upload } from "lucide-react";

export default function TracesPage() {
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
      <div className="rounded-lg border bg-card p-6 shadow-sm">
        <p className="text-sm text-muted-foreground">
          Traces list will be displayed here.
        </p>
      </div>
    </div>
  );
}

import { PageHeader } from "@/components/layout";

export default function HelpPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Help"
        description="Documentation and support resources"
      />
      <div className="rounded-lg border bg-card p-6 shadow-sm">
        <p className="text-sm text-muted-foreground">
          Help content will be displayed here.
        </p>
      </div>
    </div>
  );
}

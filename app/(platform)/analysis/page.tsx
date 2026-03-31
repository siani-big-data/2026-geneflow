import { PageHeader } from "@/components/layout";

export default function AnalysisPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Analysis"
        description="Charts, statistics, and data exploration"
      />
      <div className="rounded-lg border bg-card p-6 shadow-sm">
        <p className="text-sm text-muted-foreground">
          Analysis charts will be displayed here.
        </p>
      </div>
    </div>
  );
}

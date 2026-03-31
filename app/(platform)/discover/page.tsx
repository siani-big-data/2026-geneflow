import { PageHeader } from "@/components/layout";

export default function DiscoverPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Discover"
        description="Explore public datasets and research"
      />
      <div className="rounded-lg border bg-card p-6 shadow-sm">
        <p className="text-sm text-muted-foreground">
          Discover page content will be displayed here.
        </p>
      </div>
    </div>
  );
}

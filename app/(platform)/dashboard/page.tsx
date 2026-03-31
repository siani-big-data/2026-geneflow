import { PageHeader } from "@/components/layout";

export default function DashboardPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Dashboard"
        description="Welcome back, Dr. Sarah Martinez"
      />

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {/* Placeholder stats cards */}
        {[
          { label: "Active Studies", value: "12" },
          { label: "Total Traces", value: "1,234" },
          { label: "Running Pipelines", value: "3" },
          { label: "Completed Analyses", value: "89" },
        ].map((stat) => (
          <div
            key={stat.label}
            className="rounded-lg border bg-card p-6 shadow-sm"
          >
            <p className="text-sm font-medium text-muted-foreground">
              {stat.label}
            </p>
            <p className="mt-2 text-3xl font-semibold text-foreground">
              {stat.value}
            </p>
          </div>
        ))}
      </div>

      <div className="rounded-lg border bg-card p-6 shadow-sm">
        <h2 className="text-lg font-semibold">Recent Activity</h2>
        <p className="mt-2 text-sm text-muted-foreground">
          Activity feed will be displayed here.
        </p>
      </div>
    </div>
  );
}

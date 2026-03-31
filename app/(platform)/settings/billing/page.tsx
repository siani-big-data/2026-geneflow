import { PageHeader } from "@/components/layout";

export default function BillingPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Billing"
        description="Manage your subscription and payment methods"
      />
      <div className="rounded-lg border bg-card p-6 shadow-sm">
        <p className="text-sm text-muted-foreground">
          Billing information will be displayed here.
        </p>
      </div>
    </div>
  );
}

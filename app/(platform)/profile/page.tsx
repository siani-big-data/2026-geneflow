import { PageHeader } from "@/components/layout";

export default function ProfilePage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="My Profile"
        description="Manage your account and preferences"
      />
      <div className="rounded-lg border bg-card p-6 shadow-sm">
        <p className="text-sm text-muted-foreground">
          Profile settings will be displayed here.
        </p>
      </div>
    </div>
  );
}

import { PageHeader } from "@/components/layout";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui";

export default function SettingsPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Settings"
        description="Manage your account settings and preferences"
      />
      <Tabs defaultValue="general" className="space-y-4">
        <TabsList>
          <TabsTrigger value="general">General</TabsTrigger>
          <TabsTrigger value="notifications">Notifications</TabsTrigger>
          <TabsTrigger value="privacy">Privacy</TabsTrigger>
        </TabsList>
        <TabsContent value="general">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <p className="text-sm text-muted-foreground">
              General settings will be displayed here.
            </p>
          </div>
        </TabsContent>
        <TabsContent value="notifications">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <p className="text-sm text-muted-foreground">
              Notification preferences will be displayed here.
            </p>
          </div>
        </TabsContent>
        <TabsContent value="privacy">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <p className="text-sm text-muted-foreground">
              Privacy settings will be displayed here.
            </p>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}

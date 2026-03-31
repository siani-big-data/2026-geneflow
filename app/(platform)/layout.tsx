"use client";

import { Sidebar, Header } from "@/components/layout";
import { useUIStore } from "@/stores/ui-store";
import { cn } from "@/lib/utils";

export default function PlatformLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { sidebarCollapsed } = useUIStore();

  return (
    <div className="flex min-h-screen bg-background">
      <Sidebar />

      <div
        className={cn(
          "flex min-w-0 flex-1 flex-col transition-[margin] duration-300 ease-out will-change-[margin-left]",
          sidebarCollapsed ? "ml-20" : "ml-64"
        )}
      >
        <Header />
        <main className="flex-1 overflow-auto">
          <div className="px-16 py-10">{children}</div>
        </main>
      </div>
    </div>
  );
}

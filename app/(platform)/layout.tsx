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
    <div className="relative flex min-h-screen overflow-hidden bg-background">
      {/* Ambient background effect */}
      <div className="pointer-events-none absolute inset-0 z-0">
        <div
          className="absolute -left-20 -top-20 h-[600px] w-[600px]"
          style={{
            background:
              "radial-gradient(circle, rgba(13, 148, 136, 0.12) 0%, rgba(13, 148, 136, 0.06) 40%, transparent 70%)",
            filter: "blur(80px)",
          }}
        />
      </div>

      <Sidebar />

      <div
        className={cn(
          "flex min-w-0 flex-1 flex-col transition-all duration-300",
          sidebarCollapsed ? "ml-20" : "ml-64"
        )}
      >
        <Header />
        <main className="flex-1 overflow-auto">
          <div className="container mx-auto p-6">{children}</div>
        </main>
      </div>
    </div>
  );
}

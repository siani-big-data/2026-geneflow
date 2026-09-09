"use client";

import { useEffect, useState } from "react";
import { useRouter } from "@/lib/navigation";
import { useAuthStore } from "@/stores/auth-store";
import { Loader2 } from "lucide-react";

interface GuestGuardProps {
  children: React.ReactNode;
}

/**
 * GuestGuard component that protects auth routes (login, register).
 *
 * - Shows loading spinner only during initial auth check
 * - Redirects to dashboard if already authenticated
 * - Renders children if not authenticated (guest)
 *
 * IMPORTANT: Does NOT unmount children during login/register operations
 * to preserve component state (like registrationSuccess).
 */
export function GuestGuard({ children }: GuestGuardProps) {
  const router = useRouter();
  const { isAuthenticated, isLoading, initialize } = useAuthStore();
  const [initialized, setInitialized] = useState(false);

  // Initialize auth state on mount
  useEffect(() => {
    initialize().then(() => setInitialized(true));
  }, [initialize]);

  // Redirect to dashboard if already authenticated
  useEffect(() => {
    if (initialized && isAuthenticated) {
      router.push("/dashboard");
    }
  }, [initialized, isAuthenticated, router]);

  // Show loading state only during initial auth check
  if (!initialized) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <Loader2 className="h-8 w-8 animate-spin text-teal" />
          <p className="text-sm text-muted-foreground">Loading...</p>
        </div>
      </div>
    );
  }

  // Don't render children if authenticated (redirect will happen)
  if (isAuthenticated) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <Loader2 className="h-8 w-8 animate-spin text-teal" />
          <p className="text-sm text-muted-foreground">Redirecting...</p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}

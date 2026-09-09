"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "@/lib/navigation";
import { useAuthStore } from "@/stores/auth-store";
import { Loader2 } from "lucide-react";

interface AuthGuardProps {
  children: React.ReactNode;
}

/**
 * AuthGuard component that protects routes requiring authentication.
 *
 * - Shows loading spinner while checking auth state
 * - Redirects to login if not authenticated
 * - Redirects to /complete-profile if user doesn't have a profile
 * - Renders children if authenticated and has profile
 */
export function AuthGuard({ children }: AuthGuardProps) {
  const router = useRouter();
  const pathname = usePathname();
  const {
    isAuthenticated,
    isLoading,
    initialize,
    profile,
    profileChecked,
    checkProfile,
  } = useAuthStore();

  // Initialize auth state on mount
  useEffect(() => {
    initialize();
  }, [initialize]);

  // Check if user has a profile after authentication
  useEffect(() => {
    const doCheckProfile = async () => {
      if (!isLoading && isAuthenticated && !profileChecked) {
        // Skip API call if on complete-profile page (user is creating profile)
        if (pathname === "/complete-profile") {
          return;
        }
        await checkProfile();
      }
    };

    doCheckProfile();
  }, [isLoading, isAuthenticated, profileChecked, pathname, checkProfile]);

  // Redirect to login if not authenticated (after loading completes)
  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      // Store the intended destination for redirect after login
      const returnUrl = encodeURIComponent(pathname);
      router.push(`/login?returnUrl=${returnUrl}`);
    }
  }, [isLoading, isAuthenticated, pathname, router]);

  // Redirect to complete-profile if no profile (only after profile check is done)
  useEffect(() => {
    if (
      isAuthenticated &&
      profileChecked &&
      profile === null &&
      pathname !== "/complete-profile"
    ) {
      router.push("/complete-profile");
    }
  }, [isAuthenticated, profileChecked, profile, pathname, router]);

  // Show loading state while checking authentication
  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-4">
          <Loader2 className="h-8 w-8 animate-spin text-teal" />
          <p className="text-sm text-muted-foreground">Loading...</p>
        </div>
      </div>
    );
  }

  // Show loading while checking profile (only if not already checked)
  if (isAuthenticated && !profileChecked && pathname !== "/complete-profile") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-4">
          <Loader2 className="h-8 w-8 animate-spin text-teal" />
          <p className="text-sm text-muted-foreground">Checking profile...</p>
        </div>
      </div>
    );
  }

  // Don't render children if not authenticated (redirect will happen)
  if (!isAuthenticated) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-4">
          <Loader2 className="h-8 w-8 animate-spin text-teal" />
          <p className="text-sm text-muted-foreground">Redirecting to login...</p>
        </div>
      </div>
    );
  }

  // Don't render children if no profile and not on complete-profile page
  if (profile === null && profileChecked && pathname !== "/complete-profile") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-4">
          <Loader2 className="h-8 w-8 animate-spin text-teal" />
          <p className="text-sm text-muted-foreground">Completing setup...</p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}

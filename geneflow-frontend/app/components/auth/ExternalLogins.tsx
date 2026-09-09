"use client";

import { useState, useEffect, useCallback } from "react";
import { Loader2, Link2, Unlink, AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { authService } from "@/services";
import { signInWithGoogle, signInWithGitHub } from "@/lib/oauth";
import { cn } from "@/lib/utils";
import type { ExternalLogin } from "@/types";

const PROVIDERS = [
  {
    id: "google",
    name: "Google",
    icon: (
      <svg className="h-5 w-5" viewBox="0 0 24 24">
        <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
        <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
        <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
        <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
      </svg>
    ),
  },
  {
    id: "github",
    name: "GitHub",
    icon: (
      <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 24 24">
        <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
      </svg>
    ),
  },
];

export function ExternalLogins() {
  const [linkedLogins, setLinkedLogins] = useState<ExternalLogin[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [unlinkDialog, setUnlinkDialog] = useState<{ open: boolean; provider: string | null }>({
    open: false,
    provider: null,
  });

  const fetchLinkedLogins = useCallback(async () => {
    try {
      const logins = await authService.getExternalLogins();
      setLinkedLogins(logins);
    } catch (err) {
      console.error("Failed to fetch external logins:", err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchLinkedLogins();
  }, [fetchLinkedLogins]);

  const isLinked = (providerId: string) => {
    return linkedLogins.some((login) => login.provider.toLowerCase() === providerId.toLowerCase());
  };

  const getLinkedLogin = (providerId: string) => {
    return linkedLogins.find((login) => login.provider.toLowerCase() === providerId.toLowerCase());
  };

  const handleLink = async (providerId: string) => {
    setActionLoading(providerId);
    setError(null);

    try {
      let token: string;

      if (providerId === "google") {
        token = await signInWithGoogle();
      } else if (providerId === "github") {
        token = await signInWithGitHub();
      } else {
        throw new Error("Unsupported provider");
      }

      await authService.linkExternalLogin({ provider: providerId, token });
      await fetchLinkedLogins();
    } catch (err) {
      if (err instanceof Error) {
        if (!err.message.includes("cancelled")) {
          setError(err.message);
        }
      }
    } finally {
      setActionLoading(null);
    }
  };

  const handleUnlink = async () => {
    const provider = unlinkDialog.provider;
    if (!provider) return;

    setActionLoading(provider);
    setError(null);
    setUnlinkDialog({ open: false, provider: null });

    try {
      await authService.unlinkExternalLogin(provider);
      await fetchLinkedLogins();
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message);
      }
    } finally {
      setActionLoading(null);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString(undefined, {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  };

  return (
    <>
      <div className="rounded-xl border border-border bg-card p-6">
        <h2 className="mb-2 text-lg font-semibold text-foreground">Connected Accounts</h2>
        <p className="mb-5 text-sm text-muted-foreground">
          Link your accounts to enable quick sign-in with Google or GitHub.
        </p>

        {error && (
          <div className="mb-4 flex items-center gap-2 rounded-lg border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-600 dark:text-red-400">
            <AlertTriangle className="h-4 w-4 flex-shrink-0" />
            {error}
          </div>
        )}

        {isLoading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : (
          <div className="space-y-3">
            {PROVIDERS.map((provider) => {
              const linked = isLinked(provider.id);
              const linkedLogin = getLinkedLogin(provider.id);
              const isProcessing = actionLoading === provider.id;

              return (
                <div
                  key={provider.id}
                  className={cn(
                    "flex items-center justify-between rounded-lg border p-4 transition-colors",
                    linked
                      ? "border-emerald-500/30 bg-emerald-500/5"
                      : "border-border bg-background"
                  )}
                >
                  <div className="flex items-center gap-3">
                    <div className={cn(
                      "flex h-10 w-10 items-center justify-center rounded-lg border",
                      linked ? "border-emerald-500/30 bg-emerald-500/10" : "border-border bg-muted/50"
                    )}>
                      {provider.icon}
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium text-foreground">{provider.name}</span>
                        {linked && (
                          <span className="rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600 dark:text-emerald-400">
                            Connected
                          </span>
                        )}
                      </div>
                      {linked && linkedLogin?.linkedAt && (
                        <p className="text-xs text-muted-foreground">
                          Linked on {formatDate(linkedLogin.linkedAt)}
                          {linkedLogin.displayName && ` as ${linkedLogin.displayName}`}
                        </p>
                      )}
                      {!linked && (
                        <p className="text-xs text-muted-foreground">
                          Not connected
                        </p>
                      )}
                    </div>
                  </div>

                  {linked ? (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setUnlinkDialog({ open: true, provider: provider.id })}
                      disabled={isProcessing}
                      className="text-red-500 hover:bg-red-500/10 hover:text-red-500"
                    >
                      {isProcessing ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <>
                          <Unlink className="h-4 w-4" />
                          Disconnect
                        </>
                      )}
                    </Button>
                  ) : (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleLink(provider.id)}
                      disabled={isProcessing}
                    >
                      {isProcessing ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <>
                          <Link2 className="h-4 w-4" />
                          Connect
                        </>
                      )}
                    </Button>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Unlink Confirmation Dialog */}
      <Dialog open={unlinkDialog.open} onOpenChange={(open) => setUnlinkDialog({ open, provider: null })}>
        <DialogContent className="sm:max-w-[400px]">
          <DialogHeader>
            <DialogTitle>Disconnect Account</DialogTitle>
            <DialogDescription>
              Are you sure you want to disconnect your {unlinkDialog.provider?.charAt(0).toUpperCase()}{unlinkDialog.provider?.slice(1)} account? You won&apos;t be able to sign in with it anymore.
            </DialogDescription>
          </DialogHeader>

          <div className="py-4">
            <div className="flex items-start gap-3 rounded-lg border border-amber-500/30 bg-amber-500/10 p-4">
              <AlertTriangle className="h-5 w-5 flex-shrink-0 text-amber-500" />
              <p className="text-sm text-amber-700 dark:text-amber-400">
                If this is your only sign-in method and you don&apos;t have a password set, you may lose access to your account.
              </p>
            </div>
          </div>

          <DialogFooter>
            <Button variant="ghost" onClick={() => setUnlinkDialog({ open: false, provider: null })}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleUnlink}>
              Disconnect
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

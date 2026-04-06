"use client";

import { useState, useEffect } from "react";
import { Loader2, Smartphone, Shield, Copy, Check, AlertTriangle } from "lucide-react";
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
import { cn } from "@/lib/utils";
import type { TwoFactorSetupResponse } from "@/types";

interface TwoFactorSetupProps {
  isEnabled: boolean;
  onEnableChange: (enabled: boolean) => void;
}

export function TwoFactorSetup({ isEnabled, onEnableChange }: TwoFactorSetupProps) {
  const [setupOpen, setSetupOpen] = useState(false);
  const [disableOpen, setDisableOpen] = useState(false);
  const [step, setStep] = useState<"qr" | "verify">("qr");
  const [setupData, setSetupData] = useState<TwoFactorSetupResponse | null>(null);
  const [code, setCode] = useState("");
  const [disableCode, setDisableCode] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [secretCopied, setSecretCopied] = useState(false);

  // Reset state when dialog opens/closes
  useEffect(() => {
    if (!setupOpen) {
      setStep("qr");
      setSetupData(null);
      setCode("");
      setError(null);
      setSecretCopied(false);
    }
  }, [setupOpen]);

  useEffect(() => {
    if (!disableOpen) {
      setDisableCode("");
      setError(null);
    }
  }, [disableOpen]);

  const handleStartSetup = async () => {
    setSetupOpen(true);
    setIsLoading(true);
    setError(null);

    try {
      const data = await authService.getTwoFactorSetup();
      setSetupData(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to start 2FA setup");
    } finally {
      setIsLoading(false);
    }
  };

  const handleCopySecret = async () => {
    if (setupData?.secret) {
      await navigator.clipboard.writeText(setupData.secret);
      setSecretCopied(true);
      setTimeout(() => setSecretCopied(false), 2000);
    }
  };

  const handleVerify = async () => {
    if (!setupData || code.length !== 6) return;

    setIsLoading(true);
    setError(null);

    try {
      await authService.confirmTwoFactorSetup({
        secret: setupData.secret,
        code,
      });
      onEnableChange(true);
      setSetupOpen(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Invalid verification code");
    } finally {
      setIsLoading(false);
    }
  };

  const handleDisable = async () => {
    if (disableCode.length !== 6) return;

    setIsLoading(true);
    setError(null);

    try {
      await authService.disableTwoFactor(disableCode);
      onEnableChange(false);
      setDisableOpen(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Invalid verification code");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <>
      <div className="rounded-xl border border-border bg-card p-6">
        <h2 className="mb-5 text-lg font-semibold text-foreground">Two-Factor Authentication</h2>
        <div className="mb-6 flex items-start justify-between">
          <div className="flex-1">
            <div className="mb-2 flex items-center gap-2">
              <h3 className="text-sm font-medium text-foreground">Authenticator App</h3>
              {isEnabled && (
                <span className="rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-500">
                  Active
                </span>
              )}
            </div>
            <p className="text-sm text-muted-foreground">
              Use an authenticator app like Google Authenticator or Authy to generate verification codes.
            </p>
          </div>
          {isEnabled ? (
            <Button variant="outline" size="sm" onClick={() => setDisableOpen(true)}>
              Disable
            </Button>
          ) : (
            <Button size="sm" onClick={handleStartSetup}>
              Enable
            </Button>
          )}
        </div>
        {isEnabled && (
          <div className="rounded-lg border border-border bg-muted/30 p-4">
            <div className="flex items-start gap-3">
              <Smartphone className="mt-0.5 h-5 w-5 text-teal" />
              <div className="flex-1">
                <p className="mb-1 text-sm font-medium text-foreground">Authenticator Connected</p>
                <p className="text-xs text-muted-foreground">
                  Your account is protected with two-factor authentication via an authenticator app.
                </p>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Setup Dialog */}
      <Dialog open={setupOpen} onOpenChange={setSetupOpen}>
        <DialogContent className="sm:max-w-[480px]">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Shield className="h-5 w-5 text-teal" />
              Set Up Two-Factor Authentication
            </DialogTitle>
            <DialogDescription>
              {step === "qr"
                ? "Scan the QR code with your authenticator app to get started."
                : "Enter the 6-digit code from your authenticator app to verify setup."}
            </DialogDescription>
          </DialogHeader>

          {error && (
            <div className="flex items-center gap-2 rounded-lg border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-600 dark:text-red-400">
              <AlertTriangle className="h-4 w-4 flex-shrink-0" />
              {error}
            </div>
          )}

          {step === "qr" && (
            <div className="space-y-6 py-4">
              {isLoading ? (
                <div className="flex items-center justify-center py-12">
                  <Loader2 className="h-8 w-8 animate-spin text-teal" />
                </div>
              ) : setupData ? (
                <>
                  {/* QR Code */}
                  <div className="flex justify-center">
                    <div className="rounded-lg border border-border bg-white p-4">
                      {/* Using a QR code image from the URI - in production you'd use a QR library */}
                      <img
                        src={`https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(setupData.qrCodeUri)}`}
                        alt="2FA QR Code"
                        className="h-48 w-48"
                      />
                    </div>
                  </div>

                  {/* Manual entry option */}
                  <div className="space-y-2">
                    <p className="text-center text-xs text-muted-foreground">
                      Can&apos;t scan? Enter this code manually:
                    </p>
                    <div className="flex items-center gap-2">
                      <code className="flex-1 rounded-lg border border-border bg-muted/50 px-3 py-2 text-center font-mono text-sm">
                        {setupData.secret}
                      </code>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={handleCopySecret}
                        className="flex-shrink-0"
                      >
                        {secretCopied ? (
                          <Check className="h-4 w-4 text-emerald-500" />
                        ) : (
                          <Copy className="h-4 w-4" />
                        )}
                      </Button>
                    </div>
                  </div>

                  <div className="rounded-lg border border-border bg-muted/30 p-4">
                    <p className="text-sm text-muted-foreground">
                      <strong className="text-foreground">Supported apps:</strong> Google Authenticator, Microsoft Authenticator, Authy, 1Password, and any other TOTP-compatible app.
                    </p>
                  </div>
                </>
              ) : null}
            </div>
          )}

          {step === "verify" && (
            <div className="space-y-4 py-4">
              <div className="space-y-2">
                <label className="text-sm font-medium text-foreground">
                  Verification Code
                </label>
                <input
                  type="text"
                  value={code}
                  onChange={(e) => setCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
                  placeholder="000000"
                  maxLength={6}
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  className={cn(
                    "w-full h-12 px-4 text-center text-xl font-semibold tracking-[0.5em] rounded-lg border bg-background text-foreground transition-all",
                    "focus:outline-none focus:ring-2 focus:ring-teal/20 focus:border-teal",
                    "border-border"
                  )}
                />
              </div>
              <p className="text-center text-xs text-muted-foreground">
                Enter the 6-digit code displayed in your authenticator app.
              </p>
            </div>
          )}

          <DialogFooter>
            <Button variant="ghost" onClick={() => setSetupOpen(false)}>
              Cancel
            </Button>
            {step === "qr" ? (
              <Button
                onClick={() => setStep("verify")}
                disabled={!setupData || isLoading}
              >
                Continue
              </Button>
            ) : (
              <Button
                onClick={handleVerify}
                disabled={code.length !== 6 || isLoading}
              >
                {isLoading ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Verifying...
                  </>
                ) : (
                  "Verify & Enable"
                )}
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Disable Dialog */}
      <Dialog open={disableOpen} onOpenChange={setDisableOpen}>
        <DialogContent className="sm:max-w-[400px]">
          <DialogHeader>
            <DialogTitle>Disable Two-Factor Authentication</DialogTitle>
            <DialogDescription>
              Enter your authenticator code to disable two-factor authentication. This will make your account less secure.
            </DialogDescription>
          </DialogHeader>

          {error && (
            <div className="flex items-center gap-2 rounded-lg border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-600 dark:text-red-400">
              <AlertTriangle className="h-4 w-4 flex-shrink-0" />
              {error}
            </div>
          )}

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">
                Verification Code
              </label>
              <input
                type="text"
                value={disableCode}
                onChange={(e) => setDisableCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
                placeholder="000000"
                maxLength={6}
                inputMode="numeric"
                autoComplete="one-time-code"
                className={cn(
                  "w-full h-12 px-4 text-center text-xl font-semibold tracking-[0.5em] rounded-lg border bg-background text-foreground transition-all",
                  "focus:outline-none focus:ring-2 focus:ring-teal/20 focus:border-teal",
                  "border-border"
                )}
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="ghost" onClick={() => setDisableOpen(false)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDisable}
              disabled={disableCode.length !== 6 || isLoading}
            >
              {isLoading ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Disabling...
                </>
              ) : (
                "Disable 2FA"
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

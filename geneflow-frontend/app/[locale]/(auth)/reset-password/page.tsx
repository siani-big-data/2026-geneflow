"use client";

import { useState, useEffect } from "react";
import { useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import {
  AlertCircle,
  Loader2,
  CheckCircle2,
  Eye,
  EyeOff,
  ArrowLeft,
  KeyRound,
} from "lucide-react";
import { Button } from "@/components/ui";
import { cn } from "@/lib/utils";
import { authService } from "@/services";

export default function ResetPasswordPage() {
  const t = useTranslations("auth");
  const searchParams = useSearchParams();
  const token = searchParams.get("token");

  // Form state
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  // UI state
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [success, setSuccess] = useState(false);

  // Password validation
  const hasUppercase = /[A-Z]/.test(password);
  const hasLowercase = /[a-z]/.test(password);
  const hasNumber = /[0-9]/.test(password);
  const hasMinLength = password.length >= 8;
  const passwordsMatch = password === confirmPassword && password.length > 0;
  const isPasswordValid = hasUppercase && hasLowercase && hasNumber && hasMinLength;

  // Check for missing token
  useEffect(() => {
    if (!token) {
      setError(t("resetPassword.invalidToken"));
    }
  }, [token, t]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitted(true);
    setError("");

    if (!token) {
      setError(t("resetPassword.invalidToken"));
      return;
    }

    if (!isPasswordValid || !passwordsMatch) {
      return;
    }

    setIsLoading(true);

    try {
      await authService.resetPassword({ token, newPassword: password });
      setSuccess(true);
    } catch (err) {
      if (err instanceof Error) {
        // Check for specific error messages
        if (err.message.includes("expired") || err.message.includes("invalid")) {
          setError(t("resetPassword.tokenExpired"));
        } else {
          setError(err.message);
        }
      } else {
        setError(t("resetPassword.error"));
      }
    } finally {
      setIsLoading(false);
    }
  };

  // Success state
  if (success) {
    return (
      <div className="text-center">
        <div className="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-emerald-500/10">
          <CheckCircle2 className="h-8 w-8 text-emerald-500" />
        </div>

        <h1 className="mb-2 text-2xl font-bold text-slate-900 dark:text-slate-100 md:text-3xl">
          {t("resetPassword.successTitle")}
        </h1>
        <p className="mb-8 leading-relaxed text-slate-500 dark:text-slate-400">
          {t("resetPassword.successDescription")}
        </p>

        <Link href="/login">
          <Button className="h-12 w-full">{t("backToLogin")}</Button>
        </Link>
      </div>
    );
  }

  // No token state
  if (!token) {
    return (
      <div className="text-center">
        <div className="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-red-500/10">
          <AlertCircle className="h-8 w-8 text-red-500" />
        </div>

        <h1 className="mb-2 text-2xl font-bold text-slate-900 dark:text-slate-100 md:text-3xl">
          {t("resetPassword.invalidLinkTitle")}
        </h1>
        <p className="mb-8 leading-relaxed text-slate-500 dark:text-slate-400">
          {t("resetPassword.invalidLinkDescription")}
        </p>

        <Link href="/forgot-password">
          <Button className="h-12 w-full">{t("resetPassword.requestNewLink")}</Button>
        </Link>

        <Link
          href="/login"
          className="mt-6 flex items-center justify-center gap-2 text-sm text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
        >
          <ArrowLeft className="h-4 w-4" />
          {t("backToLogin")}
        </Link>
      </div>
    );
  }

  return (
    <>
      <div className="mb-8 text-center">
        <div className="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-teal/10">
          <KeyRound className="h-8 w-8 text-teal" />
        </div>
        <h1 className="mb-2 text-2xl font-bold text-slate-900 dark:text-slate-100 md:text-3xl">
          {t("resetPassword.title")}
        </h1>
        <p className="text-slate-500 dark:text-slate-400">
          {t("resetPassword.description")}
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-5">
        {/* Error message */}
        {error && (
          <div className="flex items-center gap-3 rounded-lg border border-red-500/20 bg-red-500/10 p-4 text-sm text-red-600 dark:text-red-400">
            <AlertCircle className="h-5 w-5 flex-shrink-0" />
            {error}
          </div>
        )}

        {/* New Password field */}
        <div className="space-y-2">
          <label
            htmlFor="password"
            className="block text-sm font-medium text-slate-700 dark:text-slate-200"
          >
            {t("resetPassword.newPassword")}
          </label>
          <div className="relative">
            <input
              type={showPassword ? "text" : "password"}
              id="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder={t("resetPassword.newPasswordPlaceholder")}
              autoComplete="new-password"
              className={cn(
                "h-11 w-full rounded-lg border bg-white px-4 pr-12 text-slate-900 transition-all placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-teal/20 dark:bg-slate-700 dark:text-slate-100 dark:placeholder:text-slate-500",
                submitted && !isPasswordValid
                  ? "border-red-500 focus:border-red-500 focus:ring-red-500/20"
                  : "border-slate-300 focus:border-teal dark:border-slate-600"
              )}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300"
            >
              {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
            </button>
          </div>

          {/* Password requirements */}
          {password.length > 0 && (
            <div className="mt-3 space-y-2">
              <p className="text-xs font-medium text-slate-500 dark:text-slate-400">
                {t("validation.passwordRequirements")}
              </p>
              <div className="grid grid-cols-2 gap-2">
                <div
                  className={cn(
                    "flex items-center gap-2 text-xs",
                    hasMinLength ? "text-emerald-500" : "text-slate-400"
                  )}
                >
                  <div
                    className={cn(
                      "h-1.5 w-1.5 rounded-full",
                      hasMinLength ? "bg-emerald-500" : "bg-slate-300 dark:bg-slate-600"
                    )}
                  />
                  {t("validation.minLength")}
                </div>
                <div
                  className={cn(
                    "flex items-center gap-2 text-xs",
                    hasUppercase ? "text-emerald-500" : "text-slate-400"
                  )}
                >
                  <div
                    className={cn(
                      "h-1.5 w-1.5 rounded-full",
                      hasUppercase ? "bg-emerald-500" : "bg-slate-300 dark:bg-slate-600"
                    )}
                  />
                  {t("validation.uppercase")}
                </div>
                <div
                  className={cn(
                    "flex items-center gap-2 text-xs",
                    hasLowercase ? "text-emerald-500" : "text-slate-400"
                  )}
                >
                  <div
                    className={cn(
                      "h-1.5 w-1.5 rounded-full",
                      hasLowercase ? "bg-emerald-500" : "bg-slate-300 dark:bg-slate-600"
                    )}
                  />
                  {t("validation.lowercase")}
                </div>
                <div
                  className={cn(
                    "flex items-center gap-2 text-xs",
                    hasNumber ? "text-emerald-500" : "text-slate-400"
                  )}
                >
                  <div
                    className={cn(
                      "h-1.5 w-1.5 rounded-full",
                      hasNumber ? "bg-emerald-500" : "bg-slate-300 dark:bg-slate-600"
                    )}
                  />
                  {t("validation.number")}
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Confirm Password field */}
        <div className="space-y-2">
          <label
            htmlFor="confirmPassword"
            className="block text-sm font-medium text-slate-700 dark:text-slate-200"
          >
            {t("resetPassword.confirmPassword")}
          </label>
          <div className="relative">
            <input
              type={showConfirmPassword ? "text" : "password"}
              id="confirmPassword"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder={t("resetPassword.confirmPasswordPlaceholder")}
              autoComplete="new-password"
              className={cn(
                "h-11 w-full rounded-lg border bg-white px-4 pr-12 text-slate-900 transition-all placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-teal/20 dark:bg-slate-700 dark:text-slate-100 dark:placeholder:text-slate-500",
                submitted && !passwordsMatch
                  ? "border-red-500 focus:border-red-500 focus:ring-red-500/20"
                  : "border-slate-300 focus:border-teal dark:border-slate-600"
              )}
            />
            <button
              type="button"
              onClick={() => setShowConfirmPassword(!showConfirmPassword)}
              className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300"
            >
              {showConfirmPassword ? (
                <EyeOff className="h-5 w-5" />
              ) : (
                <Eye className="h-5 w-5" />
              )}
            </button>
          </div>
          {submitted && confirmPassword.length > 0 && !passwordsMatch && (
            <p className="text-xs text-red-500">{t("validation.passwordsMustMatch")}</p>
          )}
        </div>

        {/* Submit button */}
        <Button
          type="submit"
          className="h-12 w-full"
          disabled={isLoading || (submitted && (!isPasswordValid || !passwordsMatch))}
        >
          {isLoading ? (
            <>
              <Loader2 className="h-5 w-5 animate-spin" />
              {t("resetPassword.resetting")}
            </>
          ) : (
            t("resetPassword.resetButton")
          )}
        </Button>
      </form>

      {/* Footer */}
      <Link
        href="/login"
        className="mt-8 flex items-center justify-center gap-2 text-sm text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
      >
        <ArrowLeft className="h-4 w-4" />
        {t("backToLogin")}
      </Link>
    </>
  );
}

"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import { AlertCircle, Loader2, Mail, ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui";
import { cn } from "@/lib/utils";
import { authService } from "@/services";

export default function ForgotPasswordPage() {
  const t = useTranslations("auth");
  // Form state
  const [email, setEmail] = useState("");

  // UI state
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [emailSent, setEmailSent] = useState(false);

  // Validation
  const emailValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitted(true);
    setError("");

    if (!emailValid) {
      return;
    }

    setIsLoading(true);

    try {
      await authService.requestPasswordReset({ email });
      setEmailSent(true);
    } catch (err) {
      // Always show success to prevent email enumeration
      // Backend also returns success even if email doesn't exist
      setEmailSent(true);
    } finally {
      setIsLoading(false);
    }
  };

  if (emailSent) {
    return (
      <div className="text-center">
        {/* Success Icon */}
        <div className="flex items-center justify-center w-16 h-16 mx-auto mb-6 rounded-full bg-blue-500/10">
          <Mail className="h-8 w-8 text-blue-500" />
        </div>

        <h1 className="text-2xl md:text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          {t("codeSentSuccess")}
        </h1>
        <p className="text-slate-500 dark:text-slate-400 mb-8 leading-relaxed">
          {t("resetPasswordDescription")}
        </p>

        <Link href="/login">
          <Button className="w-full h-12">
            {t("backToLogin")}
          </Button>
        </Link>

        <p className="mt-6 text-sm text-slate-500 dark:text-slate-400">
          {t("resendCode")}?{" "}
          <button
            onClick={() => {
              setEmailSent(false);
              setSubmitted(false);
            }}
            className="text-teal hover:underline"
          >
            {t("resendCode")}
          </button>
        </p>
      </div>
    );
  }

  return (
    <>
      <div className="text-center mb-8">
        <h1 className="text-2xl md:text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          {t("resetPassword")}
        </h1>
        <p className="text-slate-500 dark:text-slate-400">
          {t("resetPasswordDescription")}
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-5">
        {/* Error message */}
        {error && (
          <div className="flex items-center gap-3 p-4 rounded-lg bg-red-500/10 border border-red-500/20 text-red-600 dark:text-red-400 text-sm">
            <AlertCircle className="h-5 w-5 flex-shrink-0" />
            {error}
          </div>
        )}

        {/* Email field */}
        <div className="space-y-2">
          <label htmlFor="email" className="block text-sm font-medium text-slate-700 dark:text-slate-200">
            {t("email")}
          </label>
          <input
            type="email"
            id="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder={t("emailPlaceholder")}
            autoComplete="email"
            className={cn(
              "w-full h-11 px-4 rounded-lg border bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 dark:placeholder:text-slate-500 transition-all",
              "focus:outline-none focus:ring-2 focus:ring-teal/20 focus:border-teal",
              submitted && !emailValid
                ? "border-red-500 focus:ring-red-500/20"
                : "border-slate-300 dark:border-slate-600"
            )}
          />
          {submitted && !emailValid && (
            <p className="text-xs text-red-500">{t("validation.emailRequired")}</p>
          )}
        </div>

        {/* Submit button */}
        <Button type="submit" className="w-full h-12" disabled={isLoading}>
          {isLoading ? (
            <>
              <Loader2 className="h-5 w-5 animate-spin" />
              {t("sendingResetLink")}
            </>
          ) : (
            t("sendResetLink")
          )}
        </Button>
      </form>

      {/* Footer */}
      <Link
        href="/login"
        className="mt-8 flex items-center justify-center gap-2 text-sm text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200"
      >
        <ArrowLeft className="h-4 w-4" />
        {t("backToLogin")}
      </Link>
    </>
  );
}

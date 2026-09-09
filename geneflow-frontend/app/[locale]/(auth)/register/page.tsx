"use client";

import { useState, useEffect, useCallback } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import { useRouter } from "@/lib/navigation";
import { Eye, EyeOff, Loader2, Mail, Check, X, CheckCircle2, RefreshCw, ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui";
import { cn } from "@/lib/utils";
import { useAuthStore } from "@/stores/auth-store";
import { authService } from "@/services";
import { ApiClientError } from "@/lib/api-client";
import { ErrorAlert } from "@/components/auth/ErrorAlert";
import { getErrorInfo, extractErrorCode, type ErrorInfo } from "@/lib/error-messages";
import { signInWithGoogle, signInWithGitHub } from "@/lib/oauth";
import {
  USERNAME_RULES,
  EMAIL_RULES,
  PASSWORD_RULES,
  validatePassword,
} from "@/lib/validation";

export default function RegisterPage() {
  const t = useTranslations("auth");
  const tCommon = useTranslations("common");
  const router = useRouter();

  const { register, oAuthLogin, isLoading: authLoading, isAuthenticated } = useAuthStore();

  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [acceptTerms, setAcceptTerms] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<ErrorInfo | null>(null);
  const [submitted, setSubmitted] = useState(false);
  const [registrationSuccess, setRegistrationSuccess] = useState(false);

  useEffect(() => {
    if (isAuthenticated) {
      router.push("/dashboard");
    }
  }, [isAuthenticated, router]);

  // Clear errors when inputs change
  useEffect(() => {
    if (error) {
      setError(null);
    }
  }, [username, email, password, confirmPassword]);

  const usernameValid =
    username.length >= USERNAME_RULES.MIN_LENGTH &&
    username.length <= USERNAME_RULES.MAX_LENGTH &&
    USERNAME_RULES.PATTERN.test(username);
  const emailValid =
    email.length <= EMAIL_RULES.MAX_LENGTH && EMAIL_RULES.PATTERN.test(email);
  const passwordsMatch = password === confirmPassword && confirmPassword.length > 0;

  const passwordValidation = validatePassword(password);
  const passwordRequirements = [
    { label: `At least ${PASSWORD_RULES.MIN_LENGTH} characters`, valid: passwordValidation.requirements.minLength },
    { label: "Contains uppercase letter", valid: passwordValidation.requirements.hasUppercase },
    { label: "Contains lowercase letter", valid: passwordValidation.requirements.hasLowercase },
    { label: "Contains a number", valid: passwordValidation.requirements.hasNumber },
  ];

  const passwordValid = passwordValidation.valid;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitted(true);
    setError(null);

    if (!usernameValid || !emailValid || !passwordValid || !passwordsMatch || !acceptTerms) {
      return;
    }

    setIsLoading(true);

    try {
      console.log("[Register] Starting registration...");
      const result = await register({ email, username, password });
      console.log("[Register] Registration successful:", result);
      setRegistrationSuccess(true);
      console.log("[Register] registrationSuccess set to true");
    } catch (err) {
      console.error("[Register] Registration failed:", err);
      if (err instanceof ApiClientError) {
        const errorInfo = getErrorInfo(err.code, err.message);
        setError(errorInfo);
      } else if (err instanceof Error) {
        const code = extractErrorCode(err);
        setError(getErrorInfo(code, err.message));
      } else {
        setError(getErrorInfo("UNKNOWN_ERROR", "Registration failed"));
      }
    } finally {
      setIsLoading(false);
    }
  };

  const [resendState, setResendState] = useState<"idle" | "sending" | "sent">("idle");

  const handleResendVerification = useCallback(async () => {
    if (resendState === "sending" || resendState === "sent") return;

    setResendState("sending");
    try {
      await authService.resendVerificationEmail(email);
      setResendState("sent");
      // Reset to idle after 5 seconds so user can resend again if needed
      setTimeout(() => setResendState("idle"), 5000);
    } catch {
      setResendState("idle");
    }
  }, [email, resendState]);

  const handleBackToRegister = useCallback(() => {
    setRegistrationSuccess(false);
    setEmail("");
    setPassword("");
    setConfirmPassword("");
    setUsername("");
    setAcceptTerms(false);
    setSubmitted(false);
  }, []);

  const handleGoogleSignIn = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const token = await signInWithGoogle();
      await oAuthLogin("google", token);
      router.push("/dashboard");
    } catch (err) {
      if (err instanceof ApiClientError) {
        setError(getErrorInfo(err.code, err.message));
      } else if (err instanceof Error) {
        if (err.message.includes("not configured")) {
          setError(getErrorInfo("UNKNOWN_ERROR", "Google sign-in is not available at this time."));
        } else if (!err.message.includes("cancelled")) {
          setError(getErrorInfo("UNKNOWN_ERROR", err.message));
        }
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleGitHubSignIn = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const token = await signInWithGitHub();
      await oAuthLogin("github", token);
      router.push("/dashboard");
    } catch (err) {
      if (err instanceof ApiClientError) {
        setError(getErrorInfo(err.code, err.message));
      } else if (err instanceof Error) {
        if (err.message.includes("not configured")) {
          setError(getErrorInfo("UNKNOWN_ERROR", "GitHub sign-in is not available at this time."));
        } else if (!err.message.includes("cancelled")) {
          setError(getErrorInfo("UNKNOWN_ERROR", err.message));
        }
      }
    } finally {
      setIsLoading(false);
    }
  };

  const effectiveLoading = isLoading || authLoading;

  console.log("[Register] Render - registrationSuccess:", registrationSuccess);

  if (registrationSuccess) {
    console.log("[Register] Rendering success screen!");
    return (
      <div className="text-center">
        {/* Success Icon with Animation */}
        <div className="relative flex items-center justify-center w-20 h-20 mx-auto mb-6">
          <div className="absolute inset-0 rounded-full bg-teal/20 animate-ping opacity-75" />
          <div className="relative flex items-center justify-center w-20 h-20 rounded-full bg-gradient-to-br from-teal/20 to-teal/10 border border-teal/30">
            <Mail className="h-10 w-10 text-teal" />
          </div>
        </div>

        {/* Title */}
        <h1 className="text-2xl md:text-3xl font-bold text-slate-900 dark:text-slate-100 mb-3">
          {t("registrationSuccess.title")}
        </h1>

        {/* Email Display */}
        <p className="text-slate-500 dark:text-slate-400 mb-2">
          {t("registrationSuccess.subtitle")}
        </p>
        <p className="font-semibold text-slate-800 dark:text-slate-200 mb-4 px-4 py-2 bg-slate-100 dark:bg-slate-700/50 rounded-lg inline-block">
          {email}
        </p>

        {/* Description */}
        <p className="text-sm text-slate-500 dark:text-slate-400 mb-6 leading-relaxed max-w-sm mx-auto">
          {t("registrationSuccess.description")}
        </p>

        {/* Check spam hint */}
        <div className="flex items-center justify-center gap-2 text-xs text-slate-400 dark:text-slate-500 mb-8 px-4 py-2 bg-slate-50 dark:bg-slate-800/50 rounded-lg">
          <CheckCircle2 className="h-4 w-4 flex-shrink-0" />
          <span>{t("registrationSuccess.checkSpam")}</span>
        </div>

        {/* Back to Login Button */}
        <Link href="/login">
          <Button className="w-full h-12 mb-4">
            <ArrowLeft className="h-4 w-4 mr-2" />
            {t("registrationSuccess.backToLogin")}
          </Button>
        </Link>

        {/* Resend Section */}
        <div className="pt-4 border-t border-slate-200 dark:border-slate-700">
          <p className="text-sm text-slate-500 dark:text-slate-400 mb-3">
            {t("registrationSuccess.didntReceive")}
          </p>
          <button
            onClick={handleResendVerification}
            disabled={resendState !== "idle"}
            className={cn(
              "inline-flex items-center gap-2 text-sm font-medium transition-all duration-200",
              resendState === "sent"
                ? "text-green-600 dark:text-green-400"
                : resendState === "sending"
                  ? "text-slate-400 cursor-not-allowed"
                  : "text-teal hover:text-teal-dark hover:underline"
            )}
          >
            {resendState === "sending" ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                {t("registrationSuccess.resending")}
              </>
            ) : resendState === "sent" ? (
              <>
                <CheckCircle2 className="h-4 w-4" />
                {t("registrationSuccess.resent")}
              </>
            ) : (
              <>
                <RefreshCw className="h-4 w-4" />
                {t("registrationSuccess.resendVerification")}
              </>
            )}
          </button>
        </div>

        {/* Wrong email link */}
        <p className="mt-6 text-xs text-slate-400 dark:text-slate-500">
          {t("registrationSuccess.wrongEmail")}{" "}
          <button
            onClick={handleBackToRegister}
            className="text-teal hover:underline font-medium"
          >
            {t("registrationSuccess.tryAgain")}
          </button>
        </p>
      </div>
    );
  }

  return (
    <>
      <div className="text-center mb-8">
        <h1 className="text-2xl md:text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          {t("createAccount")}
        </h1>
        <p className="text-slate-500 dark:text-slate-400">Start your journey with GeneFlow</p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-5">
        {error && <ErrorAlert error={error} />}

        <div className="space-y-2">
          <label htmlFor="username" className="block text-sm font-medium text-slate-700 dark:text-slate-200">
            Username
          </label>
          <input
            type="text"
            id="username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            placeholder="johndoe"
            autoComplete="username"
            className={cn(
              "w-full h-11 px-4 rounded-lg border bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 transition-all",
              "focus:outline-none focus:ring-2 focus:ring-teal/20 focus:border-teal",
              submitted && !usernameValid ? "border-red-500" : "border-slate-300 dark:border-slate-600"
            )}
          />
          {submitted && !usernameValid && (
            <p className="text-xs text-red-500">Username must be at least 3 characters (letters and numbers only)</p>
          )}
        </div>

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
              "w-full h-11 px-4 rounded-lg border bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 transition-all",
              "focus:outline-none focus:ring-2 focus:ring-teal/20 focus:border-teal",
              submitted && !emailValid ? "border-red-500" : "border-slate-300 dark:border-slate-600"
            )}
          />
          {submitted && !emailValid && <p className="text-xs text-red-500">{t("validation.emailRequired")}</p>}
        </div>

        <div className="space-y-2">
          <label htmlFor="password" className="block text-sm font-medium text-slate-700 dark:text-slate-200">
            {t("password")}
          </label>
          <div className="relative">
            <input
              type={showPassword ? "text" : "password"}
              id="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              autoComplete="new-password"
              className={cn(
                "w-full h-11 px-4 pr-12 rounded-lg border bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 transition-all",
                "focus:outline-none focus:ring-2 focus:ring-teal/20 focus:border-teal",
                submitted && !passwordValid ? "border-red-500" : "border-slate-300 dark:border-slate-600"
              )}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-slate-400 hover:text-slate-600"
            >
              {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
            </button>
          </div>
          {password.length > 0 && (
            <div className="mt-2 p-3 bg-slate-100 dark:bg-slate-700/50 rounded-lg space-y-1">
              {passwordRequirements.map((req, index) => (
                <div key={index} className={cn("flex items-center gap-2 text-xs", req.valid ? "text-green-600" : "text-slate-500")}>
                  {req.valid ? <Check className="h-3.5 w-3.5" /> : <X className="h-3.5 w-3.5 text-red-500" />}
                  {req.label}
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="space-y-2">
          <label htmlFor="confirmPassword" className="block text-sm font-medium text-slate-700 dark:text-slate-200">
            {t("confirmPassword")}
          </label>
          <div className="relative">
            <input
              type={showConfirmPassword ? "text" : "password"}
              id="confirmPassword"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="••••••••"
              autoComplete="new-password"
              className={cn(
                "w-full h-11 px-4 pr-12 rounded-lg border bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 transition-all",
                "focus:outline-none focus:ring-2 focus:ring-teal/20 focus:border-teal",
                confirmPassword && !passwordsMatch ? "border-red-500" : "border-slate-300 dark:border-slate-600"
              )}
            />
            <button
              type="button"
              onClick={() => setShowConfirmPassword(!showConfirmPassword)}
              className="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-slate-400 hover:text-slate-600"
            >
              {showConfirmPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
            </button>
          </div>
          {confirmPassword && !passwordsMatch && <p className="text-xs text-red-500">{t("validation.passwordsMatch")}</p>}
        </div>

        <div className="space-y-2">
          <div className="flex items-start gap-2">
            <input
              type="checkbox"
              id="acceptTerms"
              checked={acceptTerms}
              onChange={(e) => setAcceptTerms(e.target.checked)}
              className="h-4 w-4 mt-0.5 rounded border-slate-300 text-teal focus:ring-teal/20 cursor-pointer"
            />
            <label htmlFor="acceptTerms" className="text-sm text-slate-600 dark:text-slate-400 leading-relaxed cursor-pointer">
              {t("agreeToTerms")} <a href="#" className="text-teal hover:underline">{t("termsOfService")}</a>
              {" "}{tCommon("and")}{" "}
              <a href="#" className="text-teal hover:underline">{t("privacyPolicy")}</a>
            </label>
          </div>
          {submitted && !acceptTerms && <p className="text-xs text-red-500">{t("validation.termsRequired")}</p>}
        </div>

        <Button type="submit" className="w-full h-12" disabled={effectiveLoading}>
          {effectiveLoading ? (
            <>
              <Loader2 className="h-5 w-5 animate-spin" />
              {t("registering")}
            </>
          ) : (
            t("register")
          )}
        </Button>
      </form>

      <div className="relative my-6">
        <div className="absolute inset-0 flex items-center">
          <div className="w-full border-t border-slate-200 dark:border-slate-700" />
        </div>
        <div className="relative flex justify-center text-sm">
          <span className="bg-slate-50 dark:bg-slate-800 px-4 text-slate-500">{t("orContinueWith")}</span>
        </div>
      </div>

      <div className="flex justify-center gap-4">
        <button
          type="button"
          onClick={handleGoogleSignIn}
          disabled={effectiveLoading}
          className={cn(
            "flex items-center justify-center w-12 h-12 rounded-full border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 transition-all",
            effectiveLoading ? "opacity-50 cursor-not-allowed" : "hover:bg-slate-50 dark:hover:bg-slate-600"
          )}
          aria-label="Sign up with Google"
        >
          <svg className="h-5 w-5" viewBox="0 0 24 24">
            <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
            <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
            <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
            <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
          </svg>
        </button>
        <button
          type="button"
          onClick={handleGitHubSignIn}
          disabled={effectiveLoading}
          className={cn(
            "flex items-center justify-center w-12 h-12 rounded-full border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 transition-all",
            effectiveLoading ? "opacity-50 cursor-not-allowed" : "hover:bg-slate-50 dark:hover:bg-slate-600"
          )}
          aria-label="Sign up with GitHub"
        >
          <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 24 24">
            <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
          </svg>
        </button>
      </div>

      <p className="mt-8 text-center text-sm text-slate-500 dark:text-slate-400">
        {t("alreadyHaveAccount")}{" "}
        <Link href="/login" className="text-teal font-medium hover:underline">{t("signIn")}</Link>
      </p>
    </>
  );
}

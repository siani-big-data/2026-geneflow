"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Eye, EyeOff, AlertCircle, Loader2, Mail, CheckCircle } from "lucide-react";
import { Button } from "@/components/ui";
import { cn } from "@/lib/utils";

export default function LoginPage() {
  const router = useRouter();

  // Form state
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  // UI state
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [submitted, setSubmitted] = useState(false);

  // 2FA state
  const [requiresTwoFactor, setRequiresTwoFactor] = useState(false);
  const [twoFactorCode, setTwoFactorCode] = useState("");
  const [twoFactorSubmitted, setTwoFactorSubmitted] = useState(false);
  const [codeSent, setCodeSent] = useState(false);

  // Validation
  const emailValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  const passwordValid = password.length >= 8;
  const twoFactorCodeValid = /^\d{6}$/.test(twoFactorCode);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitted(true);
    setError("");

    if (!emailValid || !passwordValid) {
      return;
    }

    setIsLoading(true);

    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 1500));

    // Simulate 2FA requirement for specific email
    if (email.includes("2fa")) {
      setRequiresTwoFactor(true);
      setCodeSent(true);
      setIsLoading(false);
      return;
    }

    // Simulate successful login
    setIsLoading(false);
    router.push("/dashboard");
  };

  const handleTwoFactorSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setTwoFactorSubmitted(true);
    setError("");

    if (!twoFactorCodeValid) {
      return;
    }

    setIsLoading(true);

    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 1500));

    // Simulate successful verification
    setIsLoading(false);
    router.push("/dashboard");
  };

  const resendCode = async () => {
    setIsLoading(true);
    await new Promise((resolve) => setTimeout(resolve, 1000));
    setCodeSent(true);
    setIsLoading(false);
  };

  const backToLogin = () => {
    setRequiresTwoFactor(false);
    setTwoFactorCode("");
    setTwoFactorSubmitted(false);
    setCodeSent(false);
  };

  if (requiresTwoFactor) {
    return (
      <div className="text-center">
        {/* 2FA Icon */}
        <div className="flex items-center justify-center w-16 h-16 mx-auto mb-6 rounded-full bg-blue-500/10">
          <Mail className="h-8 w-8 text-blue-500" />
        </div>

        <h1 className="text-2xl md:text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Two-Factor Authentication
        </h1>
        <p className="text-slate-500 dark:text-slate-400 mb-8">
          We&apos;ve sent a verification code to your email address.
        </p>

        <form onSubmit={handleTwoFactorSubmit} className="space-y-6">
          {/* Error message */}
          {error && (
            <div className="flex items-center gap-3 p-4 rounded-lg bg-red-500/10 border border-red-500/20 text-red-600 dark:text-red-400 text-sm">
              <AlertCircle className="h-5 w-5 flex-shrink-0" />
              {error}
            </div>
          )}

          {/* Success message */}
          {codeSent && (
            <div className="flex items-center justify-center gap-2 p-4 rounded-lg bg-green-500/10 border border-green-500/20 text-green-600 dark:text-green-400 text-sm">
              <CheckCircle className="h-5 w-5 flex-shrink-0" />
              Verification code sent successfully
            </div>
          )}

          {/* Code input */}
          <div className="space-y-2">
            <label htmlFor="twoFactorCode" className="block text-sm font-medium text-slate-700 dark:text-slate-200">
              Verification Code
            </label>
            <input
              type="text"
              id="twoFactorCode"
              value={twoFactorCode}
              onChange={(e) => setTwoFactorCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
              placeholder="000000"
              maxLength={6}
              inputMode="numeric"
              autoComplete="one-time-code"
              className={cn(
                "w-full h-11 px-4 text-center text-xl font-semibold tracking-[0.5em] rounded-lg border bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 dark:placeholder:text-slate-500 transition-all",
                "focus:outline-none focus:ring-2 focus:ring-teal/20 focus:border-teal",
                twoFactorSubmitted && !twoFactorCodeValid
                  ? "border-red-500 focus:ring-red-500/20"
                  : "border-slate-300 dark:border-slate-600"
              )}
            />
            {twoFactorSubmitted && !twoFactorCodeValid && (
              <p className="text-xs text-red-500 mt-1">Please enter a valid 6-digit code</p>
            )}
          </div>

          {/* Submit button */}
          <Button type="submit" className="w-full h-12" disabled={isLoading}>
            {isLoading ? (
              <>
                <Loader2 className="h-5 w-5 animate-spin" />
                Verifying...
              </>
            ) : (
              "Verify Code"
            )}
          </Button>

          {/* Actions */}
          <div className="flex items-center justify-center gap-6 text-sm">
            <button
              type="button"
              onClick={resendCode}
              disabled={isLoading}
              className="text-teal hover:underline disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Resend code
            </button>
            <button
              type="button"
              onClick={backToLogin}
              className="text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200"
            >
              Back to login
            </button>
          </div>
        </form>
      </div>
    );
  }

  return (
    <>
      <div className="text-center mb-8">
        <h1 className="text-2xl md:text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Welcome back
        </h1>
        <p className="text-slate-500 dark:text-slate-400">
          Sign in to your GeneFlow account
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
            Email
          </label>
          <input
            type="email"
            id="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="name@company.com"
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
            <p className="text-xs text-red-500">Please enter a valid email address</p>
          )}
        </div>

        {/* Password field */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <label htmlFor="password" className="block text-sm font-medium text-slate-700 dark:text-slate-200">
              Password
            </label>
            <Link href="/forgot-password" className="text-sm text-teal hover:underline">
              Forgot password?
            </Link>
          </div>
          <div className="relative">
            <input
              type={showPassword ? "text" : "password"}
              id="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              autoComplete="current-password"
              className={cn(
                "w-full h-11 px-4 pr-12 rounded-lg border bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 dark:placeholder:text-slate-500 transition-all",
                "focus:outline-none focus:ring-2 focus:ring-teal/20 focus:border-teal",
                submitted && !passwordValid
                  ? "border-red-500 focus:ring-red-500/20"
                  : "border-slate-300 dark:border-slate-600"
              )}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300"
              aria-label={showPassword ? "Hide password" : "Show password"}
            >
              {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
            </button>
          </div>
          {submitted && !passwordValid && (
            <p className="text-xs text-red-500">Password must be at least 8 characters</p>
          )}
        </div>

        {/* Remember me */}
        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="rememberMe"
            checked={rememberMe}
            onChange={(e) => setRememberMe(e.target.checked)}
            className="h-4 w-4 rounded border-slate-300 text-teal focus:ring-teal/20 cursor-pointer"
          />
          <label htmlFor="rememberMe" className="text-sm text-slate-600 dark:text-slate-400 cursor-pointer">
            Remember me
          </label>
        </div>

        {/* Submit button */}
        <Button type="submit" className="w-full h-12" disabled={isLoading}>
          {isLoading ? (
            <>
              <Loader2 className="h-5 w-5 animate-spin" />
              Signing in...
            </>
          ) : (
            "Sign in"
          )}
        </Button>
      </form>

      {/* Divider */}
      <div className="relative my-6">
        <div className="absolute inset-0 flex items-center">
          <div className="w-full border-t border-slate-200 dark:border-slate-700" />
        </div>
        <div className="relative flex justify-center text-sm">
          <span className="bg-slate-50 dark:bg-slate-800 px-4 text-slate-500 dark:text-slate-400">
            Or continue with
          </span>
        </div>
      </div>

      {/* OAuth buttons */}
      <div className="flex justify-center gap-4">
        <button
          type="button"
          className="flex items-center justify-center w-12 h-12 rounded-full border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-600 hover:border-slate-400 dark:hover:border-slate-500 transition-all"
          aria-label="Sign in with Google"
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
          className="flex items-center justify-center w-12 h-12 rounded-full border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-600 hover:border-slate-400 dark:hover:border-slate-500 transition-all"
          aria-label="Sign in with GitHub"
        >
          <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 24 24">
            <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
          </svg>
        </button>
      </div>

      {/* Footer */}
      <p className="mt-8 text-center text-sm text-slate-500 dark:text-slate-400">
        Don&apos;t have an account?{" "}
        <Link href="/register" className="text-teal font-medium hover:underline">
          Sign up
        </Link>
      </p>
    </>
  );
}

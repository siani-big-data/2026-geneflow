"use client";

import { useState } from "react";
import Link from "next/link";
import { Eye, EyeOff, AlertCircle, Loader2, Mail, Check, X } from "lucide-react";
import { Button } from "@/components/ui";
import { cn } from "@/lib/utils";

export default function RegisterPage() {
  // Form state
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [acceptTerms, setAcceptTerms] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  // UI state
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [registrationSuccess, setRegistrationSuccess] = useState(false);

  // Validation
  const usernameValid = username.length >= 3 && /^[a-zA-Z0-9_]+$/.test(username);
  const emailValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  const passwordsMatch = password === confirmPassword && confirmPassword.length > 0;

  // Password requirements
  const passwordRequirements = [
    { label: "At least 8 characters", valid: password.length >= 8 },
    { label: "Contains uppercase letter", valid: /[A-Z]/.test(password) },
    { label: "Contains lowercase letter", valid: /[a-z]/.test(password) },
    { label: "Contains a number", valid: /\d/.test(password) },
  ];

  const passwordValid = passwordRequirements.every((req) => req.valid);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitted(true);
    setError("");

    if (!usernameValid || !emailValid || !passwordValid || !passwordsMatch || !acceptTerms) {
      return;
    }

    setIsLoading(true);

    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 2000));

    // Simulate successful registration
    setIsLoading(false);
    setRegistrationSuccess(true);
  };

  if (registrationSuccess) {
    return (
      <div className="text-center">
        {/* Success Icon */}
        <div className="flex items-center justify-center w-16 h-16 mx-auto mb-6 rounded-full bg-blue-500/10">
          <Mail className="h-8 w-8 text-blue-500" />
        </div>

        <h1 className="text-2xl md:text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Check your email
        </h1>
        <p className="text-slate-500 dark:text-slate-400 mb-8 leading-relaxed">
          We&apos;ve sent a verification link to <strong className="text-slate-700 dark:text-slate-200">{email}</strong>.
          Please click the link to activate your account.
        </p>

        <Link href="/login">
          <Button className="w-full h-12">
            Back to login
          </Button>
        </Link>

        <p className="mt-6 text-sm text-slate-500 dark:text-slate-400">
          Didn&apos;t receive the email?{" "}
          <button className="text-teal hover:underline">
            Resend verification
          </button>
        </p>
      </div>
    );
  }

  return (
    <>
      <div className="text-center mb-8">
        <h1 className="text-2xl md:text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Create an account
        </h1>
        <p className="text-slate-500 dark:text-slate-400">
          Get started with GeneFlow for free
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

        {/* Username field */}
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
              "w-full h-11 px-4 rounded-lg border bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 dark:placeholder:text-slate-500 transition-all",
              "focus:outline-none focus:ring-2 focus:ring-teal/20 focus:border-teal",
              submitted && !usernameValid
                ? "border-red-500 focus:ring-red-500/20"
                : "border-slate-300 dark:border-slate-600"
            )}
          />
          {submitted && !usernameValid && (
            <p className="text-xs text-red-500">Username must be at least 3 characters (letters, numbers, underscores)</p>
          )}
        </div>

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
            placeholder="name@example.com"
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
          <label htmlFor="password" className="block text-sm font-medium text-slate-700 dark:text-slate-200">
            Password
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

          {/* Password requirements */}
          {password.length > 0 && (
            <div className="mt-2 p-3 bg-slate-100 dark:bg-slate-700/50 rounded-lg space-y-1">
              {passwordRequirements.map((req, index) => (
                <div
                  key={index}
                  className={cn(
                    "flex items-center gap-2 text-xs",
                    req.valid ? "text-green-600 dark:text-green-400" : "text-slate-500 dark:text-slate-400"
                  )}
                >
                  {req.valid ? (
                    <Check className="h-3.5 w-3.5" />
                  ) : (
                    <X className="h-3.5 w-3.5 text-red-500" />
                  )}
                  {req.label}
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Confirm Password field */}
        <div className="space-y-2">
          <label htmlFor="confirmPassword" className="block text-sm font-medium text-slate-700 dark:text-slate-200">
            Confirm Password
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
                "w-full h-11 px-4 pr-12 rounded-lg border bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 dark:placeholder:text-slate-500 transition-all",
                "focus:outline-none focus:ring-2 focus:ring-teal/20 focus:border-teal",
                confirmPassword && !passwordsMatch
                  ? "border-red-500 focus:ring-red-500/20"
                  : "border-slate-300 dark:border-slate-600"
              )}
            />
            <button
              type="button"
              onClick={() => setShowConfirmPassword(!showConfirmPassword)}
              className="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300"
              aria-label={showConfirmPassword ? "Hide password" : "Show password"}
            >
              {showConfirmPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
            </button>
          </div>
          {confirmPassword && !passwordsMatch && (
            <p className="text-xs text-red-500">Passwords do not match</p>
          )}
        </div>

        {/* Accept terms */}
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
              I agree to the{" "}
              <a href="#" className="text-teal hover:underline">Terms of Service</a>
              {" "}and{" "}
              <a href="#" className="text-teal hover:underline">Privacy Policy</a>
            </label>
          </div>
          {submitted && !acceptTerms && (
            <p className="text-xs text-red-500">You must accept the terms and conditions</p>
          )}
        </div>

        {/* Submit button */}
        <Button type="submit" className="w-full h-12" disabled={isLoading}>
          {isLoading ? (
            <>
              <Loader2 className="h-5 w-5 animate-spin" />
              Creating account...
            </>
          ) : (
            "Create account"
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
            Or sign up with
          </span>
        </div>
      </div>

      {/* OAuth buttons */}
      <div className="flex justify-center gap-4">
        <button
          type="button"
          className="flex items-center justify-center w-12 h-12 rounded-full border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-600 hover:border-slate-400 dark:hover:border-slate-500 transition-all"
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
          className="flex items-center justify-center w-12 h-12 rounded-full border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-600 hover:border-slate-400 dark:hover:border-slate-500 transition-all"
          aria-label="Sign up with GitHub"
        >
          <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 24 24">
            <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
          </svg>
        </button>
      </div>

      {/* Footer */}
      <p className="mt-8 text-center text-sm text-slate-500 dark:text-slate-400">
        Already have an account?{" "}
        <Link href="/login" className="text-teal font-medium hover:underline">
          Sign in
        </Link>
      </p>
    </>
  );
}

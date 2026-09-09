"use client";

import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Link } from "@/lib/navigation";
import { CheckCircle, XCircle, Loader2 } from "lucide-react";
import { Button } from "@/components/ui";
import { authService } from "@/services";

type VerificationStatus = "loading" | "success" | "error";

export default function VerifyEmailPage() {
  const searchParams = useSearchParams();
  const token = searchParams.get("token");

  const [status, setStatus] = useState<VerificationStatus>("loading");
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    async function verifyEmail() {
      if (!token) {
        setStatus("error");
        setErrorMessage("No verification token provided.");
        return;
      }

      try {
        await authService.verifyEmail({ token });
        setStatus("success");
      } catch (error) {
        setStatus("error");
        if (error instanceof Error) {
          setErrorMessage(error.message);
        } else {
          setErrorMessage("Failed to verify email. The link may have expired.");
        }
      }
    }

    verifyEmail();
  }, [token]);

  if (status === "loading") {
    return (
      <div className="text-center">
        <div className="flex items-center justify-center w-16 h-16 mx-auto mb-6 rounded-full bg-blue-500/10">
          <Loader2 className="h-8 w-8 text-blue-500 animate-spin" />
        </div>
        <h1 className="text-2xl md:text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Verifying your email
        </h1>
        <p className="text-slate-500 dark:text-slate-400">
          Please wait while we verify your email address...
        </p>
      </div>
    );
  }

  if (status === "success") {
    return (
      <div className="text-center">
        <div className="flex items-center justify-center w-16 h-16 mx-auto mb-6 rounded-full bg-green-500/10">
          <CheckCircle className="h-8 w-8 text-green-500" />
        </div>
        <h1 className="text-2xl md:text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Email verified!
        </h1>
        <p className="text-slate-500 dark:text-slate-400 mb-8">
          Your email has been successfully verified. You can now log in to your account.
        </p>
        <Link href="/login">
          <Button className="w-full h-12">Continue to login</Button>
        </Link>
      </div>
    );
  }

  return (
    <div className="text-center">
      <div className="flex items-center justify-center w-16 h-16 mx-auto mb-6 rounded-full bg-red-500/10">
        <XCircle className="h-8 w-8 text-red-500" />
      </div>
      <h1 className="text-2xl md:text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
        Verification failed
      </h1>
      <p className="text-slate-500 dark:text-slate-400 mb-8">
        {errorMessage}
      </p>
      <div className="space-y-3">
        <Link href="/login">
          <Button className="w-full h-12">Go to login</Button>
        </Link>
        <Link href="/register">
          <Button variant="outline" className="w-full h-12">
            Create new account
          </Button>
        </Link>
      </div>
    </div>
  );
}

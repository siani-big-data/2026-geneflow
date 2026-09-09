"use client";

import { Suspense, useEffect } from "react";
import { useSearchParams } from "next/navigation";
import { Loader2 } from "lucide-react";

/**
 * GitHub OAuth Callback Content Component.
 *
 * Handles the actual callback logic.
 */
function GitHubCallbackContent() {
  const searchParams = useSearchParams();

  useEffect(() => {
    const code = searchParams.get("code");
    const state = searchParams.get("state");
    const error = searchParams.get("error");
    const errorDescription = searchParams.get("error_description");

    // Check for errors from GitHub
    if (error) {
      window.opener?.postMessage(
        {
          type: "github-oauth-callback",
          error: errorDescription || error,
        },
        window.location.origin
      );
      return;
    }

    // Validate state to prevent CSRF
    const storedState = sessionStorage.getItem("github_oauth_state");
    if (!state || state !== storedState) {
      window.opener?.postMessage(
        {
          type: "github-oauth-callback",
          error: "Invalid state parameter. Please try again.",
        },
        window.location.origin
      );
      return;
    }

    // Clear the stored state
    sessionStorage.removeItem("github_oauth_state");

    if (!code) {
      window.opener?.postMessage(
        {
          type: "github-oauth-callback",
          error: "No authorization code received from GitHub.",
        },
        window.location.origin
      );
      return;
    }

    // Exchange the code for an access token via our backend
    // The code needs to be exchanged server-side to keep the client secret secure
    exchangeCodeForToken(code);
  }, [searchParams]);

  async function exchangeCodeForToken(code: string) {
    try {
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5286";
      const response = await fetch(`${apiUrl}/api/v1/auth/oauth/github/exchange`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ code }),
      });

      if (!response.ok) {
        const error = await response.json().catch(() => ({ message: "Failed to exchange code" }));
        window.opener?.postMessage(
          {
            type: "github-oauth-callback",
            error: error.message || "Failed to authenticate with GitHub",
          },
          window.location.origin
        );
        return;
      }

      const data = await response.json();

      // Send the access token back to the parent window
      window.opener?.postMessage(
        {
          type: "github-oauth-callback",
          token: data.accessToken,
        },
        window.location.origin
      );
    } catch (err) {
      window.opener?.postMessage(
        {
          type: "github-oauth-callback",
          error: err instanceof Error ? err.message : "Failed to authenticate with GitHub",
        },
        window.location.origin
      );
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-900">
      <div className="text-center">
        <Loader2 className="h-8 w-8 animate-spin text-teal mx-auto mb-4" />
        <p className="text-slate-600 dark:text-slate-400">
          Completing GitHub sign in...
        </p>
      </div>
    </div>
  );
}

/**
 * GitHub OAuth Callback Page.
 *
 * This page handles the redirect from GitHub after OAuth authorization.
 * It extracts the authorization code and state from the URL, validates
 * the state to prevent CSRF attacks, and sends the result back to the
 * parent window that opened the popup.
 */
export default function GitHubCallbackPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-900">
          <div className="text-center">
            <Loader2 className="h-8 w-8 animate-spin text-teal mx-auto mb-4" />
            <p className="text-slate-600 dark:text-slate-400">
              Loading...
            </p>
          </div>
        </div>
      }
    >
      <GitHubCallbackContent />
    </Suspense>
  );
}

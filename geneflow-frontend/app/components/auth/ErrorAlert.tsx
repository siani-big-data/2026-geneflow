"use client";

import { AlertCircle, ArrowRight } from "lucide-react";
import { Link } from "@/lib/navigation";
import { ErrorInfo } from "@/lib/error-messages";

interface ErrorAlertProps {
  error: ErrorInfo;
  onAction?: () => void;
}

/**
 * Error alert component for authentication forms.
 * Displays user-friendly error messages with optional action links.
 */
export function ErrorAlert({ error, onAction }: ErrorAlertProps) {
  return (
    <div className="p-4 rounded-lg bg-red-500/10 border border-red-500/20">
      <div className="flex items-start gap-3">
        <AlertCircle className="h-5 w-5 text-red-500 flex-shrink-0 mt-0.5" />
        <div className="flex-1 min-w-0">
          <h4 className="text-sm font-medium text-red-600 dark:text-red-400">
            {error.title}
          </h4>
          <p className="text-sm text-red-600/80 dark:text-red-400/80 mt-1">
            {error.message}
          </p>
          {error.action && (
            <div className="mt-3">
              {error.actionLink ? (
                <Link
                  href={error.actionLink}
                  className="inline-flex items-center gap-1.5 text-sm font-medium text-red-600 dark:text-red-400 hover:underline"
                >
                  {error.action}
                  <ArrowRight className="h-4 w-4" />
                </Link>
              ) : onAction ? (
                <button
                  type="button"
                  onClick={onAction}
                  className="inline-flex items-center gap-1.5 text-sm font-medium text-red-600 dark:text-red-400 hover:underline"
                >
                  {error.action}
                  <ArrowRight className="h-4 w-4" />
                </button>
              ) : null}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

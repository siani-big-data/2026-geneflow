"use client";

import Link from "next/link";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui";
import { Home } from "lucide-react";

export default function NotFound() {
  const t = useTranslations("errors");

  return (
    <div className="flex min-h-screen flex-col items-center justify-center">
      <div className="text-center">
        <h1 className="text-6xl font-bold text-teal">404</h1>
        <h2 className="mt-4 text-2xl font-semibold text-foreground">
          {t("notFound")}
        </h2>
        <p className="mt-2 text-muted-foreground">
          {t("notFoundDescription")}
        </p>
        <Link href="/dashboard" className="mt-8 inline-block">
          <Button>
            <Home className="h-4 w-4" />
            {t("goHome")}
          </Button>
        </Link>
      </div>
    </div>
  );
}

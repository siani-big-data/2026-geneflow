"use client";

import { useTranslations } from "next-intl";
import { Rss } from "lucide-react";
import { FeedList } from "@/components/search";

export default function FeedPage() {
  const t = useTranslations("feed");

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div className="flex items-center gap-3">
        <div className="rounded-lg bg-teal/10 p-2">
          <Rss className="h-5 w-5 text-teal" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold text-foreground">
            {t("title")}
          </h1>
          <p className="text-sm text-muted-foreground">{t("subtitle")}</p>
        </div>
      </div>

      <FeedList />
    </div>
  );
}

"use client";

import { Search, Upload, Moon, Sun, Bell } from "lucide-react";
import { useTranslations } from "next-intl";
import { useTheme } from "@/providers";
import { Button } from "@/components/ui";
import { LocaleSwitcher } from "@/components/shared";

export function Header() {
  const { theme, setTheme, resolvedTheme } = useTheme();
  const t = useTranslations("header");

  const toggleTheme = () => {
    if (theme === "system") {
      setTheme(resolvedTheme === "dark" ? "light" : "dark");
    } else {
      setTheme(theme === "dark" ? "light" : "dark");
    }
  };

  return (
    <header className="flex h-16 flex-shrink-0 items-center justify-between border-b border-border bg-card px-6">
      {/* Search */}
      <div className="flex-1">
        <div className="relative max-w-md">
          <label htmlFor="global-search" className="sr-only">
            {t("searchPlaceholder")}
          </label>
          <Search
            className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground transition-colors duration-200 peer-focus:text-teal"
            aria-hidden="true"
          />
          <input
            id="global-search"
            type="search"
            placeholder={t("searchPlaceholder")}
            className="peer min-h-[40px] w-full rounded-lg border border-border/50 bg-muted/50 py-2 pl-10 pr-4 text-sm placeholder:text-muted-foreground/70 transition-all duration-200 focus:border-teal/50 focus:bg-background focus:outline-none focus:ring-2 focus:ring-teal/20 dark:border-white/10 dark:bg-white/5 dark:placeholder:text-white/40 dark:focus:border-teal/60 dark:focus:bg-white/10"
            aria-label={t("searchPlaceholder")}
          />
        </div>
      </div>

      {/* Actions */}
      <div className="flex items-center gap-3">
        <Button className="bg-gradient-to-r from-teal to-teal/90 hover:from-teal/90 hover:to-teal/80 hover:shadow-md">
          <Upload className="h-4 w-4" />
          <span>{t("uploadTraces")}</span>
        </Button>

        <LocaleSwitcher />

        <button
          onClick={toggleTheme}
          className="flex min-h-[40px] min-w-[40px] items-center justify-center rounded-lg p-2.5 text-muted-foreground transition-all duration-200 hover:bg-muted/50 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 active:scale-95"
          aria-label={
            resolvedTheme === "light" ? t("switchToDark") : t("switchToLight")
          }
        >
          {resolvedTheme === "light" ? (
            <Moon className="h-5 w-5 transition-transform duration-200" />
          ) : (
            <Sun className="h-5 w-5 transition-transform duration-200" />
          )}
        </button>

        <button
          className="relative flex min-h-[40px] min-w-[40px] items-center justify-center rounded-lg p-2.5 text-muted-foreground transition-all duration-200 hover:bg-muted/50 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 active:scale-95"
          aria-label={t("notifications")}
        >
          <Bell className="h-5 w-5 transition-transform duration-200" />
          <span
            className="absolute right-2 top-2 h-2 w-2 animate-pulse rounded-full bg-teal ring-2 ring-card"
            aria-hidden="true"
          />
        </button>
      </div>
    </header>
  );
}

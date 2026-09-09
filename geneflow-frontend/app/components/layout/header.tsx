"use client";

import { Moon, Sun } from "lucide-react";
import { useTranslations } from "next-intl";
import { useTheme } from "@/providers";
import { LocaleSwitcher } from "@/components/shared";
import { NotificationsTray } from "./notifications-tray";

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
    <header className="flex h-16 flex-shrink-0 items-center justify-end border-b border-border bg-card px-6">
      {/* Actions */}
      <div className="flex items-center gap-3">
        <NotificationsTray />

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
      </div>
    </header>
  );
}

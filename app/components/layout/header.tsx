"use client";

import { useState, useRef, useEffect } from "react";
import { Search, Upload, Moon, Sun, Bell, User, LogOut, Settings, ChevronDown } from "lucide-react";
import { useTranslations } from "next-intl";
import { useRouter } from "@/lib/navigation";
import { useTheme } from "@/providers";
import { Button } from "@/components/ui";
import { LocaleSwitcher } from "@/components/shared";
import { useAuthStore } from "@/stores/auth-store";
import { profileService } from "@/services/profile.service";

export function Header() {
  const { theme, setTheme, resolvedTheme } = useTheme();
  const t = useTranslations("header");
  const router = useRouter();
  const { user, profile, logout, isLoading } = useAuthStore();

  // Get display name and photo from profile if available
  const displayName = profile?.fullName || user?.username || "User";
  const initials = profile?.initials || displayName.charAt(0).toUpperCase();
  const photoUrl = profileService.resolveStorageUrl(profile?.photoThumbnailUrl || profile?.photoUrl);

  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);

  // Close menu when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setUserMenuOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const toggleTheme = () => {
    if (theme === "system") {
      setTheme(resolvedTheme === "dark" ? "light" : "dark");
    } else {
      setTheme(theme === "dark" ? "light" : "dark");
    }
  };

  const handleLogout = async () => {
    setUserMenuOpen(false);
    await logout();
    router.push("/login");
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

        {/* User Menu */}
        <div className="relative" ref={userMenuRef}>
          <button
            onClick={() => setUserMenuOpen(!userMenuOpen)}
            className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-all duration-200 hover:bg-muted/50 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
            aria-expanded={userMenuOpen}
            aria-haspopup="true"
          >
            {photoUrl ? (
              <img
                src={photoUrl}
                alt={displayName}
                className="h-8 w-8 rounded-full object-cover"
              />
            ) : (
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-teal/10 text-xs font-medium text-teal">
                {initials}
              </div>
            )}
            <span className="hidden md:block font-medium text-foreground">
              {displayName}
            </span>
            <ChevronDown className={`h-4 w-4 transition-transform duration-200 ${userMenuOpen ? "rotate-180" : ""}`} />
          </button>

          {/* Dropdown Menu */}
          {userMenuOpen && (
            <div className="absolute right-0 top-full z-50 mt-2 w-56 origin-top-right rounded-lg border border-border bg-card py-1 shadow-lg ring-1 ring-black/5 focus:outline-none">
              {/* User Info */}
              <div className="border-b border-border px-4 py-3">
                <p className="text-sm font-medium text-foreground">{displayName}</p>
                <p className="truncate text-xs text-muted-foreground">{user?.email}</p>
              </div>

              {/* Menu Items */}
              <div className="py-1">
                <button
                  onClick={() => {
                    setUserMenuOpen(false);
                    router.push("/settings");
                  }}
                  className="flex w-full items-center gap-3 px-4 py-2 text-sm text-muted-foreground transition-colors hover:bg-muted/50 hover:text-foreground"
                >
                  <Settings className="h-4 w-4" />
                  {t("settings")}
                </button>
              </div>

              {/* Logout */}
              <div className="border-t border-border py-1">
                <button
                  onClick={handleLogout}
                  disabled={isLoading}
                  className="flex w-full items-center gap-3 px-4 py-2 text-sm text-red-500 transition-colors hover:bg-red-500/10 disabled:opacity-50"
                >
                  <LogOut className="h-4 w-4" />
                  {t("logout")}
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}

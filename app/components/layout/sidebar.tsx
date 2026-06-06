"use client";

import * as React from "react";
import Image from "next/image";
import { usePathname } from "next/navigation";
import { ChevronLeft, ChevronRight, User, LogOut } from "lucide-react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils";
import { useUIStore } from "@/stores/ui-store";
import { useAuthStore } from "@/stores/auth-store";
import { profileService } from "@/services/profile.service";
import { mainNavigation, bottomNavigation } from "./navigation";
import { Link, useRouter } from "@/lib/navigation";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui";

export function Sidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const { sidebarCollapsed, toggleSidebar } = useUIStore();
  const { user, profile, logout } = useAuthStore();
  const t = useTranslations("navigation");
  const tCommon = useTranslations("common");

  // Get display name and photo from profile if available
  const displayName = profile?.fullName || user?.username || "User";
  const initials = profile?.initials || displayName.charAt(0).toUpperCase();
  const photoUrl = profileService.resolveStorageUrl(profile?.photoThumbnailUrl || profile?.photoUrl);

  const handleLogout = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    await logout();
    router.push("/login");
  };

  const isActive = (href: string) => {
    // Remove locale prefix from pathname for comparison
    const pathWithoutLocale = pathname.replace(/^\/(es|en)/, "");
    if (href === "/dashboard") {
      return pathWithoutLocale === "/dashboard" || pathWithoutLocale === "/";
    }
    return pathWithoutLocale.startsWith(href);
  };

  return (
    <aside
      className={cn(
        "fixed left-0 top-0 bottom-0 z-30 flex flex-col border-r border-border bg-card transition-[width] duration-300 ease-out will-change-[width]",
        sidebarCollapsed ? "w-20" : "w-64"
      )}
      aria-label="Main navigation"
    >
      {/* Logo */}
      <div className="relative flex h-16 flex-shrink-0 items-center border-b border-border px-6">
        <Link
          href="/dashboard"
          className={cn(
            "flex items-center gap-2 rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
            sidebarCollapsed && "w-full justify-center"
          )}
          aria-label="GeneFlow home"
        >
          <Image
            src="/logo.png"
            alt="GeneFlow"
            width={40}
            height={40}
            className="flex-shrink-0 rounded-lg object-contain"
          />
          <div
            className={cn(
              "flex flex-col overflow-hidden whitespace-nowrap transition-all duration-300 ease-out",
              sidebarCollapsed ? "w-0 opacity-0" : "w-auto opacity-100"
            )}
          >
            <span className="text-base font-semibold leading-none text-foreground">
              {tCommon("appName")}
            </span>
            <span className="mt-0.5 text-[10px] leading-none text-muted-foreground">
              {tCommon("tagline")}
            </span>
          </div>
        </Link>

        {/* Collapse Toggle */}
        <button
          onClick={toggleSidebar}
          className="absolute -right-3 top-1/2 flex h-6 w-6 -translate-y-1/2 items-center justify-center rounded-full border border-border bg-card shadow-sm transition-all duration-200 hover:scale-110 hover:bg-muted/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          aria-label={sidebarCollapsed ? t("expandSidebar") : t("collapseSidebar")}
          aria-expanded={!sidebarCollapsed}
        >
          {sidebarCollapsed ? (
            <ChevronRight className="h-3.5 w-3.5 text-muted-foreground" />
          ) : (
            <ChevronLeft className="h-3.5 w-3.5 text-muted-foreground" />
          )}
        </button>
      </div>

      {/* Main Navigation */}
      <nav className="flex-1 space-y-1 overflow-y-auto p-4" aria-label="Main">
        {mainNavigation.map((item) => {
          const Icon = item.icon;
          const active = isActive(item.href);
          const name = t(item.nameKey);

          const linkContent = (
            <Link
              href={item.href}
              className={cn(
                "flex min-h-[40px] items-center gap-3 rounded-lg transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-card overflow-hidden",
                sidebarCollapsed ? "justify-center px-3 py-2.5" : "px-4 py-2.5",
                active
                  ? "bg-gradient-to-r from-teal/10 to-blue-deep/5 text-teal shadow-sm"
                  : "text-muted-foreground hover:translate-x-0.5 hover:bg-muted/50 hover:text-foreground"
              )}
              aria-current={active ? "page" : undefined}
            >
              <Icon className="h-[18px] w-[18px] flex-shrink-0 transition-transform duration-200" />
              <span
                className={cn(
                  "text-sm font-medium whitespace-nowrap transition-all duration-300 ease-out",
                  sidebarCollapsed ? "w-0 opacity-0" : "w-auto opacity-100"
                )}
              >
                {name}
              </span>
            </Link>
          );

          if (sidebarCollapsed) {
            return (
              <Tooltip key={item.nameKey} delayDuration={0}>
                <TooltipTrigger asChild>{linkContent}</TooltipTrigger>
                <TooltipContent side="right">{name}</TooltipContent>
              </Tooltip>
            );
          }

          return <React.Fragment key={item.nameKey}>{linkContent}</React.Fragment>;
        })}
      </nav>

      {/* Bottom Navigation */}
      <div className="flex-shrink-0 space-y-1 border-t border-border p-4">
        {bottomNavigation.map((item) => {
          const Icon = item.icon;
          const active = isActive(item.href);
          const name = t(item.nameKey);

          const linkContent = (
            <Link
              href={item.href}
              className={cn(
                "flex min-h-[40px] w-full items-center gap-3 rounded-lg text-muted-foreground transition-all duration-200 hover:translate-x-0.5 hover:bg-muted/50 hover:text-foreground overflow-hidden",
                sidebarCollapsed ? "justify-center px-3 py-2.5" : "px-4 py-2.5",
                active && "bg-muted/50 text-foreground"
              )}
            >
              <Icon className="h-[18px] w-[18px] flex-shrink-0 transition-transform duration-200" />
              <span
                className={cn(
                  "text-sm font-medium whitespace-nowrap transition-all duration-300 ease-out",
                  sidebarCollapsed ? "w-0 opacity-0" : "w-auto opacity-100"
                )}
              >
                {name}
              </span>
            </Link>
          );

          if (sidebarCollapsed) {
            return (
              <Tooltip key={item.nameKey} delayDuration={0}>
                <TooltipTrigger asChild>{linkContent}</TooltipTrigger>
                <TooltipContent side="right">{name}</TooltipContent>
              </Tooltip>
            );
          }

          return <React.Fragment key={item.nameKey}>{linkContent}</React.Fragment>;
        })}
      </div>

      {/* User Profile */}
      <div className="flex-shrink-0 border-t border-border p-4">
        <Tooltip delayDuration={0}>
          <TooltipTrigger asChild>
            <div
              className={cn(
                "group flex items-center gap-3 rounded-lg bg-muted/30 px-3 py-2.5 transition-all duration-300 ease-out hover:bg-muted/50 overflow-hidden",
                sidebarCollapsed && "justify-center"
              )}
            >
              <Link href="/profile" className="flex items-center gap-3 flex-1 min-w-0">
                {photoUrl ? (
                  <img
                    src={photoUrl}
                    alt={displayName}
                    className="h-8 w-8 flex-shrink-0 rounded-full object-cover"
                  />
                ) : (
                  <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-blue-deep to-teal text-xs font-medium text-white">
                    {initials}
                  </div>
                )}
                <div
                  className={cn(
                    "min-w-0 flex-1 overflow-hidden whitespace-nowrap transition-all duration-300 ease-out",
                    sidebarCollapsed ? "w-0 opacity-0" : "w-auto opacity-100"
                  )}
                >
                  <p className="truncate text-sm font-medium text-foreground">
                    {displayName}
                  </p>
                  <p className="truncate text-xs text-muted-foreground">
                    {user?.email || ""}
                  </p>
                </div>
              </Link>
              <button
                onClick={handleLogout}
                className={cn(
                  "text-muted-foreground transition-all duration-300 ease-out hover:text-red-500",
                  sidebarCollapsed ? "w-0 opacity-0" : "opacity-0 group-hover:opacity-100"
                )}
                aria-label={t("logout")}
                tabIndex={sidebarCollapsed ? -1 : 0}
              >
                <LogOut className="h-4 w-4" />
              </button>
            </div>
          </TooltipTrigger>
          {sidebarCollapsed && (
            <TooltipContent side="right">{displayName}</TooltipContent>
          )}
        </Tooltip>
      </div>
    </aside>
  );
}

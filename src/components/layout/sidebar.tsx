"use client";

import * as React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { ChevronLeft, ChevronRight, User, LogOut } from "lucide-react";
import { cn } from "@/lib/utils";
import { useUIStore } from "@/stores/ui-store";
import { mainNavigation, bottomNavigation } from "./navigation";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui";

export function Sidebar() {
  const pathname = usePathname();
  const { sidebarCollapsed, toggleSidebar } = useUIStore();

  const isActive = (href: string) => {
    if (href === "/dashboard") {
      return pathname === "/dashboard" || pathname === "/";
    }
    return pathname.startsWith(href);
  };

  return (
    <aside
      className={cn(
        "fixed left-0 top-0 bottom-0 z-30 flex flex-col border-r border-border bg-card transition-all duration-300",
        sidebarCollapsed ? "w-20" : "w-64"
      )}
      aria-label="Main navigation"
    >
      {/* Logo */}
      <div className="relative flex h-16 flex-shrink-0 items-center border-b border-border px-6">
        {!sidebarCollapsed ? (
          <Link
            href="/dashboard"
            className="flex items-center gap-2 rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            aria-label="GeneFlow home"
          >
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-gradient-to-br from-teal to-blue-deep">
              <span className="font-semibold text-white">GF</span>
            </div>
            <div className="flex flex-col">
              <span className="text-base font-semibold leading-none text-foreground">
                GeneFlow
              </span>
              <span className="mt-0.5 text-[10px] leading-none text-muted-foreground">
                Sequencing Platform
              </span>
            </div>
          </Link>
        ) : (
          <Link
            href="/dashboard"
            className="flex w-full items-center justify-center rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            aria-label="GeneFlow home"
          >
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-gradient-to-br from-teal to-blue-deep">
              <span className="font-semibold text-white">GF</span>
            </div>
          </Link>
        )}

        {/* Collapse Toggle */}
        <button
          onClick={toggleSidebar}
          className="absolute -right-3 top-1/2 flex h-6 w-6 -translate-y-1/2 items-center justify-center rounded-full border border-border bg-card shadow-sm transition-all duration-200 hover:scale-110 hover:bg-muted/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          aria-label={sidebarCollapsed ? "Expand sidebar" : "Collapse sidebar"}
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

          const linkContent = (
            <Link
              href={item.href}
              className={cn(
                "flex min-h-[40px] items-center gap-3 rounded-lg transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-card",
                sidebarCollapsed ? "justify-center px-3 py-2.5" : "px-4 py-2.5",
                active
                  ? "bg-gradient-to-r from-teal/10 to-blue-deep/5 text-teal shadow-sm"
                  : "text-muted-foreground hover:translate-x-0.5 hover:bg-muted/50 hover:text-foreground"
              )}
              aria-current={active ? "page" : undefined}
            >
              <Icon className="h-[18px] w-[18px] flex-shrink-0 transition-transform duration-200" />
              {!sidebarCollapsed && (
                <span className="text-sm font-medium">{item.name}</span>
              )}
            </Link>
          );

          if (sidebarCollapsed) {
            return (
              <Tooltip key={item.name} delayDuration={0}>
                <TooltipTrigger asChild>{linkContent}</TooltipTrigger>
                <TooltipContent side="right">{item.name}</TooltipContent>
              </Tooltip>
            );
          }

          return <React.Fragment key={item.name}>{linkContent}</React.Fragment>;
        })}
      </nav>

      {/* Bottom Navigation */}
      <div className="flex-shrink-0 space-y-1 border-t border-border p-4">
        {bottomNavigation.map((item) => {
          const Icon = item.icon;
          const active = isActive(item.href);

          const linkContent = (
            <Link
              href={item.href}
              className={cn(
                "flex min-h-[40px] w-full items-center gap-3 rounded-lg text-muted-foreground transition-all duration-200 hover:translate-x-0.5 hover:bg-muted/50 hover:text-foreground",
                sidebarCollapsed ? "justify-center px-3 py-2.5" : "px-4 py-2.5",
                active && "bg-muted/50 text-foreground"
              )}
            >
              <Icon className="h-[18px] w-[18px] flex-shrink-0 transition-transform duration-200" />
              {!sidebarCollapsed && (
                <span className="text-sm font-medium">{item.name}</span>
              )}
            </Link>
          );

          if (sidebarCollapsed) {
            return (
              <Tooltip key={item.name} delayDuration={0}>
                <TooltipTrigger asChild>{linkContent}</TooltipTrigger>
                <TooltipContent side="right">{item.name}</TooltipContent>
              </Tooltip>
            );
          }

          return <React.Fragment key={item.name}>{linkContent}</React.Fragment>;
        })}
      </div>

      {/* User Profile */}
      <div className="flex-shrink-0 border-t border-border p-4">
        {!sidebarCollapsed ? (
          <Link
            href="/profile"
            className="group flex items-center gap-3 rounded-lg bg-muted/30 px-3 py-2.5 transition-all hover:bg-muted/50"
          >
            <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-blue-deep to-teal">
              <User className="h-4 w-4 text-white" />
            </div>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium text-foreground">
                Dr. Sarah Martinez
              </p>
              <p className="truncate text-xs text-muted-foreground">
                Principal Investigator
              </p>
            </div>
            <button className="text-muted-foreground opacity-0 transition-colors hover:text-foreground group-hover:opacity-100">
              <LogOut className="h-4 w-4" />
            </button>
          </Link>
        ) : (
          <Tooltip delayDuration={0}>
            <TooltipTrigger asChild>
              <Link
                href="/profile"
                className="flex items-center justify-center rounded-lg bg-muted/30 px-3 py-2.5 transition-all hover:bg-muted/50"
              >
                <div className="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-blue-deep to-teal">
                  <User className="h-4 w-4 text-white" />
                </div>
              </Link>
            </TooltipTrigger>
            <TooltipContent side="right">Dr. Sarah Martinez</TooltipContent>
          </Tooltip>
        )}
      </div>
    </aside>
  );
}

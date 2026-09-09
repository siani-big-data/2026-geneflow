"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
import { Users } from "lucide-react";
import { Link, usePathname } from "@/lib/navigation";
import { OwnerAvatar } from "@/components/ui";
import { cn } from "@/lib/utils";
import type { Org } from "@/types";

export interface OrgHeaderProps {
  org: Org;
}

/**
 * Header strip rendered atop every org page. Shows the avatar, handle, name,
 * description and member count plus a tab strip for Overview/People/Settings.
 */
export function OrgHeader({ org }: OrgHeaderProps) {
  const t = useTranslations("orgs.header");
  const pathname = usePathname();

  const tabs = React.useMemo(
    () => [
      {
        key: "overview",
        label: t("overview"),
        href: `/orgs/${org.handle}`,
      },
      {
        key: "people",
        label: t("people"),
        href: `/orgs/${org.handle}/people`,
      },
    ],
    [org.handle, t],
  );

  const normalized = pathname.replace(/^\/(es|en)/, "");

  return (
    <header className="space-y-4">
      <div className="flex items-start gap-4">
        <OwnerAvatar
          owner={{
            type: "Org",
            handle: org.handle,
            displayName: org.name,
            avatarUrl: org.avatarUrl,
          }}
          size={64}
        />
        <div className="min-w-0 flex-1 space-y-1">
          <div className="flex items-center gap-2">
            <h1 className="truncate text-2xl font-semibold text-foreground">
              {org.name}
            </h1>
            <span className="text-sm text-muted-foreground">
              @{org.handle}
            </span>
          </div>
          {org.description && (
            <p className="text-sm text-muted-foreground">{org.description}</p>
          )}
          <div className="flex items-center gap-1.5 pt-1 text-xs text-muted-foreground">
            <Users className="h-3.5 w-3.5" />
            <span>{t("membersCount", { count: org.memberCount })}</span>
          </div>
        </div>
      </div>

      <nav
        aria-label="Organization sections"
        className="flex gap-1 border-b border-border"
      >
        {tabs.map((tab) => {
          const isActive =
            tab.key === "overview"
              ? normalized === `/orgs/${org.handle}`
              : normalized === tab.href;
          return (
            <Link
              key={tab.key}
              href={tab.href as never}
              className={cn(
                "border-b-2 px-3 py-2 text-sm font-medium transition-colors",
                isActive
                  ? "border-teal text-teal"
                  : "border-transparent text-muted-foreground hover:text-foreground",
              )}
              aria-current={isActive ? "page" : undefined}
            >
              {tab.label}
            </Link>
          );
        })}
      </nav>
    </header>
  );
}

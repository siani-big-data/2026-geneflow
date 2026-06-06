"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
import { Building2, Check, ChevronsUpDown, Plus, User } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  OwnerAvatar,
} from "@/components/ui";
import { useRouter } from "@/lib/navigation";
import { useActiveOrgStore } from "@/stores/active-org-store";
import { useMyOrgs } from "@/hooks/use-orgs";
import { useAuthStore } from "@/stores/auth-store";
import { cn } from "@/lib/utils";
import type { OwnerRef } from "@/types";

interface OrgSwitcherProps {
  /** Optional class for the trigger button. */
  className?: string;
  /** Render compact (icon-only) variant. */
  compact?: boolean;
}

/**
 * Sidebar/header combobox that switches between the user's personal context
 * and any org they belong to. Persists the choice via `useActiveOrgStore`
 * and navigates to the appropriate landing page on change.
 */
export function OrgSwitcher({ className, compact = false }: OrgSwitcherProps) {
  const t = useTranslations("orgs.switcher");
  const router = useRouter();
  const { activeContext, setActiveContext } = useActiveOrgStore();
  const { data: orgs = [] } = useMyOrgs();
  const { user, profile } = useAuthStore();

  const personalName = profile?.fullName || user?.username || "Personal";
  const personalAvatar =
    profile?.photoThumbnailUrl || profile?.photoUrl || undefined;

  const personalOwner: OwnerRef = {
    type: "User",
    handle: user?.username ?? "me",
    displayName: personalName,
    avatarUrl: personalAvatar,
  };

  const activeOrg =
    activeContext.type === "org"
      ? orgs.find((o) => o.handle === activeContext.handle)
      : undefined;

  const currentOwner: OwnerRef =
    activeContext.type === "org" && activeOrg
      ? {
          type: "Org",
          handle: activeOrg.handle,
          displayName: activeOrg.name,
          avatarUrl: activeOrg.avatarUrl,
        }
      : personalOwner;

  const currentLabel =
    activeContext.type === "org" && activeOrg
      ? activeOrg.name
      : t("personal");

  const handleSelectPersonal = () => {
    setActiveContext({ type: "personal" });
    router.push("/dashboard");
  };

  const handleSelectOrg = (handle: string) => {
    setActiveContext({ type: "org", handle });
    router.push(`/orgs/${handle}` as never);
  };

  const handleCreateNew = () => {
    router.push("/orgs/new" as never);
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          aria-label={t("label")}
          className={cn(
            "group flex w-full items-center gap-2 rounded-lg border border-border bg-card px-2.5 py-2 text-left text-sm transition-all duration-200 hover:bg-muted/60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
            compact && "justify-center px-2",
            className,
          )}
        >
          <OwnerAvatar owner={currentOwner} size={24} />
          {!compact && (
            <>
              <span className="min-w-0 flex-1 truncate text-sm font-medium text-foreground">
                {currentLabel}
              </span>
              <ChevronsUpDown className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
            </>
          )}
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-64">
        <DropdownMenuLabel className="text-xs uppercase tracking-wide text-muted-foreground">
          {t("label")}
        </DropdownMenuLabel>
        <DropdownMenuItem
          onClick={handleSelectPersonal}
          className="flex items-center gap-2"
        >
          <OwnerAvatar owner={personalOwner} size={24} />
          <span className="flex-1 truncate">{t("personal")}</span>
          {activeContext.type === "personal" && (
            <Check className="h-4 w-4 text-teal" />
          )}
        </DropdownMenuItem>
        {orgs.length > 0 && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuLabel className="flex items-center gap-1.5 text-xs uppercase tracking-wide text-muted-foreground">
              <Building2 className="h-3 w-3" />
              <span>Organizations</span>
            </DropdownMenuLabel>
            {orgs.map((org) => {
              const isActive =
                activeContext.type === "org" &&
                activeContext.handle === org.handle;
              return (
                <DropdownMenuItem
                  key={org.id}
                  onClick={() => handleSelectOrg(org.handle)}
                  className="flex items-center gap-2"
                >
                  <OwnerAvatar
                    owner={{
                      type: "Org",
                      handle: org.handle,
                      displayName: org.name,
                      avatarUrl: org.avatarUrl,
                    }}
                    size={24}
                  />
                  <span className="flex-1 truncate">{org.name}</span>
                  {isActive && <Check className="h-4 w-4 text-teal" />}
                </DropdownMenuItem>
              );
            })}
          </>
        )}
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onClick={handleCreateNew}
          className="flex items-center gap-2 text-teal"
        >
          <Plus className="h-4 w-4" />
          <span>{t("createNew")}</span>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/** Convenience export for callers that want the icon variant. */
export function PersonalContextIcon() {
  return <User className="h-4 w-4" />;
}

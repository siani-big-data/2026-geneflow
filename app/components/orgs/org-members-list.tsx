"use client";

import * as React from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Loader2, Trash2 } from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  AvatarImage,
  Badge,
  Button,
  ConfirmDialog,
} from "@/components/ui";
import {
  useChangeOrgMemberRole,
  useRemoveOrgMember,
} from "@/hooks/use-orgs";
import { useAuthStore } from "@/stores/auth-store";
import type { Org, OrgMember, OrgRole } from "@/types";
import { cn } from "@/lib/utils";

export interface OrgMembersListProps {
  org: Org;
  members: OrgMember[];
}

const ROLE_ORDER: OrgRole[] = ["Owner", "Admin", "Member"];

function RolePill({ role }: { role: OrgRole }) {
  const t = useTranslations("orgs.roles");
  const variant =
    role === "Owner"
      ? "default"
      : role === "Admin"
        ? "secondary"
        : "outline";
  const label =
    role === "Owner" ? t("owner") : role === "Admin" ? t("admin") : t("member");
  return <Badge variant={variant as "default" | "secondary" | "outline"}>{label}</Badge>;
}

function initialsFor(name: string): string {
  return name
    .split(/[\s._-]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("") || name.slice(0, 2).toUpperCase();
}

function UserCell({
  userId,
  userName,
  avatarUrl,
  isSelf,
  youLabel,
}: {
  userId: string;
  userName?: string | null;
  avatarUrl?: string | null;
  isSelf: boolean;
  youLabel: string;
}) {
  const displayName = userName?.trim() || `${userId.slice(0, 8)}…`;
  const initials = initialsFor(userName?.trim() || userId);
  const content = (
    <span className="flex items-center gap-2.5">
      <Avatar className="h-8 w-8">
        {avatarUrl ? <AvatarImage src={avatarUrl} alt={displayName} /> : null}
        <AvatarFallback className="text-xs">{initials}</AvatarFallback>
      </Avatar>
      <span className="text-sm font-medium text-foreground">{displayName}</span>
    </span>
  );
  return (
    <span className="inline-flex items-center gap-2">
      {userName ? (
        <Link
          href={`/users/${userName}`}
          className="hover:underline focus:outline-none focus-visible:underline"
        >
          {content}
        </Link>
      ) : (
        content
      )}
      {isSelf && (
        <span className="text-xs text-muted-foreground">({youLabel})</span>
      )}
    </span>
  );
}

/**
 * Table of org members. Owners/Admins can edit roles and remove members.
 * The current viewer cannot remove themselves if they are the sole Owner.
 */
export function OrgMembersList({ org, members }: OrgMembersListProps) {
  const t = useTranslations("orgs.members");
  const tCommon = useTranslations("common");
  const { user } = useAuthStore();
  const changeRole = useChangeOrgMemberRole(org.handle);
  const removeMember = useRemoveOrgMember(org.handle);

  const canManage = org.myRole === "Owner" || org.myRole === "Admin";

  const ownerCount = members.filter((m) => m.role === "Owner").length;

  const [pendingRemoveUserId, setPendingRemoveUserId] = React.useState<
    string | null
  >(null);

  const handleChangeRole = (userId: string, role: OrgRole) => {
    changeRole.mutate({ userId, role });
  };

  const handleRemove = (userId: string) => {
    setPendingRemoveUserId(userId);
  };

  const confirmRemove = async () => {
    if (!pendingRemoveUserId) return;
    try {
      await removeMember.mutateAsync(pendingRemoveUserId);
    } finally {
      setPendingRemoveUserId(null);
    }
  };

  if (members.length === 0) {
    return (
      <div className="rounded-lg border border-border bg-card p-8 text-center text-sm text-muted-foreground">
        {t("empty")}
      </div>
    );
  }

  return (
    <>
    <div className="overflow-hidden rounded-lg border border-border bg-card">
      <table className="w-full text-sm">
        <thead className="border-b border-border bg-muted/30 text-left text-xs uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="px-4 py-2.5 font-medium">{t("columns.user")}</th>
            <th className="px-4 py-2.5 font-medium">{t("columns.role")}</th>
            <th className="px-4 py-2.5 font-medium">{t("columns.joined")}</th>
            <th className="px-4 py-2.5" />
          </tr>
        </thead>
        <tbody>
          {members.map((m) => {
            const isSelf = m.userId === user?.id;
            const isLastOwner = m.role === "Owner" && ownerCount <= 1;
            return (
              <tr
                key={m.userId}
                className="border-b border-border last:border-0"
              >
                <td className="px-4 py-3">
                  <UserCell
                    userId={m.userId}
                    userName={m.userName}
                    avatarUrl={m.avatarUrl}
                    isSelf={isSelf}
                    youLabel={tCommon("yes") === "Yes" ? "you" : "tú"}
                  />
                </td>
                <td className="px-4 py-3">
                  {canManage && !isLastOwner ? (
                    <select
                      value={m.role}
                      onChange={(e) =>
                        handleChangeRole(m.userId, e.target.value as OrgRole)
                      }
                      disabled={changeRole.isPending}
                      className="rounded-md border border-border bg-background px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-teal/20"
                    >
                      {ROLE_ORDER.map((r) => (
                        <option key={r} value={r}>
                          {r}
                        </option>
                      ))}
                    </select>
                  ) : (
                    <RolePill role={m.role} />
                  )}
                </td>
                <td className="px-4 py-3 text-muted-foreground">
                  {new Date(m.joinedAt).toLocaleDateString()}
                </td>
                <td className="px-4 py-3 text-right">
                  {canManage && !(isSelf && isLastOwner) && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleRemove(m.userId)}
                      disabled={removeMember.isPending || isLastOwner}
                      title={isLastOwner ? t("lastOwnerError") : undefined}
                      className={cn(
                        "text-muted-foreground hover:text-red-500",
                        isLastOwner && "opacity-50",
                      )}
                    >
                      {removeMember.isPending ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Trash2 className="h-4 w-4" />
                      )}
                    </Button>
                  )}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
    <ConfirmDialog
      open={pendingRemoveUserId !== null}
      onOpenChange={(next) => {
        if (!next) setPendingRemoveUserId(null);
      }}
      title={t("confirmRemove")}
      variant="destructive"
      confirmLabel={tCommon("remove")}
      loading={removeMember.isPending}
      onConfirm={confirmRemove}
    />
    </>
  );
}

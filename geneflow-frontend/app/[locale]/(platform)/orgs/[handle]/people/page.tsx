"use client";

import * as React from "react";
import { useParams } from "next/navigation";
import { Loader2, UserPlus } from "lucide-react";
import { useTranslations } from "next-intl";
import {
  InviteMemberDialog,
  OrgHeader,
  OrgMembersList,
} from "@/components/orgs";
import { Button } from "@/components/ui";
import { useOrg, useOrgMembers } from "@/hooks/use-orgs";

export default function OrgPeoplePage() {
  const params = useParams<{ handle: string }>();
  const handle = params?.handle ?? "";
  const { data: org, isLoading: orgLoading } = useOrg(handle);
  const { data: members = [], isLoading: membersLoading } =
    useOrgMembers(handle);
  const t = useTranslations("orgs.members");
  const [inviteOpen, setInviteOpen] = React.useState(false);

  if (orgLoading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-teal" />
      </div>
    );
  }

  if (!org) {
    return (
      <div className="rounded-lg border border-border bg-card p-8 text-center text-sm text-muted-foreground">
        Failed to load organization.
      </div>
    );
  }

  const canInvite = org.myRole === "Owner" || org.myRole === "Admin";

  return (
    <div className="space-y-6">
      <OrgHeader org={org} />

      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold text-foreground">{t("title")}</h2>
        {canInvite && (
          <Button onClick={() => setInviteOpen(true)}>
            <UserPlus className="h-4 w-4" />
            {t("invite")}
          </Button>
        )}
      </div>

      {membersLoading ? (
        <div className="flex h-48 items-center justify-center">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      ) : (
        <OrgMembersList org={org} members={members} />
      )}

      {canInvite && (
        <InviteMemberDialog
          open={inviteOpen}
          onOpenChange={setInviteOpen}
          orgHandle={org.handle}
        />
      )}
    </div>
  );
}

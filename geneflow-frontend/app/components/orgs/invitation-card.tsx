"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
import { Check, Loader2, X } from "lucide-react";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui";
import {
  useAcceptInvitation,
  useDeclineInvitation,
} from "@/hooks/use-org-invitations";
import type { OrgInvitation } from "@/types";

export interface InvitationCardProps {
  invitation: OrgInvitation;
}

/**
 * Card showing a single pending org invitation with Accept/Decline actions.
 * Reused on `/invitations` and inside the dashboard `InvitationsBanner`.
 */
export function InvitationCard({ invitation }: InvitationCardProps) {
  const t = useTranslations("orgs.invitations");
  const tRoles = useTranslations("orgs.roles");
  const accept = useAcceptInvitation();
  const decline = useDeclineInvitation();

  const expires = React.useMemo(
    () => new Date(invitation.expiresAt).toLocaleDateString(),
    [invitation.expiresAt],
  );

  const roleLabel =
    invitation.role === "Owner"
      ? tRoles("owner")
      : invitation.role === "Admin"
        ? tRoles("admin")
        : tRoles("member");

  const handleAccept = () => accept.mutate(invitation.token);
  const handleDecline = () => decline.mutate(invitation.token);

  const busy = accept.isPending || decline.isPending;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0 flex-1">
            <CardTitle className="truncate">
              {t("invitedToOrg")}{" "}
              <span className="text-teal">@{invitation.orgHandle}</span>
            </CardTitle>
            <CardDescription className="mt-1">
              {invitation.orgName}
            </CardDescription>
          </div>
          <Badge variant="secondary">{roleLabel}</Badge>
        </div>
      </CardHeader>
      <CardContent>
        <p className="text-xs text-muted-foreground">
          {t("expiresIn")}: {expires}
        </p>
      </CardContent>
      <CardFooter className="gap-2">
        <Button
          variant="ghost"
          onClick={handleDecline}
          disabled={busy}
          aria-label={t("decline")}
        >
          {decline.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <>
              <X className="h-4 w-4" />
              <span className="ml-1">{t("decline")}</span>
            </>
          )}
        </Button>
        <Button
          onClick={handleAccept}
          disabled={busy}
          aria-label={t("accept")}
        >
          {accept.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <>
              <Check className="h-4 w-4" />
              <span className="ml-1">{t("accept")}</span>
            </>
          )}
        </Button>
      </CardFooter>
    </Card>
  );
}

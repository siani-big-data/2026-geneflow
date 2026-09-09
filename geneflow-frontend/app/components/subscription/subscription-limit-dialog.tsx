"use client";

import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Button } from "@/components/ui";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useSubscriptionLimitStore } from "@/stores/subscription-limit-store";

/**
 * Global dialog that surfaces backend `SubscriptionLimitBehavior` rejections
 * (no active subscription, study/trace/member limit reached) with a single,
 * consistent CTA toward the billing page.
 *
 * Driven by `useSubscriptionLimitStore`, fed by the QueryProvider's
 * MutationCache.onError. Must be mounted exactly once near the root of the
 * authenticated app.
 */
export function SubscriptionLimitDialog() {
  const t = useTranslations("subscriptionLimit");
  const locale = useLocale();
  const router = useRouter();
  const { isOpen, error, close } = useSubscriptionLimitStore();

  const codeKey = error?.code.replace("Subscription.", "") ?? "";
  const fallbackMessage = error?.message ?? "";

  // Try the i18n entry per-code first; fall back to the server message.
  const title = codeKey ? t(`codes.${codeKey}.title`) : t("title");
  const description = codeKey
    ? // next-intl will throw on missing keys; we use a default through the
      // server-provided message so any new backend code degrades gracefully.
      tOrFallback(t, `codes.${codeKey}.description`, fallbackMessage)
    : fallbackMessage;

  const handleUpgrade = () => {
    close();
    router.push(`/${locale}/settings/billing`);
  };

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) close();
      }}
    >
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={close}>
            {t("close")}
          </Button>
          <Button onClick={handleUpgrade}>{t("viewPlans")}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

/**
 * `useTranslations` throws on missing keys. Wrap each lookup so a brand-new
 * backend code (added before its FE translation lands) shows the server's
 * human-readable message instead of crashing the dialog.
 */
function tOrFallback(
  t: ReturnType<typeof useTranslations>,
  key: string,
  fallback: string,
): string {
  try {
    return t(key);
  } catch {
    return fallback;
  }
}

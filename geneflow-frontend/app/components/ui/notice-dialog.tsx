"use client";

import * as React from "react";
import { useTranslations } from "next-intl";
import { AlertCircle, CheckCircle2, Info } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "./dialog";
import { Button } from "./button";
import { cn } from "@/lib/utils";

export type NoticeVariant = "info" | "success" | "error";

export interface NoticeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: React.ReactNode;
  okLabel?: string;
  variant?: NoticeVariant;
}

const variantIcon: Record<NoticeVariant, React.ElementType> = {
  info: Info,
  success: CheckCircle2,
  error: AlertCircle,
};

const variantColor: Record<NoticeVariant, string> = {
  info: "text-blue-500",
  success: "text-emerald-500",
  error: "text-red-500",
};

/**
 * Generic notice modal — drop-in replacement for `window.alert`.
 * Single OK button. Use the variant prop for visual emphasis on success
 * or error.
 */
export function NoticeDialog({
  open,
  onOpenChange,
  title,
  description,
  okLabel,
  variant = "info",
}: NoticeDialogProps) {
  const tCommon = useTranslations("common");
  const Icon = variantIcon[variant];

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[440px]">
        <DialogHeader>
          <div className="flex items-start gap-3">
            <Icon
              className={cn("mt-0.5 h-5 w-5 shrink-0", variantColor[variant])}
            />
            <div className="space-y-1.5">
              <DialogTitle>{title}</DialogTitle>
              {description ? (
                <DialogDescription>{description}</DialogDescription>
              ) : null}
            </div>
          </div>
        </DialogHeader>
        <DialogFooter>
          <Button type="button" onClick={() => onOpenChange(false)}>
            {okLabel ?? tCommon("ok")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

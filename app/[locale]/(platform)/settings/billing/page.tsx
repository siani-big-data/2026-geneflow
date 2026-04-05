"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import {
  ArrowLeft,
  CreditCard,
  Download,
  Check,
  AlertTriangle,
  Zap,
  HardDrive,
  Users,
  BarChart3,
  Building2,
  ExternalLink,
  ChevronRight,
} from "lucide-react";
import { cn } from "@/lib/utils";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Button,
} from "@/components/ui";

const currentPlanData = {
  name: "Professional",
  price: 49,
  nextBillingDate: "2026-05-03",
  status: "active",
};

const usageData = {
  billingPeriod: { start: "Apr 3, 2026", end: "May 3, 2026" },
  samples: { used: 2847, total: 5000 },
  storage: { used: 45.2, total: 100 },
  pipelines: { used: 23, total: 50 },
  collaborators: { used: 7, total: 10 },
};

const paymentMethods = [
  { id: 1, type: "visa", last4: "4242", expiry: "12/27", isDefault: true },
];

const invoices = [
  { id: "INV-2026-004", date: "Apr 3, 2026", amount: 49.00, status: "paid" },
  { id: "INV-2026-003", date: "Mar 3, 2026", amount: 49.00, status: "paid" },
  { id: "INV-2026-002", date: "Feb 3, 2026", amount: 49.00, status: "paid" },
  { id: "INV-2026-001", date: "Jan 3, 2026", amount: 49.00, status: "paid" },
];

const billingInfo = {
  name: "Dr. Sarah Martinez",
  email: "billing@stanford.edu",
  address: "450 Serra Mall, Stanford, CA 94305",
  taxId: "US-123456789",
};

export default function BillingPage() {
  const t = useTranslations("billingPage");
  const tCommon = useTranslations("common");
  const [changePlanOpen, setChangePlanOpen] = useState(false);
  const [cancelSubOpen, setCancelSubOpen] = useState(false);
  const [addPaymentOpen, setAddPaymentOpen] = useState(false);
  const [editBillingOpen, setEditBillingOpen] = useState(false);

  const usagePercent = (used: number, total: number) => Math.round((used / total) * 100);

  return (
    <div className="-mx-16 -mt-10 min-h-full bg-background">
      {/* Header */}
      <div className="border-b border-border bg-card">
        <div className="mx-auto max-w-[1200px] px-8 py-6">
          <Link
            href="/settings"
            className="mb-4 inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" />
            {t("backToSettings")}
          </Link>
          <h1 className="text-2xl font-semibold text-foreground">{t("title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("description")}</p>
        </div>
      </div>

      <div className="mx-auto max-w-[1200px] px-8 py-8">
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          {/* Main Content - 2 columns */}
          <div className="space-y-6 lg:col-span-2">
            {/* Current Plan */}
            <div className="rounded-xl border border-border bg-card p-6">
              <div className="mb-6 flex items-start justify-between">
                <div>
                  <h2 className="text-lg font-semibold text-foreground">{t("currentPlan.title")}</h2>
                  <div className="mt-2 flex items-baseline gap-2">
                    <span className="text-3xl font-bold text-foreground">{t("currentPlan.price")}</span>
                    <span className="text-muted-foreground">{t("currentPlan.perMonth")}</span>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <span className="rounded-full bg-teal/10 px-3 py-1 text-sm font-medium text-teal">
                    {t("currentPlan.plan")}
                  </span>
                  <span className="rounded-full bg-emerald-500/10 px-3 py-1 text-sm font-medium text-emerald-500">
                    {t("currentPlan.status")}
                  </span>
                </div>
              </div>
              <p className="mb-4 text-sm text-muted-foreground">
                {t("currentPlan.renewsOn", { date: new Date(currentPlanData.nextBillingDate).toLocaleDateString() })}
              </p>
              <div className="flex gap-3">
                <Button variant="outline" onClick={() => setChangePlanOpen(true)}>
                  {t("currentPlan.changePlan")}
                </Button>
                <Button
                  variant="ghost"
                  className="text-destructive hover:text-destructive"
                  onClick={() => setCancelSubOpen(true)}
                >
                  {t("currentPlan.cancelSubscription")}
                </Button>
              </div>
            </div>

            {/* Usage */}
            <div className="rounded-xl border border-border bg-card p-6">
              <div className="mb-6 flex items-center justify-between">
                <h2 className="text-lg font-semibold text-foreground">{t("usage.title")}</h2>
                <span className="text-sm text-muted-foreground">
                  {t("usage.period")}: {usageData.billingPeriod.start} - {usageData.billingPeriod.end}
                </span>
              </div>
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                {/* Samples */}
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Zap className="h-4 w-4 text-teal" />
                      <span className="text-sm font-medium text-foreground">{t("usage.samples.title")}</span>
                    </div>
                    <span className="text-sm text-muted-foreground">
                      {t("usage.samples.used", { used: usageData.samples.used.toLocaleString(), total: usageData.samples.total.toLocaleString() })}
                    </span>
                  </div>
                  <div className="h-2 overflow-hidden rounded-full bg-muted">
                    <div
                      className="h-full rounded-full bg-teal transition-all"
                      style={{ width: `${usagePercent(usageData.samples.used, usageData.samples.total)}%` }}
                    />
                  </div>
                </div>

                {/* Storage */}
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <HardDrive className="h-4 w-4 text-blue-deep" />
                      <span className="text-sm font-medium text-foreground">{t("usage.storage.title")}</span>
                    </div>
                    <span className="text-sm text-muted-foreground">
                      {t("usage.storage.used", { used: usageData.storage.used, total: usageData.storage.total })}
                    </span>
                  </div>
                  <div className="h-2 overflow-hidden rounded-full bg-muted">
                    <div
                      className="h-full rounded-full bg-blue-deep transition-all"
                      style={{ width: `${usagePercent(usageData.storage.used, usageData.storage.total)}%` }}
                    />
                  </div>
                </div>

                {/* Pipelines */}
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <BarChart3 className="h-4 w-4 text-teal" />
                      <span className="text-sm font-medium text-foreground">{t("usage.pipelines.title")}</span>
                    </div>
                    <span className="text-sm text-muted-foreground">
                      {t("usage.pipelines.used", { used: usageData.pipelines.used, total: usageData.pipelines.total })}
                    </span>
                  </div>
                  <div className="h-2 overflow-hidden rounded-full bg-muted">
                    <div
                      className="h-full rounded-full bg-teal transition-all"
                      style={{ width: `${usagePercent(usageData.pipelines.used, usageData.pipelines.total)}%` }}
                    />
                  </div>
                </div>

                {/* Collaborators */}
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Users className="h-4 w-4 text-blue-deep" />
                      <span className="text-sm font-medium text-foreground">{t("usage.collaborators.title")}</span>
                    </div>
                    <span className="text-sm text-muted-foreground">
                      {t("usage.collaborators.used", { used: usageData.collaborators.used, total: usageData.collaborators.total })}
                    </span>
                  </div>
                  <div className="h-2 overflow-hidden rounded-full bg-muted">
                    <div
                      className="h-full rounded-full bg-blue-deep transition-all"
                      style={{ width: `${usagePercent(usageData.collaborators.used, usageData.collaborators.total)}%` }}
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* Billing History */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
              <div className="flex items-center justify-between border-b border-border p-6">
                <h2 className="text-lg font-semibold text-foreground">{t("billingHistory.title")}</h2>
                <Button variant="ghost" size="sm">
                  <Download className="mr-2 h-4 w-4" />
                  {t("billingHistory.downloadAll")}
                </Button>
              </div>
              <div className="divide-y divide-border">
                <div className="grid grid-cols-4 gap-4 bg-muted/30 px-6 py-3 text-xs font-medium uppercase tracking-wider text-muted-foreground">
                  <span>{t("billingHistory.invoice")}</span>
                  <span>{t("billingHistory.date")}</span>
                  <span>{t("billingHistory.amount")}</span>
                  <span>{t("billingHistory.status")}</span>
                </div>
                {invoices.map((invoice) => (
                  <div key={invoice.id} className="grid grid-cols-4 items-center gap-4 px-6 py-4">
                    <span className="text-sm font-medium text-foreground">{invoice.id}</span>
                    <span className="text-sm text-muted-foreground">{invoice.date}</span>
                    <span className="text-sm text-foreground">${invoice.amount.toFixed(2)}</span>
                    <div className="flex items-center justify-between">
                      <span className={cn(
                        "rounded-full px-2.5 py-0.5 text-xs font-medium",
                        invoice.status === "paid" && "bg-emerald-500/10 text-emerald-500",
                        invoice.status === "pending" && "bg-yellow-500/10 text-yellow-500",
                        invoice.status === "failed" && "bg-destructive/10 text-destructive"
                      )}>
                        {t(`billingHistory.${invoice.status}`)}
                      </span>
                      <button className="text-sm text-teal hover:text-teal/80">
                        <Download className="h-4 w-4" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
              <div className="border-t border-border p-4 text-center">
                <button className="text-sm font-medium text-teal hover:text-teal/80">
                  {t("billingHistory.viewAll")}
                </button>
              </div>
            </div>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Payment Method */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h3 className="mb-4 text-base font-semibold text-foreground">{t("paymentMethod.title")}</h3>
              {paymentMethods.map((method) => (
                <div key={method.id} className="flex items-center gap-4 rounded-lg border border-border p-4">
                  <div className="flex h-10 w-14 items-center justify-center rounded bg-muted">
                    <CreditCard className="h-5 w-5 text-muted-foreground" />
                  </div>
                  <div className="flex-1">
                    <p className="text-sm font-medium text-foreground">
                      {t("paymentMethod.cardEnding", { last4: method.last4 })}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {t("paymentMethod.expires", { date: method.expiry })}
                    </p>
                  </div>
                  {method.isDefault && (
                    <span className="rounded bg-teal/10 px-2 py-0.5 text-xs font-medium text-teal">
                      {t("paymentMethod.default")}
                    </span>
                  )}
                </div>
              ))}
              <div className="mt-4 flex gap-2">
                <Button variant="outline" size="sm" className="flex-1" onClick={() => setAddPaymentOpen(true)}>
                  {t("paymentMethod.addNew")}
                </Button>
                <Button variant="ghost" size="sm">
                  {t("paymentMethod.update")}
                </Button>
              </div>
            </div>

            {/* Billing Info */}
            <div className="rounded-xl border border-border bg-card p-6">
              <div className="mb-4 flex items-center justify-between">
                <h3 className="text-base font-semibold text-foreground">{t("billingInfo.title")}</h3>
                <button
                  onClick={() => setEditBillingOpen(true)}
                  className="text-sm font-medium text-teal hover:text-teal/80"
                >
                  {t("billingInfo.edit")}
                </button>
              </div>
              <div className="space-y-4 text-sm">
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("billingInfo.name")}</p>
                  <p className="text-foreground">{billingInfo.name}</p>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("billingInfo.email")}</p>
                  <p className="text-foreground">{billingInfo.email}</p>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("billingInfo.address")}</p>
                  <p className="text-foreground">{billingInfo.address}</p>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("billingInfo.taxId")}</p>
                  <p className="text-foreground">{billingInfo.taxId}</p>
                </div>
              </div>
            </div>

            {/* Upgrade CTA */}
            <div className="rounded-xl border border-teal/30 bg-gradient-to-br from-teal/5 to-blue-deep/5 p-6">
              <div className="mb-3 flex items-center gap-2">
                <Building2 className="h-5 w-5 text-teal" />
                <h3 className="font-semibold text-foreground">{t("plans.enterprise.name")}</h3>
              </div>
              <p className="mb-4 text-sm text-muted-foreground">{t("plans.enterprise.description")}</p>
              <Button className="w-full">
                {t("plans.enterprise.contact")}
                <ExternalLink className="ml-2 h-4 w-4" />
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Change Plan Dialog */}
      <Dialog open={changePlanOpen} onOpenChange={setChangePlanOpen}>
        <DialogContent className="sm:max-w-[600px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.changePlan.title")}</DialogTitle>
            <DialogDescription>{t("dialogs.changePlan.description")}</DialogDescription>
          </DialogHeader>
          <div className="grid grid-cols-3 gap-4 py-4">
            {/* Starter */}
            <div className="rounded-lg border border-border p-4">
              <h4 className="font-semibold text-foreground">{t("plans.starter.name")}</h4>
              <p className="mb-2 text-2xl font-bold text-foreground">{t("plans.starter.price")}</p>
              <p className="mb-4 text-xs text-muted-foreground">{t("plans.starter.description")}</p>
              <ul className="space-y-2 text-xs text-muted-foreground">
                <li className="flex items-center gap-1.5">
                  <Check className="h-3 w-3 text-teal" />
                  {t("plans.starter.features.samples")}
                </li>
                <li className="flex items-center gap-1.5">
                  <Check className="h-3 w-3 text-teal" />
                  {t("plans.starter.features.storage")}
                </li>
              </ul>
            </div>

            {/* Professional (Current) */}
            <div className="rounded-lg border-2 border-teal bg-teal/5 p-4">
              <div className="mb-2 flex items-center justify-between">
                <h4 className="font-semibold text-foreground">{t("plans.professional.name")}</h4>
                <span className="rounded bg-teal px-1.5 py-0.5 text-xs text-white">{t("plans.professional.current")}</span>
              </div>
              <p className="mb-2 text-2xl font-bold text-foreground">{t("plans.professional.price")}<span className="text-sm font-normal text-muted-foreground">/mo</span></p>
              <p className="mb-4 text-xs text-muted-foreground">{t("plans.professional.description")}</p>
              <ul className="space-y-2 text-xs text-muted-foreground">
                <li className="flex items-center gap-1.5">
                  <Check className="h-3 w-3 text-teal" />
                  {t("plans.professional.features.samples")}
                </li>
                <li className="flex items-center gap-1.5">
                  <Check className="h-3 w-3 text-teal" />
                  {t("plans.professional.features.storage")}
                </li>
              </ul>
            </div>

            {/* Enterprise */}
            <div className="rounded-lg border border-border p-4">
              <h4 className="font-semibold text-foreground">{t("plans.enterprise.name")}</h4>
              <p className="mb-2 text-2xl font-bold text-foreground">{t("plans.enterprise.price")}</p>
              <p className="mb-4 text-xs text-muted-foreground">{t("plans.enterprise.description")}</p>
              <ul className="space-y-2 text-xs text-muted-foreground">
                <li className="flex items-center gap-1.5">
                  <Check className="h-3 w-3 text-teal" />
                  {t("plans.enterprise.features.samples")}
                </li>
                <li className="flex items-center gap-1.5">
                  <Check className="h-3 w-3 text-teal" />
                  {t("plans.enterprise.features.sla")}
                </li>
              </ul>
            </div>
          </div>
          <p className="text-xs text-muted-foreground">{t("dialogs.changePlan.prorate")}</p>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setChangePlanOpen(false)}>{tCommon("cancel")}</Button>
            <Button onClick={() => setChangePlanOpen(false)}>{t("dialogs.changePlan.confirm")}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Cancel Subscription Dialog */}
      <Dialog open={cancelSubOpen} onOpenChange={setCancelSubOpen}>
        <DialogContent className="sm:max-w-[480px]">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-destructive" />
              {t("dialogs.cancelSubscription.title")}
            </DialogTitle>
            <DialogDescription>{t("dialogs.cancelSubscription.description")}</DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <p className="mb-3 text-sm text-foreground">{t("dialogs.cancelSubscription.warning")}</p>
            <ul className="space-y-2 text-sm text-muted-foreground">
              <li className="flex items-center gap-2">
                <ChevronRight className="h-4 w-4 text-destructive" />
                {t("dialogs.cancelSubscription.loseAccess.storage")}
              </li>
              <li className="flex items-center gap-2">
                <ChevronRight className="h-4 w-4 text-destructive" />
                {t("dialogs.cancelSubscription.loseAccess.pipelines")}
              </li>
              <li className="flex items-center gap-2">
                <ChevronRight className="h-4 w-4 text-destructive" />
                {t("dialogs.cancelSubscription.loseAccess.collaborators")}
              </li>
              <li className="flex items-center gap-2">
                <ChevronRight className="h-4 w-4 text-destructive" />
                {t("dialogs.cancelSubscription.loseAccess.support")}
              </li>
            </ul>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCancelSubOpen(false)}>
              {t("dialogs.cancelSubscription.keepPlan")}
            </Button>
            <Button variant="destructive" onClick={() => setCancelSubOpen(false)}>
              {t("dialogs.cancelSubscription.confirm")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Add Payment Method Dialog */}
      <Dialog open={addPaymentOpen} onOpenChange={setAddPaymentOpen}>
        <DialogContent className="sm:max-w-[700px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.addPayment.title")}</DialogTitle>
            <DialogDescription>{t("dialogs.addPayment.description")}</DialogDescription>
          </DialogHeader>
          <div className="grid grid-cols-1 gap-8 py-6 md:grid-cols-2">
            {/* 3D Credit Card Preview */}
            <div className="flex items-center justify-center">
              <div
                className="group h-[200px] w-[320px]"
                style={{ perspective: "1000px" }}
              >
                <div
                  className="relative h-full w-full transition-transform duration-700"
                  style={{
                    transformStyle: "preserve-3d",
                  }}
                  onMouseEnter={(e) => e.currentTarget.style.transform = "rotateY(180deg)"}
                  onMouseLeave={(e) => e.currentTarget.style.transform = "rotateY(0deg)"}
                >
                  {/* Front of Card */}
                  <div
                    className="absolute inset-0 rounded-2xl bg-gradient-to-br from-teal via-teal/90 to-blue-deep p-6 shadow-xl"
                    style={{ backfaceVisibility: "hidden" }}
                  >
                    {/* Chip */}
                    <div className="mb-6 flex items-center justify-between">
                      <div className="h-10 w-14 rounded-md bg-gradient-to-br from-yellow-300 via-yellow-400 to-yellow-500 p-1">
                        <div className="h-full w-full rounded border border-yellow-600/30 bg-gradient-to-br from-yellow-200 to-yellow-400" />
                      </div>
                      <div className="text-xl font-bold tracking-wider text-white/90">VISA</div>
                    </div>
                    {/* Card Number */}
                    <div className="mb-4">
                      <p className="font-mono text-xl tracking-[0.2em] text-white">
                        1234 5678 9012 3456
                      </p>
                    </div>
                    {/* Card Details */}
                    <div className="flex items-end justify-between">
                      <div>
                        <p className="mb-1 text-[10px] uppercase tracking-wider text-white/60">Card Holder</p>
                        <p className="font-mono text-sm uppercase tracking-wider text-white">SARAH MARTINEZ</p>
                      </div>
                      <div>
                        <p className="mb-1 text-[10px] uppercase tracking-wider text-white/60">Expires</p>
                        <p className="font-mono text-sm tracking-wider text-white">12/27</p>
                      </div>
                    </div>
                  </div>

                  {/* Back of Card */}
                  <div
                    className="absolute inset-0 rounded-2xl bg-gradient-to-br from-slate-700 via-slate-800 to-slate-900 shadow-xl"
                    style={{
                      backfaceVisibility: "hidden",
                      transform: "rotateY(180deg)"
                    }}
                  >
                    {/* Magnetic Strip */}
                    <div className="mt-6 h-12 w-full bg-slate-950" />
                    {/* Signature Strip & CVV */}
                    <div className="mt-6 px-6">
                      <div className="flex items-center gap-4">
                        <div className="h-10 flex-1 rounded bg-gradient-to-r from-slate-200 to-slate-300" />
                        <div className="flex h-10 w-16 items-center justify-center rounded bg-white">
                          <span className="font-mono text-sm font-bold text-slate-800">123</span>
                        </div>
                      </div>
                      <p className="mt-2 text-right text-[10px] uppercase tracking-wider text-white/60">CVC</p>
                    </div>
                    {/* Info Text */}
                    <div className="mt-6 px-6">
                      <p className="text-[9px] leading-relaxed text-white/40">
                        This card is property of GeneFlow Bank. If found, please return to any GeneFlow branch.
                        Unauthorized use is prohibited.
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Form */}
            <div className="space-y-4">
              <div className="space-y-2">
                <label className="text-sm font-medium text-foreground">{t("dialogs.addPayment.cardNumber")}</label>
                <input
                  type="text"
                  placeholder="1234 5678 9012 3456"
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium text-foreground">{t("dialogs.addPayment.nameOnCard")}</label>
                <input
                  type="text"
                  placeholder="SARAH MARTINEZ"
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm uppercase transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="text-sm font-medium text-foreground">{t("dialogs.addPayment.expiry")}</label>
                  <input
                    type="text"
                    placeholder="MM/YY"
                    className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                  />
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-medium text-foreground">
                    {t("dialogs.addPayment.cvc")}
                    <span className="ml-1 text-xs text-muted-foreground">(hover card)</span>
                  </label>
                  <input
                    type="text"
                    placeholder="123"
                    className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                  />
                </div>
              </div>
              <label className="flex items-center gap-2 pt-2">
                <input type="checkbox" className="rounded border-border" />
                <span className="text-sm text-muted-foreground">{t("dialogs.addPayment.setDefault")}</span>
              </label>
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setAddPaymentOpen(false)}>{tCommon("cancel")}</Button>
            <Button onClick={() => setAddPaymentOpen(false)}>{t("dialogs.addPayment.add")}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Billing Info Dialog */}
      <Dialog open={editBillingOpen} onOpenChange={setEditBillingOpen}>
        <DialogContent className="sm:max-w-[480px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.editBillingInfo.title")}</DialogTitle>
            <DialogDescription>{t("dialogs.editBillingInfo.description")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("billingInfo.name")}</label>
              <input
                type="text"
                defaultValue={billingInfo.name}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("billingInfo.email")}</label>
              <input
                type="email"
                defaultValue={billingInfo.email}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("billingInfo.address")}</label>
              <textarea
                rows={2}
                defaultValue={billingInfo.address}
                className="w-full resize-none rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("billingInfo.taxId")}</label>
              <input
                type="text"
                defaultValue={billingInfo.taxId}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setEditBillingOpen(false)}>{tCommon("cancel")}</Button>
            <Button onClick={() => setEditBillingOpen(false)}>{t("dialogs.editBillingInfo.save")}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

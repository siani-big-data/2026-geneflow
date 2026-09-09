"use client";

import { useState, useEffect } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import {
  ArrowLeft,
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
  Loader2,
  AlertCircle,
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
import { PaymentMethodList } from "@/components/payment";
import { planService, subscriptionService, usageService } from "@/services";
import type { Plan, Subscription, BillingCycle, BillingUsage } from "@/types";
import { BillingCycleId } from "@/types";
import { useAuthStore } from "@/stores/auth-store";

export default function BillingPage() {
  const t = useTranslations("billingPage");
  const tCommon = useTranslations("common");
  const { profile, user } = useAuthStore();

  // Data state
  const [subscription, setSubscription] = useState<Subscription | null>(null);
  const [plans, setPlans] = useState<Plan[]>([]);
  const [currentPlan, setCurrentPlan] = useState<Plan | null>(null);
  const [billingUsage, setBillingUsage] = useState<BillingUsage | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Dialog state
  const [changePlanOpen, setChangePlanOpen] = useState(false);
  const [cancelSubOpen, setCancelSubOpen] = useState(false);
  const [editBillingOpen, setEditBillingOpen] = useState(false);

  // Action state
  const [selectedPlanId, setSelectedPlanId] = useState<string | null>(null);
  const [selectedBillingCycle, setSelectedBillingCycle] = useState<BillingCycle>("Monthly");
  const [isChangingPlan, setIsChangingPlan] = useState(false);
  const [isCancelling, setIsCancelling] = useState(false);
  const [cancellationReason, setCancellationReason] = useState("");

  // Fetch subscription, plans, and usage data
  useEffect(() => {
    async function fetchData() {
      setIsLoading(true);
      setError(null);

      try {
        const [subscriptionData, plansData] = await Promise.all([
          subscriptionService.getCurrent(),
          planService.getAll(),
        ]);

        setSubscription(subscriptionData);
        setPlans(plansData.sort((a, b) => a.displayOrder - b.displayOrder));

        // Find current plan
        if (subscriptionData) {
          const plan = plansData.find((p) => p.id === subscriptionData.planId);
          setCurrentPlan(plan || null);

          // Fetch usage data if there's an active subscription
          try {
            const usageData = await usageService.getBillingUsage();
            setBillingUsage(usageData);
          } catch (usageErr) {
            console.error("Failed to fetch usage data:", usageErr);
            // Don't fail the whole page for usage errors - show subscription without usage
          }
        }
      } catch (err) {
        console.error("Failed to fetch billing data:", err);
        setError(err instanceof Error ? err.message : "Failed to load billing data");
      } finally {
        setIsLoading(false);
      }
    }

    fetchData();
  }, []);

  // Calculate usage percentages
  const usagePercent = (used: number, total: number) =>
    total > 0 ? Math.min(Math.round((used / total) * 100), 100) : 0;

  // Get price for display
  const getPrice = (plan: Plan, cycle: BillingCycle = "Monthly") => {
    return cycle === "Monthly" ? plan.pricing.monthlyPrice : plan.pricing.annualPrice;
  };

  // The backend uses -1 as the sentinel for "no limit"; show it as unlimited
  // instead of leaking the raw -1 into the UI.
  const formatLimit = (value: number): string => (value < 0 ? "∞" : String(value));

  // Format date
  const formatDate = (dateString: string | null | undefined) => {
    if (!dateString) return "-";
    return new Date(dateString).toLocaleDateString();
  };

  // Handle plan change
  const handleChangePlan = async () => {
    if (!selectedPlanId) return;

    setIsChangingPlan(true);
    try {
      const newSubscription = await subscriptionService.changePlan({
        newPlanId: selectedPlanId,
        billingCycleId: BillingCycleId[selectedBillingCycle],
      });
      setSubscription(newSubscription);

      // Update current plan
      const plan = plans.find((p) => p.id === newSubscription.planId);
      setCurrentPlan(plan || null);

      // Refresh billing usage to get updated limits from new plan
      try {
        const updatedUsage = await usageService.getBillingUsage();
        setBillingUsage(updatedUsage);
      } catch (usageErr) {
        console.error("Failed to refresh usage data:", usageErr);
      }

      setChangePlanOpen(false);
      setSelectedPlanId(null);
    } catch (err) {
      console.error("Failed to change plan:", err);
      setError(err instanceof Error ? err.message : "Failed to change plan");
    } finally {
      setIsChangingPlan(false);
    }
  };

  // Handle subscription cancellation
  const handleCancelSubscription = async () => {
    setIsCancelling(true);
    try {
      await subscriptionService.cancel({
        reason: cancellationReason || undefined,
      });

      // Refresh subscription data
      const updatedSubscription = await subscriptionService.getCurrent();
      setSubscription(updatedSubscription);

      setCancelSubOpen(false);
      setCancellationReason("");
    } catch (err) {
      console.error("Failed to cancel subscription:", err);
      setError(err instanceof Error ? err.message : "Failed to cancel subscription");
    } finally {
      setIsCancelling(false);
    }
  };

  // Loading state
  if (isLoading) {
    return (
      <div className="-mx-16 -mt-10 flex min-h-[400px] items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="h-8 w-8 animate-spin text-teal" />
          <p className="text-sm text-muted-foreground">{tCommon("loading")}</p>
        </div>
      </div>
    );
  }

  // Error state
  if (error && !subscription && plans.length === 0) {
    return (
      <div className="-mx-16 -mt-10 flex min-h-[400px] items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-3 text-center">
          <AlertCircle className="h-8 w-8 text-destructive" />
          <p className="text-sm text-destructive">{error}</p>
          <Button variant="outline" onClick={() => window.location.reload()}>
            {tCommon("retry")}
          </Button>
        </div>
      </div>
    );
  }

  // Use billing usage data from API, fallback to subscription period if no usage data
  const usageData = currentPlan && billingUsage
    ? {
        billingPeriod: {
          start: formatDate(billingUsage.period.startDate),
          end: formatDate(billingUsage.period.endDate),
        },
        studies: billingUsage.studies,
        traces: billingUsage.traces,
        members: billingUsage.members,
        daysRemaining: billingUsage.period.daysRemaining,
        totalDays: billingUsage.period.totalDays,
      }
    : currentPlan && subscription
    ? {
        billingPeriod: {
          start: formatDate(subscription.currentPeriod.startDate),
          end: formatDate(subscription.currentPeriod.endDate),
        },
        studies: { used: 0, total: currentPlan.limits.maxStudies, percentage: 0 },
        traces: { used: 0, total: currentPlan.limits.maxTracesPerMonth, percentage: 0 },
        members: { used: 0, total: currentPlan.limits.maxMembersPerStudy, percentage: 0 },
        daysRemaining: subscription.currentPeriod.daysRemaining,
        totalDays: 30,
      }
    : null;

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

      {/* Error banner */}
      {error && (
        <div className="mx-auto max-w-[1200px] px-8 pt-4">
          <div className="flex items-center gap-2 rounded-lg border border-destructive/50 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            <AlertCircle className="h-4 w-4" />
            {error}
          </div>
        </div>
      )}

      <div className="mx-auto max-w-[1200px] px-8 py-8">
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          {/* Main Content - 2 columns */}
          <div className="space-y-6 lg:col-span-2">
            {/* Current Plan */}
            <div className="rounded-xl border border-border bg-card p-6">
              <div className="mb-6 flex items-start justify-between">
                <div>
                  <h2 className="text-lg font-semibold text-foreground">{t("currentPlan.title")}</h2>
                  {subscription && currentPlan ? (
                    <>
                      <div className="mt-2 flex items-baseline gap-2">
                        <span className="text-3xl font-bold text-foreground">
                          {getPrice(currentPlan, subscription.billingCycle)}€
                        </span>
                        <span className="text-muted-foreground">
                          /{subscription.billingCycle === "Monthly" ? t("currentPlan.perMonth") : t("currentPlan.perYear")}
                        </span>
                      </div>
                    </>
                  ) : (
                    <p className="mt-2 text-sm text-muted-foreground">{t("currentPlan.noSubscription")}</p>
                  )}
                </div>
                <div className="flex items-center gap-2">
                  {subscription && (
                    <>
                      <span className="rounded-full bg-teal/10 px-3 py-1 text-sm font-medium text-teal">
                        {subscription.planName}
                      </span>
                      <span
                        className={cn(
                          "rounded-full px-3 py-1 text-sm font-medium",
                          subscription.status === "Active" && "bg-emerald-500/10 text-emerald-500",
                          subscription.status === "Trial" && "bg-blue-500/10 text-blue-500",
                          subscription.status === "Cancelled" && "bg-yellow-500/10 text-yellow-500",
                          subscription.status === "Expired" && "bg-destructive/10 text-destructive"
                        )}
                      >
                        {subscription.status}
                      </span>
                    </>
                  )}
                </div>
              </div>

              {subscription && (
                <>
                  <p className="mb-4 text-sm text-muted-foreground">
                    {subscription.status === "Cancelled"
                      ? t("currentPlan.expiresOn", { date: formatDate(subscription.currentPeriod.endDate) })
                      : t("currentPlan.renewsOn", { date: formatDate(subscription.currentPeriod.endDate) })}
                    {subscription.isInTrial && subscription.trialEndDate && (
                      <span className="ml-2 text-blue-500">
                        ({t("currentPlan.trialEnds", { date: formatDate(subscription.trialEndDate) })})
                      </span>
                    )}
                  </p>
                  <div className="flex gap-3">
                    <Button variant="outline" onClick={() => setChangePlanOpen(true)}>
                      {t("currentPlan.changePlan")}
                    </Button>
                    {subscription.status !== "Cancelled" && !subscription.isFree && (
                      <Button
                        variant="ghost"
                        className="text-destructive hover:text-destructive"
                        onClick={() => setCancelSubOpen(true)}
                      >
                        {t("currentPlan.cancelSubscription")}
                      </Button>
                    )}
                  </div>
                </>
              )}

              {!subscription && (
                <Button onClick={() => setChangePlanOpen(true)}>
                  {t("currentPlan.selectPlan")}
                </Button>
              )}
            </div>

            {/* Usage */}
            {usageData && (
              <div className="rounded-xl border border-border bg-card p-6">
                <div className="mb-6 flex items-center justify-between">
                  <h2 className="text-lg font-semibold text-foreground">{t("usage.title")}</h2>
                  <span className="text-sm text-muted-foreground">
                    {t("usage.period")}: {usageData.billingPeriod.start} - {usageData.billingPeriod.end}
                  </span>
                </div>
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                  {/* Studies */}
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Zap className="h-4 w-4 text-teal" />
                        <span className="text-sm font-medium text-foreground">{t("usage.studies.title")}</span>
                      </div>
                      <span className="text-sm text-muted-foreground">
                        {usageData.studies.used} / {usageData.studies.total}
                      </span>
                    </div>
                    <div className="h-2 overflow-hidden rounded-full bg-muted">
                      <div
                        className="h-full rounded-full bg-teal transition-all"
                        style={{ width: `${usageData.studies.percentage}%` }}
                      />
                    </div>
                  </div>

                  {/* Traces */}
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <HardDrive className="h-4 w-4 text-blue-deep" />
                        <span className="text-sm font-medium text-foreground">{t("usage.traces.title")}</span>
                      </div>
                      <span className="text-sm text-muted-foreground">
                        {usageData.traces.used} / {usageData.traces.total}
                      </span>
                    </div>
                    <div className="h-2 overflow-hidden rounded-full bg-muted">
                      <div
                        className="h-full rounded-full bg-blue-deep transition-all"
                        style={{ width: `${usageData.traces.percentage}%` }}
                      />
                    </div>
                  </div>

                  {/* Members per Study */}
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Users className="h-4 w-4 text-teal" />
                        <span className="text-sm font-medium text-foreground">{t("usage.members.title")}</span>
                      </div>
                      <span className="text-sm text-muted-foreground">
                        {usageData.members.used} / {usageData.members.total}
                      </span>
                    </div>
                    <div className="h-2 overflow-hidden rounded-full bg-muted">
                      <div
                        className="h-full rounded-full bg-teal transition-all"
                        style={{ width: `${usageData.members.percentage}%` }}
                      />
                    </div>
                  </div>

                  {/* Days Remaining */}
                  {usageData && (
                    <div className="space-y-3">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <BarChart3 className="h-4 w-4 text-blue-deep" />
                          <span className="text-sm font-medium text-foreground">{t("usage.daysRemaining.title")}</span>
                        </div>
                        <span className="text-sm text-muted-foreground">
                          {usageData.daysRemaining} {t("usage.daysRemaining.days")}
                        </span>
                      </div>
                      <div className="h-2 overflow-hidden rounded-full bg-muted">
                        <div
                          className="h-full rounded-full bg-blue-deep transition-all"
                          style={{ width: `${usagePercent(usageData.totalDays - usageData.daysRemaining, usageData.totalDays)}%` }}
                        />
                      </div>
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* Plan Features */}
            {currentPlan && currentPlan.features.length > 0 && (
              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-4 text-lg font-semibold text-foreground">{t("planFeatures.title")}</h2>
                <ul className="grid grid-cols-1 gap-3 md:grid-cols-2">
                  {currentPlan.features.map((feature, index) => (
                    <li key={index} className="flex items-center gap-2 text-sm text-muted-foreground">
                      <Check className="h-4 w-4 text-teal" />
                      {feature}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Payment Methods */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h3 className="mb-4 text-base font-semibold text-foreground">{t("paymentMethod.title")}</h3>
              <PaymentMethodList showAddButton />
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
                  <p className="text-foreground">{profile?.fullName || "-"}</p>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("billingInfo.email")}</p>
                  <p className="text-foreground">{user?.email || "-"}</p>
                </div>
              </div>
            </div>

            {/* Upgrade CTA */}
            {currentPlan && !plans.some((p) => p.displayOrder > currentPlan.displayOrder && !p.isFree) ? null : (
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
            )}
          </div>
        </div>
      </div>

      {/* Change Plan Dialog */}
      <Dialog open={changePlanOpen} onOpenChange={setChangePlanOpen}>
        <DialogContent className="sm:max-w-[700px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.changePlan.title")}</DialogTitle>
            <DialogDescription>{t("dialogs.changePlan.description")}</DialogDescription>
          </DialogHeader>

          {/* Billing Cycle Toggle */}
          <div className="flex justify-center gap-2 py-2">
            <Button
              variant={selectedBillingCycle === "Monthly" ? "default" : "outline"}
              size="sm"
              onClick={() => setSelectedBillingCycle("Monthly")}
            >
              {t("dialogs.changePlan.monthly")}
            </Button>
            <Button
              variant={selectedBillingCycle === "Yearly" ? "default" : "outline"}
              size="sm"
              onClick={() => setSelectedBillingCycle("Yearly")}
            >
              {t("dialogs.changePlan.yearly")}
              <span className="ml-1 text-xs text-emerald-500">-20%</span>
            </Button>
          </div>

          <div className="grid grid-cols-1 gap-4 py-4 sm:grid-cols-2 lg:grid-cols-3">
            {plans.map((plan) => {
              const isCurrentPlan = subscription?.planId === plan.id;
              const isSelected = selectedPlanId === plan.id;
              const price = getPrice(plan, selectedBillingCycle);

              return (
                <div
                  key={plan.id}
                  onClick={() => !isCurrentPlan && setSelectedPlanId(plan.id)}
                  className={cn(
                    "cursor-pointer rounded-lg border p-4 transition-all",
                    isCurrentPlan && "border-2 border-teal bg-teal/5",
                    isSelected && !isCurrentPlan && "border-2 border-blue-deep bg-blue-deep/5",
                    !isCurrentPlan && !isSelected && "border-border hover:border-muted-foreground/50"
                  )}
                >
                  <div className="mb-2 flex items-center justify-between">
                    <h4 className="font-semibold text-foreground">{plan.name}</h4>
                    {isCurrentPlan && (
                      <span className="rounded bg-teal px-1.5 py-0.5 text-xs text-white">
                        {t("dialogs.changePlan.current")}
                      </span>
                    )}
                  </div>
                  <p className="mb-2 text-2xl font-bold text-foreground">
                    {plan.isFree ? (
                      t("dialogs.changePlan.free")
                    ) : (
                      <>
                        {price}€
                        <span className="text-sm font-normal text-muted-foreground">
                          /{selectedBillingCycle === "Monthly" ? "mes" : "año"}
                        </span>
                      </>
                    )}
                  </p>
                  <p className="mb-4 text-xs text-muted-foreground">{plan.description}</p>
                  <ul className="space-y-2 text-xs text-muted-foreground">
                    <li className="flex items-center gap-1.5">
                      <Check className="h-3 w-3 text-teal" />
                      {formatLimit(plan.limits.maxStudies)} {t("dialogs.changePlan.studies")}
                    </li>
                    <li className="flex items-center gap-1.5">
                      <Check className="h-3 w-3 text-teal" />
                      {formatLimit(plan.limits.maxTracesPerMonth)} {t("dialogs.changePlan.tracesPerMonth")}
                    </li>
                    <li className="flex items-center gap-1.5">
                      <Check className="h-3 w-3 text-teal" />
                      {formatLimit(plan.limits.maxMembersPerStudy)} {t("dialogs.changePlan.membersPerStudy")}
                    </li>
                  </ul>
                </div>
              );
            })}
          </div>

          <p className="text-xs text-muted-foreground">{t("dialogs.changePlan.prorate")}</p>

          <DialogFooter>
            <Button variant="ghost" onClick={() => setChangePlanOpen(false)}>
              {tCommon("cancel")}
            </Button>
            <Button
              onClick={handleChangePlan}
              disabled={!selectedPlanId || selectedPlanId === subscription?.planId || isChangingPlan}
            >
              {isChangingPlan && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t("dialogs.changePlan.confirm")}
            </Button>
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
            <ul className="mb-4 space-y-2 text-sm text-muted-foreground">
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

            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">
                {t("dialogs.cancelSubscription.reasonLabel")}
              </label>
              <textarea
                rows={3}
                value={cancellationReason}
                onChange={(e) => setCancellationReason(e.target.value)}
                placeholder={t("dialogs.cancelSubscription.reasonPlaceholder")}
                className="w-full resize-none rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCancelSubOpen(false)}>
              {t("dialogs.cancelSubscription.keepPlan")}
            </Button>
            <Button variant="destructive" onClick={handleCancelSubscription} disabled={isCancelling}>
              {isCancelling && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t("dialogs.cancelSubscription.confirm")}
            </Button>
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
                defaultValue={profile?.fullName || ""}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("billingInfo.email")}</label>
              <input
                type="email"
                defaultValue={user?.email || ""}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setEditBillingOpen(false)}>
              {tCommon("cancel")}
            </Button>
            <Button onClick={() => setEditBillingOpen(false)}>{t("dialogs.editBillingInfo.save")}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

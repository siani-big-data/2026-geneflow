"use client";

import { useState, useRef } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import { PageHeader } from "@/components/layout";
import { Button, Switch } from "@/components/ui";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  User,
  Shield,
  Bell,
  Palette,
  CreditCard,
  Users,
  Save,
  Camera,
  Mail,
  Lock,
  Globe,
  Smartphone,
  Download,
  Key,
  ArrowRight,
  Sun,
  Moon,
  Monitor,
  Upload,
  Laptop,
  MapPin,
  Clock,
  AlertTriangle,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { TwoFactorSetup, ExternalLogins } from "@/components/auth";

type SettingsSection = "account" | "security" | "notifications" | "preferences" | "billing" | "collaboration";

const sections = [
  { id: "account" as const, nameKey: "account", icon: User },
  { id: "security" as const, nameKey: "security", icon: Shield },
  { id: "notifications" as const, nameKey: "notifications", icon: Bell },
  { id: "preferences" as const, nameKey: "preferences", icon: Palette },
  { id: "billing" as const, nameKey: "billing", icon: CreditCard },
  { id: "collaboration" as const, nameKey: "collaboration", icon: Users },
];

const mockSessions = [
  { id: "1", device: "Chrome on MacOS", location: "Stanford, CA", lastActive: "Active now", current: true },
  { id: "2", device: "Safari on iPhone", location: "San Francisco, CA", lastActive: "2 hours ago", current: false },
  { id: "3", device: "Firefox on Windows", location: "New York, NY", lastActive: "1 day ago", current: false },
];

const mockBlockedUsers: { id: string; name: string; email: string; blockedAt: string }[] = [];

export default function SettingsPage() {
  const t = useTranslations("settings");
  const tCommon = useTranslations("common");
  const [activeSection, setActiveSection] = useState<SettingsSection>("account");
  const [emailNotifications, setEmailNotifications] = useState(true);
  const [studyUpdates, setStudyUpdates] = useState(true);
  const [analysisComplete, setAnalysisComplete] = useState(true);
  const [teamInvites, setTeamInvites] = useState(true);
  const [weeklyDigest, setWeeklyDigest] = useState(false);
  const [marketingEmails, setMarketingEmails] = useState(false);
  const [twoFactorEnabled, setTwoFactorEnabled] = useState(false);
  const [sessionTimeout, setSessionTimeout] = useState(true);
  const [autoCollabApproval, setAutoCollabApproval] = useState(false);
  const [selectedTheme, setSelectedTheme] = useState<"light" | "dark" | "system">("light");

  // Modal states
  const [uploadPhotoOpen, setUploadPhotoOpen] = useState(false);
  const [sessionsOpen, setSessionsOpen] = useState(false);
  const [dataExportOpen, setDataExportOpen] = useState(false);
  const [blockedUsersOpen, setBlockedUsersOpen] = useState(false);
  const [deactivateOpen, setDeactivateOpen] = useState(false);
  const [deleteAccountOpen, setDeleteAccountOpen] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);

  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedPhoto, setSelectedPhoto] = useState<File | null>(null);

  const handleSaveChanges = () => {
    setSaveSuccess(true);
    setTimeout(() => setSaveSuccess(false), 2000);
  };

  const handlePhotoSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setSelectedPhoto(file);
    }
  };

  const handleUploadPhoto = () => {
    if (selectedPhoto) {
      console.log("Uploading photo:", selectedPhoto.name);
      setUploadPhotoOpen(false);
      setSelectedPhoto(null);
    }
  };

  const handleRemovePhoto = () => {
    console.log("Removing photo");
  };

  const handleRequestDataExport = () => {
    console.log("Requesting data export");
    setDataExportOpen(false);
  };

  const handleEndSession = (sessionId: string) => {
    console.log("Ending session:", sessionId);
  };


  return (
    <div className="space-y-6">
      <PageHeader
        title={t("title")}
        description={t("description")}
      />

      <div className="flex gap-6">
        {/* Sidebar Navigation */}
        <aside className="w-56 flex-shrink-0">
          <nav className="space-y-1">
            {sections.map((section) => {
              const Icon = section.icon;
              const isActive = activeSection === section.id;
              return (
                <button
                  key={section.id}
                  onClick={() => setActiveSection(section.id)}
                  className={cn(
                    "flex w-full items-center gap-3 rounded-lg px-4 py-2.5 text-sm font-medium transition-all",
                    isActive
                      ? "bg-gradient-to-r from-teal/10 to-blue-deep/5 text-teal shadow-sm"
                      : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
                  )}
                >
                  <Icon className="h-4.5 w-4.5" />
                  {t(`sections.${section.nameKey}`)}
                </button>
              );
            })}
          </nav>
        </aside>

        {/* Main Content */}
        <div className="min-w-0 flex-1 space-y-6">
          {/* Account Section */}
          {activeSection === "account" && (
            <>
              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">{t("account.profileInfo")}</h2>

                <div className="mb-6 flex items-start gap-6 border-b border-border pb-6">
                  <div className="group relative">
                    <div className="flex h-20 w-20 items-center justify-center rounded-xl bg-gradient-to-br from-teal to-blue-deep text-2xl font-semibold text-white shadow-sm">
                      SM
                    </div>
                    <button
                      className="absolute bottom-0 right-0 rounded-lg border border-border bg-background p-1.5 opacity-0 shadow-md transition-all hover:bg-muted group-hover:opacity-100"
                      aria-label="Change profile photo"
                    >
                      <Camera className="h-3.5 w-3.5 text-foreground" />
                    </button>
                  </div>
                  <div className="flex-1">
                    <h3 className="mb-1 text-base font-medium text-foreground">{t("account.profilePhoto")}</h3>
                    <p className="mb-3 text-sm text-muted-foreground">{t("account.profilePhotoDesc")}</p>
                    <div className="flex gap-2">
                      <button
                        onClick={() => setUploadPhotoOpen(true)}
                        className="rounded-lg border border-border px-3 py-1.5 text-xs font-medium transition-all hover:bg-muted/50"
                      >
                        {t("account.uploadNew")}
                      </button>
                      <button
                        onClick={handleRemovePhoto}
                        className="px-3 py-1.5 text-xs font-medium text-muted-foreground transition-all hover:text-foreground"
                      >
                        {t("account.remove")}
                      </button>
                    </div>
                  </div>
                </div>

                <div className="space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <label htmlFor="first-name" className="text-sm font-medium text-foreground">
                        {t("account.firstName")}
                      </label>
                      <input
                        id="first-name"
                        type="text"
                        defaultValue="Sarah"
                        className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                      />
                    </div>
                    <div className="space-y-2">
                      <label htmlFor="last-name" className="text-sm font-medium text-foreground">
                        {t("account.lastName")}
                      </label>
                      <input
                        id="last-name"
                        type="text"
                        defaultValue="Martinez"
                        className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                      />
                    </div>
                  </div>

                  <div className="space-y-2">
                    <label htmlFor="email" className="text-sm font-medium text-foreground">
                      {t("account.emailAddress")}
                    </label>
                    <div className="relative">
                      <Mail className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                      <input
                        id="email"
                        type="email"
                        defaultValue="s.martinez@stanford.edu"
                        className="w-full rounded-lg border border-border bg-background py-2.5 pl-10 pr-3.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <label htmlFor="role" className="text-sm font-medium text-foreground">
                        {t("account.role")}
                      </label>
                      <input
                        id="role"
                        type="text"
                        defaultValue="Principal Investigator"
                        className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                      />
                    </div>
                    <div className="space-y-2">
                      <label htmlFor="institution" className="text-sm font-medium text-foreground">
                        {t("account.institution")}
                      </label>
                      <input
                        id="institution"
                        type="text"
                        defaultValue="Stanford Medical Center"
                        className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                      />
                    </div>
                  </div>

                  <div className="space-y-2">
                    <label htmlFor="bio" className="text-sm font-medium text-foreground">
                      {t("account.bio")}
                    </label>
                    <textarea
                      id="bio"
                      rows={4}
                      defaultValue="Molecular geneticist specializing in Type 2 Diabetes research with a focus on genome-wide association studies and precision medicine approaches."
                      className="w-full resize-none rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                    />
                  </div>
                </div>

                <div className="mt-6 flex items-center justify-end gap-3 border-t border-border pt-6">
                  <Button variant="ghost" onClick={() => setActiveSection("account")}>
                    {tCommon("cancel")}
                  </Button>
                  <Button onClick={handleSaveChanges}>
                    <Save className="h-4 w-4" />
                    {saveSuccess ? t("account.saved") : t("account.saveChanges")}
                  </Button>
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-4 text-lg font-semibold text-foreground">{t("account.researchIdentifiers")}</h2>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <label htmlFor="orcid" className="text-sm font-medium text-foreground">
                      {t("account.orcid")}
                    </label>
                    <input
                      id="orcid"
                      type="text"
                      defaultValue="0000-0002-1234-5678"
                      className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 font-mono text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                    />
                  </div>
                  <div className="space-y-2">
                    <label htmlFor="website" className="text-sm font-medium text-foreground">
                      {t("account.website")}
                    </label>
                    <div className="relative">
                      <Globe className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                      <input
                        id="website"
                        type="url"
                        defaultValue="https://martinez-lab.stanford.edu"
                        className="w-full rounded-lg border border-border bg-background py-2.5 pl-10 pr-3.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* Danger Zone */}
              <div className="rounded-xl border border-red-500/30 bg-card p-6">
                <h2 className="mb-4 text-lg font-semibold text-red-500">{t("account.dangerZone")}</h2>
                <div className="space-y-4">
                  <div className="flex items-start justify-between border-b border-border pb-4">
                    <div className="flex-1">
                      <h3 className="mb-1 text-sm font-medium text-foreground">{t("account.deactivateAccount")}</h3>
                      <p className="text-sm text-muted-foreground">
                        {t("account.deactivateDesc")}
                      </p>
                    </div>
                    <button
                      onClick={() => setDeactivateOpen(true)}
                      className="rounded-lg border border-red-500 px-4 py-2 text-sm font-medium text-red-500 transition-all hover:bg-red-500/10"
                    >
                      {t("account.deactivate")}
                    </button>
                  </div>
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <h3 className="mb-1 text-sm font-medium text-foreground">{t("account.deleteAccount")}</h3>
                      <p className="text-sm text-muted-foreground">
                        {t("account.deleteDesc")}
                      </p>
                    </div>
                    <button
                      onClick={() => setDeleteAccountOpen(true)}
                      className="rounded-lg bg-red-500 px-4 py-2 text-sm font-medium text-white transition-all hover:bg-red-500/90"
                    >
                      {t("account.deleteAccount")}
                    </button>
                  </div>
                </div>
              </div>
            </>
          )}

          {/* Security Section */}
          {activeSection === "security" && (
            <>
              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">{t("security.password")}</h2>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <label htmlFor="current-password" className="text-sm font-medium text-foreground">
                      {t("security.currentPassword")}
                    </label>
                    <div className="relative">
                      <Lock className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                      <input
                        id="current-password"
                        type="password"
                        className="w-full rounded-lg border border-border bg-background py-2.5 pl-10 pr-3.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                      />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <label htmlFor="new-password" className="text-sm font-medium text-foreground">
                      {t("security.newPassword")}
                    </label>
                    <div className="relative">
                      <Lock className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                      <input
                        id="new-password"
                        type="password"
                        className="w-full rounded-lg border border-border bg-background py-2.5 pl-10 pr-3.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                      />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <label htmlFor="confirm-password" className="text-sm font-medium text-foreground">
                      {t("security.confirmPassword")}
                    </label>
                    <div className="relative">
                      <Lock className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                      <input
                        id="confirm-password"
                        type="password"
                        className="w-full rounded-lg border border-border bg-background py-2.5 pl-10 pr-3.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                      />
                    </div>
                  </div>
                </div>
                <div className="mt-6 flex items-center justify-end gap-3 border-t border-border pt-6">
                  <Button variant="ghost">{tCommon("cancel")}</Button>
                  <Button onClick={handleSaveChanges}>
                    {saveSuccess ? t("security.updated") : t("security.updatePassword")}
                  </Button>
                </div>
              </div>

              <TwoFactorSetup
                isEnabled={twoFactorEnabled}
                onEnableChange={setTwoFactorEnabled}
              />

              <ExternalLogins />

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-4 text-lg font-semibold text-foreground">{t("security.securityOptions")}</h2>
                <div className="space-y-4">
                  <div className="flex items-start justify-between py-3">
                    <div className="flex-1">
                      <h3 className="mb-1 text-sm font-medium text-foreground">{t("security.sessionTimeout")}</h3>
                      <p className="text-sm text-muted-foreground">
                        {t("security.sessionTimeoutDesc")}
                      </p>
                    </div>
                    <Switch checked={sessionTimeout} onCheckedChange={setSessionTimeout} />
                  </div>
                  <div className="border-t border-border pt-3">
                    <button
                      onClick={() => setSessionsOpen(true)}
                      className="flex items-center gap-2 text-sm font-medium text-foreground transition-colors hover:text-teal"
                    >
                      <Key className="h-4 w-4" />
                      {t("security.viewActiveSessions")}
                    </button>
                  </div>
                </div>
              </div>
            </>
          )}

          {/* Notifications Section */}
          {activeSection === "notifications" && (
            <>
              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">{t("notifications.emailNotifications")}</h2>
                <div className="space-y-5">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <h3 className="mb-1 text-sm font-medium text-foreground">{t("notifications.enableEmail")}</h3>
                      <p className="text-sm text-muted-foreground">
                        {t("notifications.enableEmailDesc")}
                      </p>
                    </div>
                    <Switch checked={emailNotifications} onCheckedChange={setEmailNotifications} />
                  </div>

                  {emailNotifications && (
                    <div className="space-y-4 border-l-2 border-border pl-4">
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <h3 className="mb-1 text-sm font-medium text-foreground">{t("notifications.studyUpdates")}</h3>
                          <p className="text-sm text-muted-foreground">
                            {t("notifications.studyUpdatesDesc")}
                          </p>
                        </div>
                        <Switch checked={studyUpdates} onCheckedChange={setStudyUpdates} />
                      </div>

                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <h3 className="mb-1 text-sm font-medium text-foreground">{t("notifications.analysisCompletion")}</h3>
                          <p className="text-sm text-muted-foreground">
                            {t("notifications.analysisCompletionDesc")}
                          </p>
                        </div>
                        <Switch checked={analysisComplete} onCheckedChange={setAnalysisComplete} />
                      </div>

                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <h3 className="mb-1 text-sm font-medium text-foreground">{t("notifications.teamInvitations")}</h3>
                          <p className="text-sm text-muted-foreground">
                            {t("notifications.teamInvitationsDesc")}
                          </p>
                        </div>
                        <Switch checked={teamInvites} onCheckedChange={setTeamInvites} />
                      </div>

                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <h3 className="mb-1 text-sm font-medium text-foreground">{t("notifications.weeklyDigest")}</h3>
                          <p className="text-sm text-muted-foreground">
                            {t("notifications.weeklyDigestDesc")}
                          </p>
                        </div>
                        <Switch checked={weeklyDigest} onCheckedChange={setWeeklyDigest} />
                      </div>
                    </div>
                  )}
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">{t("notifications.marketing")}</h2>
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <h3 className="mb-1 text-sm font-medium text-foreground">{t("notifications.productUpdates")}</h3>
                    <p className="text-sm text-muted-foreground">
                      {t("notifications.productUpdatesDesc")}
                    </p>
                  </div>
                  <Switch checked={marketingEmails} onCheckedChange={setMarketingEmails} />
                </div>
              </div>
            </>
          )}

          {/* Preferences Section */}
          {activeSection === "preferences" && (
            <>
              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">{t("preferences.appearance")}</h2>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <label className="text-sm font-medium text-foreground">{t("preferences.theme")}</label>
                    <div className="grid grid-cols-3 gap-3">
                      <button
                        onClick={() => setSelectedTheme("light")}
                        className={cn(
                          "rounded-lg border-2 bg-background p-4 text-left transition-all hover:bg-muted/20",
                          selectedTheme === "light" ? "border-teal" : "border-border"
                        )}
                      >
                        <div className="mb-2 flex items-center gap-2">
                          <Sun className="h-4 w-4 text-amber-500" />
                          <span className="text-sm font-medium text-foreground">{t("preferences.light")}</span>
                        </div>
                        <p className="text-xs text-muted-foreground">{t("preferences.lightDesc")}</p>
                      </button>
                      <button
                        onClick={() => setSelectedTheme("dark")}
                        className={cn(
                          "rounded-lg border-2 bg-background p-4 text-left transition-all hover:bg-muted/20",
                          selectedTheme === "dark" ? "border-teal" : "border-border"
                        )}
                      >
                        <div className="mb-2 flex items-center gap-2">
                          <Moon className="h-4 w-4 text-violet-500" />
                          <span className="text-sm font-medium text-foreground">{t("preferences.dark")}</span>
                        </div>
                        <p className="text-xs text-muted-foreground">{t("preferences.darkDesc")}</p>
                      </button>
                      <button
                        onClick={() => setSelectedTheme("system")}
                        className={cn(
                          "rounded-lg border-2 bg-background p-4 text-left transition-all hover:bg-muted/20",
                          selectedTheme === "system" ? "border-teal" : "border-border"
                        )}
                      >
                        <div className="mb-2 flex items-center gap-2">
                          <Monitor className="h-4 w-4 text-blue-500" />
                          <span className="text-sm font-medium text-foreground">{t("preferences.system")}</span>
                        </div>
                        <p className="text-xs text-muted-foreground">{t("preferences.systemDesc")}</p>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">{t("preferences.dataDisplay")}</h2>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <label htmlFor="timezone" className="text-sm font-medium text-foreground">
                      {t("preferences.timezone")}
                    </label>
                    <select
                      id="timezone"
                      className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                    >
                      <option>Pacific Time (PT)</option>
                      <option>Eastern Time (ET)</option>
                      <option>Central Time (CT)</option>
                      <option>Mountain Time (MT)</option>
                      <option>UTC</option>
                    </select>
                  </div>
                  <div className="space-y-2">
                    <label htmlFor="date-format" className="text-sm font-medium text-foreground">
                      {t("preferences.dateFormat")}
                    </label>
                    <select
                      id="date-format"
                      className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                    >
                      <option>MM/DD/YYYY</option>
                      <option>DD/MM/YYYY</option>
                      <option>YYYY-MM-DD</option>
                    </select>
                  </div>
                  <div className="space-y-2">
                    <label htmlFor="items-per-page" className="text-sm font-medium text-foreground">
                      {t("preferences.itemsPerPage")}
                    </label>
                    <select
                      id="items-per-page"
                      className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                    >
                      <option>10</option>
                      <option>25</option>
                      <option>50</option>
                      <option>100</option>
                    </select>
                  </div>
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">{t("preferences.dataExport")}</h2>
                <p className="mb-4 text-sm text-muted-foreground">
                  {t("preferences.dataExportDesc")}
                </p>
                <Button variant="outline" onClick={() => setDataExportOpen(true)}>
                  <Download className="h-4 w-4" />
                  {t("preferences.requestExport")}
                </Button>
              </div>
            </>
          )}

          {/* Billing Section */}
          {activeSection === "billing" && (
            <>
              <div className="rounded-xl border border-border bg-gradient-to-br from-teal/5 to-blue-deep/5 p-8 text-center">
                <CreditCard className="mx-auto mb-4 h-12 w-12 text-teal" />
                <h2 className="mb-2 text-xl font-semibold text-foreground">{t("billing.title")}</h2>
                <p className="mx-auto mb-6 max-w-md text-sm text-muted-foreground">
                  {t("billing.description")}
                </p>
                <Link href="/settings/billing">
                  <Button>
                    {t("billing.goToBilling")}
                    <ArrowRight className="h-4 w-4" />
                  </Button>
                </Link>
              </div>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                <div className="rounded-xl border border-border bg-card p-5">
                  <p className="mb-2 text-xs text-muted-foreground">{t("billing.currentPlan")}</p>
                  <p className="mb-1 text-lg font-semibold text-foreground">Professional</p>
                  <p className="text-xs text-emerald-500">{t("billing.active")}</p>
                </div>
                <div className="rounded-xl border border-border bg-card p-5">
                  <p className="mb-2 text-xs text-muted-foreground">{t("billing.nextBilling")}</p>
                  <p className="mb-1 text-lg font-semibold text-foreground">April 15, 2026</p>
                  <p className="text-xs text-muted-foreground">$149/month</p>
                </div>
                <div className="rounded-xl border border-border bg-card p-5">
                  <p className="mb-2 text-xs text-muted-foreground">{t("billing.paymentMethod")}</p>
                  <p className="mb-1 text-lg font-semibold text-foreground">Visa **** 4242</p>
                  <p className="text-xs text-muted-foreground">Expires 12/2027</p>
                </div>
              </div>
            </>
          )}

          {/* Collaboration Section */}
          {activeSection === "collaboration" && (
            <>
              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">{t("collaboration.settings")}</h2>
                <div className="space-y-5">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <h3 className="mb-1 text-sm font-medium text-foreground">{t("collaboration.autoApprove")}</h3>
                      <p className="text-sm text-muted-foreground">
                        {t("collaboration.autoApproveDesc")}
                      </p>
                    </div>
                    <Switch checked={autoCollabApproval} onCheckedChange={setAutoCollabApproval} />
                  </div>

                  <div className="border-t border-border pt-4">
                    <div className="space-y-2">
                      <label htmlFor="default-role" className="text-sm font-medium text-foreground">
                        {t("collaboration.defaultRole")}
                      </label>
                      <select
                        id="default-role"
                        className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                      >
                        <option>{t("collaboration.roles.viewerReadOnly")}</option>
                        <option>{t("collaboration.roles.collaborator")}</option>
                        <option>{t("collaboration.roles.researchScientist")}</option>
                      </select>
                    </div>
                  </div>
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">{t("collaboration.visibility")}</h2>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <label htmlFor="profile-visibility" className="text-sm font-medium text-foreground">
                      {t("collaboration.profileVisibility")}
                    </label>
                    <select
                      id="profile-visibility"
                      className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                    >
                      <option>{t("collaboration.profileOptions.public")}</option>
                      <option>{t("collaboration.profileOptions.institutionOnly")}</option>
                      <option>{t("collaboration.profileOptions.private")}</option>
                    </select>
                  </div>
                  <div className="space-y-2">
                    <label htmlFor="study-visibility" className="text-sm font-medium text-foreground">
                      {t("collaboration.studyVisibility")}
                    </label>
                    <select
                      id="study-visibility"
                      className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                    >
                      <option>{t("collaboration.studyOptions.private")}</option>
                      <option>{t("collaboration.studyOptions.institution")}</option>
                      <option>{t("collaboration.studyOptions.public")}</option>
                    </select>
                  </div>
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-4 text-lg font-semibold text-foreground">{t("collaboration.blockedUsers")}</h2>
                <p className="mb-4 text-sm text-muted-foreground">
                  {t("collaboration.blockedUsersDesc")}
                </p>
                <button
                  onClick={() => setBlockedUsersOpen(true)}
                  className="text-sm font-medium text-foreground transition-colors hover:text-teal"
                >
                  {t("collaboration.viewBlocked")} ({mockBlockedUsers.length})
                </button>
              </div>
            </>
          )}
        </div>
      </div>

      {/* Upload Photo Dialog */}
      <Dialog open={uploadPhotoOpen} onOpenChange={setUploadPhotoOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.uploadPhoto.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.uploadPhoto.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <div
              className={cn(
                "group cursor-pointer rounded-lg border-2 border-dashed p-8 text-center transition-all",
                selectedPhoto
                  ? "border-teal bg-teal/5"
                  : "border-border hover:border-teal/50"
              )}
              onClick={() => fileInputRef.current?.click()}
            >
              <input
                ref={fileInputRef}
                type="file"
                accept="image/jpeg,image/png"
                onChange={handlePhotoSelect}
                className="hidden"
              />
              <Upload className="mx-auto mb-3 h-10 w-10 text-muted-foreground" />
              {selectedPhoto ? (
                <p className="text-sm font-medium text-foreground">{selectedPhoto.name}</p>
              ) : (
                <p className="text-sm text-muted-foreground">{t("dialogs.uploadPhoto.clickToSelect")}</p>
              )}
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => {
                setSelectedPhoto(null);
                setUploadPhotoOpen(false);
              }}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleUploadPhoto}
              disabled={!selectedPhoto}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90 disabled:opacity-50"
            >
              {t("dialogs.uploadPhoto.upload")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Active Sessions Dialog */}
      <Dialog open={sessionsOpen} onOpenChange={setSessionsOpen}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.sessions.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.sessions.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3 py-4">
            {mockSessions.map((session) => (
              <div
                key={session.id}
                className="flex items-center justify-between rounded-lg border border-border p-4"
              >
                <div className="flex items-center gap-3">
                  <div className="rounded-lg bg-muted p-2">
                    <Laptop className="h-4 w-4 text-foreground" />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-foreground">{session.device}</p>
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <MapPin className="h-3 w-3" />
                      {session.location}
                      <span>•</span>
                      <Clock className="h-3 w-3" />
                      {session.lastActive}
                    </div>
                  </div>
                </div>
                {session.current ? (
                  <span className="rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-medium text-emerald-500">
                    {t("dialogs.sessions.current")}
                  </span>
                ) : (
                  <button
                    onClick={() => handleEndSession(session.id)}
                    className="text-xs font-medium text-red-500 hover:text-red-500/80"
                  >
                    {t("dialogs.sessions.endSession")}
                  </button>
                )}
              </div>
            ))}
          </div>
          <DialogFooter>
            <button
              onClick={() => setSessionsOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {t("dialogs.sessions.done")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Data Export Dialog */}
      <Dialog open={dataExportOpen} onOpenChange={setDataExportOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.dataExport.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.dataExport.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <div className="rounded-lg border border-border bg-muted/30 p-4">
              <p className="text-sm text-muted-foreground">
                {t("dialogs.dataExport.includesTitle")}
              </p>
              <ul className="mt-2 space-y-1 text-sm text-foreground">
                <li>• {t("dialogs.dataExport.profileInfo")}</li>
                <li>• {t("dialogs.dataExport.studyData")}</li>
                <li>• {t("dialogs.dataExport.analysisResults")}</li>
                <li>• {t("dialogs.dataExport.activityLogs")}</li>
              </ul>
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => setDataExportOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleRequestDataExport}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90"
            >
              {t("dialogs.dataExport.requestExport")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Blocked Users Dialog */}
      <Dialog open={blockedUsersOpen} onOpenChange={setBlockedUsersOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.blockedUsers.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.blockedUsers.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            {mockBlockedUsers.length === 0 ? (
              <div className="text-center py-8">
                <Users className="mx-auto mb-3 h-10 w-10 text-muted-foreground" />
                <p className="text-sm text-muted-foreground">
                  {t("dialogs.blockedUsers.noBlockedUsers")}
                </p>
              </div>
            ) : (
              <div className="space-y-3">
                {mockBlockedUsers.map((user) => (
                  <div key={user.id} className="flex items-center justify-between p-3 border rounded-lg">
                    <div>
                      <p className="font-medium">{user.name}</p>
                      <p className="text-xs text-muted-foreground">{user.email}</p>
                    </div>
                    <button className="text-xs text-teal hover:text-teal/80">{t("dialogs.blockedUsers.unblock")}</button>
                  </div>
                ))}
              </div>
            )}
          </div>
          <DialogFooter>
            <button
              onClick={() => setBlockedUsersOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {t("dialogs.blockedUsers.done")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Deactivate Account Dialog */}
      <Dialog open={deactivateOpen} onOpenChange={setDeactivateOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.deactivate.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.deactivate.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <div className="flex items-start gap-3 rounded-lg border border-amber-500/30 bg-amber-500/10 p-4">
              <AlertTriangle className="h-5 w-5 flex-shrink-0 text-amber-500" />
              <p className="text-sm text-amber-700 dark:text-amber-400">
                {t("dialogs.deactivate.warning")}
              </p>
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => setDeactivateOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={() => {
                console.log("Deactivating account");
                setDeactivateOpen(false);
              }}
              className="rounded-lg border border-red-500 px-4 py-2.5 text-sm font-medium text-red-500 hover:bg-red-500/10"
            >
              {t("dialogs.deactivate.confirm")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Account Dialog */}
      <Dialog open={deleteAccountOpen} onOpenChange={setDeleteAccountOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle className="text-red-500">{t("dialogs.deleteAccount.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.deleteAccount.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <div className="space-y-4">
              <div className="flex items-start gap-3 rounded-lg border border-red-500/30 bg-red-500/10 p-4">
                <AlertTriangle className="h-5 w-5 flex-shrink-0 text-red-500" />
                <div className="text-sm text-red-700 dark:text-red-400">
                  <p className="font-medium">{t("dialogs.deleteAccount.warningTitle")}</p>
                  <ul className="mt-1 list-disc pl-4 space-y-0.5">
                    <li>{t("dialogs.deleteAccount.warningProfile")}</li>
                    <li>{t("dialogs.deleteAccount.warningStudies")}</li>
                    <li>{t("dialogs.deleteAccount.warningTraces")}</li>
                  </ul>
                </div>
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium text-foreground">
                  {t("dialogs.deleteAccount.confirmLabel")}
                </label>
                <input
                  type="text"
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
                  placeholder={t("dialogs.deleteAccount.confirmPlaceholder")}
                />
              </div>
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => setDeleteAccountOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={() => {
                console.log("Deleting account");
                setDeleteAccountOpen(false);
              }}
              className="rounded-lg bg-red-500 px-4 py-2.5 text-sm font-medium text-white hover:bg-red-500/90"
            >
              {t("dialogs.deleteAccount.confirm")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

    </div>
  );
}

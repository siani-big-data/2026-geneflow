"use client";

import { useState } from "react";
import Link from "next/link";
import { PageHeader } from "@/components/layout";
import { Button, Switch } from "@/components/ui";
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
} from "lucide-react";
import { cn } from "@/lib/utils";

type SettingsSection = "account" | "security" | "notifications" | "preferences" | "billing" | "collaboration";

const sections = [
  { id: "account" as const, name: "Account", icon: User },
  { id: "security" as const, name: "Security", icon: Shield },
  { id: "notifications" as const, name: "Notifications", icon: Bell },
  { id: "preferences" as const, name: "Preferences", icon: Palette },
  { id: "billing" as const, name: "Billing", icon: CreditCard },
  { id: "collaboration" as const, name: "Collaboration", icon: Users },
];

export default function SettingsPage() {
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

  return (
    <div className="space-y-6">
      <PageHeader
        title="Settings"
        description="Manage your account settings and preferences"
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
                  {section.name}
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
                <h2 className="mb-5 text-lg font-semibold text-foreground">Profile Information</h2>

                <div className="mb-6 flex items-start gap-6 border-b border-border pb-6">
                  <div className="group relative">
                    <div className="flex h-20 w-20 items-center justify-center rounded-xl bg-gradient-to-br from-teal to-blue-deep text-2xl font-semibold text-white shadow-sm">
                      SM
                    </div>
                    <button className="absolute bottom-0 right-0 rounded-lg border border-border bg-background p-1.5 opacity-0 shadow-md transition-all hover:bg-muted group-hover:opacity-100">
                      <Camera className="h-3.5 w-3.5 text-foreground" />
                    </button>
                  </div>
                  <div className="flex-1">
                    <h3 className="mb-1 text-base font-medium text-foreground">Profile Photo</h3>
                    <p className="mb-3 text-sm text-muted-foreground">Update your profile picture. JPG or PNG, max 5MB.</p>
                    <div className="flex gap-2">
                      <button className="rounded-lg border border-border px-3 py-1.5 text-xs font-medium transition-all hover:bg-muted/50">
                        Upload New
                      </button>
                      <button className="px-3 py-1.5 text-xs font-medium text-muted-foreground transition-all hover:text-foreground">
                        Remove
                      </button>
                    </div>
                  </div>
                </div>

                <div className="space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <label htmlFor="first-name" className="text-sm font-medium text-foreground">
                        First Name
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
                        Last Name
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
                      Email Address
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
                        Role
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
                        Institution
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
                      Bio
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
                  <Button variant="ghost">Cancel</Button>
                  <Button>
                    <Save className="h-4 w-4" />
                    Save Changes
                  </Button>
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-4 text-lg font-semibold text-foreground">Research Identifiers</h2>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <label htmlFor="orcid" className="text-sm font-medium text-foreground">
                      ORCID iD
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
                      Website
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
                <h2 className="mb-4 text-lg font-semibold text-red-500">Danger Zone</h2>
                <div className="space-y-4">
                  <div className="flex items-start justify-between border-b border-border pb-4">
                    <div className="flex-1">
                      <h3 className="mb-1 text-sm font-medium text-foreground">Deactivate Account</h3>
                      <p className="text-sm text-muted-foreground">
                        Temporarily disable your account. You can reactivate it at any time.
                      </p>
                    </div>
                    <button className="rounded-lg border border-red-500 px-4 py-2 text-sm font-medium text-red-500 transition-all hover:bg-red-500/10">
                      Deactivate
                    </button>
                  </div>
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <h3 className="mb-1 text-sm font-medium text-foreground">Delete Account</h3>
                      <p className="text-sm text-muted-foreground">
                        Permanently delete your account and all associated data. This action cannot be undone.
                      </p>
                    </div>
                    <button className="rounded-lg bg-red-500 px-4 py-2 text-sm font-medium text-white transition-all hover:bg-red-500/90">
                      Delete Account
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
                <h2 className="mb-5 text-lg font-semibold text-foreground">Password</h2>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <label htmlFor="current-password" className="text-sm font-medium text-foreground">
                      Current Password
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
                      New Password
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
                      Confirm New Password
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
                  <Button variant="ghost">Cancel</Button>
                  <Button>Update Password</Button>
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">Two-Factor Authentication</h2>
                <div className="mb-6 flex items-start justify-between">
                  <div className="flex-1">
                    <div className="mb-2 flex items-center gap-2">
                      <h3 className="text-sm font-medium text-foreground">Enable 2FA</h3>
                      {twoFactorEnabled && (
                        <span className="rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-500">
                          Active
                        </span>
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground">
                      Add an extra layer of security to your account using authentication apps.
                    </p>
                  </div>
                  <Switch checked={twoFactorEnabled} onCheckedChange={setTwoFactorEnabled} />
                </div>
                {twoFactorEnabled && (
                  <div className="rounded-lg border border-border bg-muted/30 p-4">
                    <div className="flex items-start gap-3">
                      <Smartphone className="mt-0.5 h-5 w-5 text-teal" />
                      <div className="flex-1">
                        <p className="mb-1 text-sm font-medium text-foreground">Authenticator App Connected</p>
                        <p className="text-xs text-muted-foreground">
                          You're using an authenticator app for two-factor authentication.
                        </p>
                      </div>
                      <button className="text-xs font-medium text-red-500 hover:text-red-500/80">Remove</button>
                    </div>
                  </div>
                )}
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-4 text-lg font-semibold text-foreground">Security Options</h2>
                <div className="space-y-4">
                  <div className="flex items-start justify-between py-3">
                    <div className="flex-1">
                      <h3 className="mb-1 text-sm font-medium text-foreground">Session Timeout</h3>
                      <p className="text-sm text-muted-foreground">
                        Automatically log out after 30 minutes of inactivity.
                      </p>
                    </div>
                    <Switch checked={sessionTimeout} onCheckedChange={setSessionTimeout} />
                  </div>
                  <div className="border-t border-border pt-3">
                    <button className="flex items-center gap-2 text-sm font-medium text-foreground transition-colors hover:text-teal">
                      <Key className="h-4 w-4" />
                      View Active Sessions
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
                <h2 className="mb-5 text-lg font-semibold text-foreground">Email Notifications</h2>
                <div className="space-y-5">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <h3 className="mb-1 text-sm font-medium text-foreground">Enable Email Notifications</h3>
                      <p className="text-sm text-muted-foreground">
                        Receive email updates about your studies and activities.
                      </p>
                    </div>
                    <Switch checked={emailNotifications} onCheckedChange={setEmailNotifications} />
                  </div>

                  {emailNotifications && (
                    <div className="space-y-4 border-l-2 border-border pl-4">
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <h3 className="mb-1 text-sm font-medium text-foreground">Study Updates</h3>
                          <p className="text-sm text-muted-foreground">
                            Notifications when studies you're involved in are updated.
                          </p>
                        </div>
                        <Switch checked={studyUpdates} onCheckedChange={setStudyUpdates} />
                      </div>

                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <h3 className="mb-1 text-sm font-medium text-foreground">Analysis Completion</h3>
                          <p className="text-sm text-muted-foreground">
                            Get notified when your analysis pipelines complete.
                          </p>
                        </div>
                        <Switch checked={analysisComplete} onCheckedChange={setAnalysisComplete} />
                      </div>

                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <h3 className="mb-1 text-sm font-medium text-foreground">Team Invitations</h3>
                          <p className="text-sm text-muted-foreground">
                            Receive notifications when you're invited to collaborate.
                          </p>
                        </div>
                        <Switch checked={teamInvites} onCheckedChange={setTeamInvites} />
                      </div>

                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <h3 className="mb-1 text-sm font-medium text-foreground">Weekly Digest</h3>
                          <p className="text-sm text-muted-foreground">
                            Summary of your activity and important updates once a week.
                          </p>
                        </div>
                        <Switch checked={weeklyDigest} onCheckedChange={setWeeklyDigest} />
                      </div>
                    </div>
                  )}
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">Marketing & Updates</h2>
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <h3 className="mb-1 text-sm font-medium text-foreground">Product Updates & News</h3>
                    <p className="text-sm text-muted-foreground">
                      Receive occasional emails about new features and platform updates.
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
                <h2 className="mb-5 text-lg font-semibold text-foreground">Appearance</h2>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <label className="text-sm font-medium text-foreground">Theme</label>
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
                          <span className="text-sm font-medium text-foreground">Light</span>
                        </div>
                        <p className="text-xs text-muted-foreground">Clean and bright</p>
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
                          <span className="text-sm font-medium text-foreground">Dark</span>
                        </div>
                        <p className="text-xs text-muted-foreground">Easy on the eyes</p>
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
                          <span className="text-sm font-medium text-foreground">System</span>
                        </div>
                        <p className="text-xs text-muted-foreground">System default</p>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">Data & Display</h2>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <label htmlFor="timezone" className="text-sm font-medium text-foreground">
                      Timezone
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
                      Date Format
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
                      Items Per Page
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
                <h2 className="mb-5 text-lg font-semibold text-foreground">Data Export</h2>
                <p className="mb-4 text-sm text-muted-foreground">
                  Download a copy of your account data and research information.
                </p>
                <Button variant="outline">
                  <Download className="h-4 w-4" />
                  Request Data Export
                </Button>
              </div>
            </>
          )}

          {/* Billing Section */}
          {activeSection === "billing" && (
            <>
              <div className="rounded-xl border border-border bg-gradient-to-br from-teal/5 to-blue-deep/5 p-8 text-center">
                <CreditCard className="mx-auto mb-4 h-12 w-12 text-teal" />
                <h2 className="mb-2 text-xl font-semibold text-foreground">Billing & Subscription</h2>
                <p className="mx-auto mb-6 max-w-md text-sm text-muted-foreground">
                  Manage your subscription plan, payment methods, billing history, and usage metrics on the dedicated billing page.
                </p>
                <Link href="/settings/billing">
                  <Button>
                    Go to Billing
                    <ArrowRight className="h-4 w-4" />
                  </Button>
                </Link>
              </div>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                <div className="rounded-xl border border-border bg-card p-5">
                  <p className="mb-2 text-xs text-muted-foreground">Current Plan</p>
                  <p className="mb-1 text-lg font-semibold text-foreground">Professional</p>
                  <p className="text-xs text-emerald-500">Active</p>
                </div>
                <div className="rounded-xl border border-border bg-card p-5">
                  <p className="mb-2 text-xs text-muted-foreground">Next Billing</p>
                  <p className="mb-1 text-lg font-semibold text-foreground">April 15, 2026</p>
                  <p className="text-xs text-muted-foreground">$149/month</p>
                </div>
                <div className="rounded-xl border border-border bg-card p-5">
                  <p className="mb-2 text-xs text-muted-foreground">Payment Method</p>
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
                <h2 className="mb-5 text-lg font-semibold text-foreground">Collaboration Settings</h2>
                <div className="space-y-5">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <h3 className="mb-1 text-sm font-medium text-foreground">Auto-approve Collaborations</h3>
                      <p className="text-sm text-muted-foreground">
                        Automatically accept collaboration requests from verified institutions.
                      </p>
                    </div>
                    <Switch checked={autoCollabApproval} onCheckedChange={setAutoCollabApproval} />
                  </div>

                  <div className="border-t border-border pt-4">
                    <div className="space-y-2">
                      <label htmlFor="default-role" className="text-sm font-medium text-foreground">
                        Default Role for Invited Members
                      </label>
                      <select
                        id="default-role"
                        className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                      >
                        <option>Viewer (Read-only)</option>
                        <option>Collaborator</option>
                        <option>Research Scientist</option>
                      </select>
                    </div>
                  </div>
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-5 text-lg font-semibold text-foreground">Visibility</h2>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <label htmlFor="profile-visibility" className="text-sm font-medium text-foreground">
                      Profile Visibility
                    </label>
                    <select
                      id="profile-visibility"
                      className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                    >
                      <option>Public - Visible to all GeneFlow users</option>
                      <option>Institution Only - Visible to your institution</option>
                      <option>Private - Only visible to collaborators</option>
                    </select>
                  </div>
                  <div className="space-y-2">
                    <label htmlFor="study-visibility" className="text-sm font-medium text-foreground">
                      Default Study Visibility
                    </label>
                    <select
                      id="study-visibility"
                      className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                    >
                      <option>Private - Team members only</option>
                      <option>Institution - Visible to institution members</option>
                      <option>Public - Visible to all users</option>
                    </select>
                  </div>
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <h2 className="mb-4 text-lg font-semibold text-foreground">Blocked Users</h2>
                <p className="mb-4 text-sm text-muted-foreground">
                  Manage users you've blocked from collaborating with you.
                </p>
                <button className="text-sm font-medium text-foreground transition-colors hover:text-teal">
                  View Blocked Users (0)
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

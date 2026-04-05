"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import {
  Search,
  Book,
  Users,
  FileText,
  Settings,
  BarChart3,
  PlayCircle,
  Mail,
  MessageCircle,
  FileQuestion,
  Zap,
  Shield,
  Globe,
  ExternalLink,
  ChevronRight,
  ChevronDown,
  Download,
  Send,
} from "lucide-react";
import { cn } from "@/lib/utils";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

const helpCategories = [
  {
    titleKey: "gettingStarted",
    icon: Zap,
    color: "text-teal",
    bgColor: "bg-teal/10",
    topicKeys: ["createStudy", "setupProfile", "inviteTeam", "understandDashboard"],
  },
  {
    titleKey: "studiesData",
    icon: FileText,
    color: "text-blue-deep",
    bgColor: "bg-blue-deep/10",
    topicKeys: ["manageMetadata", "uploadTraces", "organizeSamples", "shareStudies"],
  },
  {
    titleKey: "pipelines",
    icon: BarChart3,
    color: "text-teal",
    bgColor: "bg-teal/10",
    topicKeys: ["runPipelines", "understandResults", "qcMetrics", "exportData"],
  },
  {
    titleKey: "collaboration",
    icon: Users,
    color: "text-blue-deep",
    bgColor: "bg-blue-deep/10",
    topicKeys: ["rolesPermissions", "inviteCollaborators", "shareSecurely", "manageAccess"],
  },
  {
    titleKey: "security",
    icon: Shield,
    color: "text-teal",
    bgColor: "bg-teal/10",
    topicKeys: ["twoFactor", "encryption", "compliance", "auditLogs"],
  },
  {
    titleKey: "accountSettings",
    icon: Settings,
    color: "text-blue-deep",
    bgColor: "bg-blue-deep/10",
    topicKeys: ["profileCustomization", "notificationPrefs", "billing", "apiTokens"],
  },
];

const faqKeys = [
  "uploadTraces",
  "availablePipelines",
  "inviteTeam",
  "exportResults",
  "dataProtection",
  "fileFormats",
] as const;

const quickStartGuides = [
  { key: "createStudy", duration: "5" },
  { key: "uploadTraces", duration: "8" },
  { key: "runPipeline", duration: "10" },
] as const;

const workflows = [
  { key: "qc", stepKeys: ["upload", "runQC", "reviewMetrics", "filterSamples"] },
  { key: "variantCalling", stepKeys: ["align", "callVariants", "annotate", "exportVCF"] },
  { key: "collaborative", stepKeys: ["createStudy", "inviteMembers", "assignRoles", "shareResults"] },
] as const;

function AccordionItem({
  question,
  answer,
  isOpen,
  onToggle,
}: {
  question: string;
  answer: string;
  isOpen: boolean;
  onToggle: () => void;
}) {
  return (
    <div className="border-b border-border">
      <button
        onClick={onToggle}
        className="flex w-full items-center justify-between py-4 text-left transition-colors hover:text-teal"
      >
        <span className="pr-4 text-sm font-medium text-foreground">{question}</span>
        <ChevronDown
          className={cn(
            "h-4 w-4 flex-shrink-0 text-muted-foreground transition-transform",
            isOpen && "rotate-180"
          )}
        />
      </button>
      {isOpen && (
        <div className="pb-4">
          <p className="text-sm leading-relaxed text-muted-foreground">{answer}</p>
        </div>
      )}
    </div>
  );
}

export default function HelpPage() {
  const t = useTranslations("help");
  const tCommon = useTranslations("common");
  const [searchQuery, setSearchQuery] = useState("");
  const [openFaqIndex, setOpenFaqIndex] = useState<number | null>(null);
  const [contactOpen, setContactOpen] = useState(false);
  const [liveChatOpen, setLiveChatOpen] = useState(false);
  const [chatMessages, setChatMessages] = useState<{ sender: "user" | "support"; text: string }[]>([]);
  const [chatInput, setChatInput] = useState("");

  const handleSendMessage = () => {
    if (chatInput.trim()) {
      setChatMessages([...chatMessages, { sender: "user", text: chatInput }]);
      setChatInput("");
      // Simulate support response
      setTimeout(() => {
        setChatMessages(prev => [...prev, {
          sender: "support",
          text: t("dialogs.liveChat.autoResponse")
        }]);
      }, 1000);
    }
  };

  const handleTopicClick = (topic: string) => {
    console.log("Navigating to topic:", topic);
  };

  return (
    <div className="-mx-16 -mt-10 min-h-full bg-background">
      {/* Hero Section */}
      <div className="border-b border-border bg-card">
        <div className="mx-auto max-w-[1200px] px-8 py-12">
          <div className="mx-auto max-w-[700px] text-center">
            <h1 className="mb-3 text-3xl font-semibold text-foreground">{t("hero.title")}</h1>
            <p className="mb-8 text-base text-muted-foreground">
              {t("hero.description")}
            </p>

            {/* Search Bar */}
            <div className="relative">
              <Search className="absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-muted-foreground" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder={t("hero.searchPlaceholder")}
                className="w-full rounded-xl border border-border bg-background py-4 pl-12 pr-4 text-sm text-foreground shadow-sm transition-all placeholder:text-muted-foreground focus:border-teal focus:outline-none focus:ring-4 focus:ring-teal/10"
              />
            </div>
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-[1200px] px-8 py-8">
        {/* Quick Start Guides */}
        <div className="mb-12">
          <div className="mb-6 flex items-center justify-between">
            <div>
              <h2 className="mb-1 text-xl font-semibold text-foreground">{t("quickStart.title")}</h2>
              <p className="text-sm text-muted-foreground">{t("quickStart.description")}</p>
            </div>
          </div>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            {quickStartGuides.map((guide, idx) => (
              <div
                key={idx}
                className="group cursor-pointer rounded-xl border border-border bg-card p-5 transition-all hover:border-teal/30 hover:shadow-md"
              >
                <div className="mb-3 flex items-start gap-3">
                  <div className="rounded-lg bg-teal/10 p-2 transition-colors group-hover:bg-teal/20">
                    <PlayCircle className="h-5 w-5 text-teal" />
                  </div>
                  <div className="flex-1">
                    <h3 className="mb-1 font-medium text-foreground transition-colors group-hover:text-teal">
                      {t(`quickStart.guides.${guide.key}.title`)}
                    </h3>
                    <p className="text-xs text-muted-foreground">{guide.duration} {t("quickStart.minRead")}</p>
                  </div>
                </div>
                <p className="text-sm text-muted-foreground">{t(`quickStart.guides.${guide.key}.description`)}</p>
              </div>
            ))}
          </div>
        </div>

        {/* Help Categories */}
        <div className="mb-12">
          <div className="mb-6">
            <h2 className="mb-1 text-xl font-semibold text-foreground">{t("categories.title")}</h2>
            <p className="text-sm text-muted-foreground">{t("categories.description")}</p>
          </div>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
            {helpCategories.map((category, idx) => {
              const Icon = category.icon;
              return (
                <div
                  key={idx}
                  className="group cursor-pointer rounded-xl border border-border bg-card p-6 transition-all hover:border-teal/30 hover:shadow-md"
                >
                  <div className="mb-4 flex items-center gap-3">
                    <div
                      className={cn(
                        "rounded-lg p-2.5 transition-transform group-hover:scale-110",
                        category.bgColor
                      )}
                    >
                      <Icon className={cn("h-5 w-5", category.color)} />
                    </div>
                    <h3 className="font-semibold text-foreground">{t(`categories.${category.titleKey}.title`)}</h3>
                  </div>
                  <ul className="space-y-2.5">
                    {category.topicKeys.map((topicKey, topicIdx) => (
                      <li key={topicIdx}>
                        <button
                          onClick={() => handleTopicClick(topicKey)}
                          className="group/item flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-teal"
                        >
                          <ChevronRight className="h-3.5 w-3.5 opacity-0 transition-opacity group-hover/item:opacity-100" />
                          <span>{t(`categories.${category.titleKey}.topics.${topicKey}`)}</span>
                        </button>
                      </li>
                    ))}
                  </ul>
                </div>
              );
            })}
          </div>
        </div>

        {/* Workflows */}
        <div className="mb-12">
          <div className="mb-6">
            <h2 className="mb-1 text-xl font-semibold text-foreground">{t("workflows.title")}</h2>
            <p className="text-sm text-muted-foreground">{t("workflows.description")}</p>
          </div>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            {workflows.map((workflow, idx) => (
              <div
                key={idx}
                className="rounded-xl border border-border bg-card p-5 transition-all hover:border-teal/30 hover:shadow-md"
              >
                <h3 className="mb-2 font-medium text-foreground">{t(`workflows.${workflow.key}.title`)}</h3>
                <p className="mb-4 text-sm text-muted-foreground">{t(`workflows.${workflow.key}.description`)}</p>
                <div className="space-y-2">
                  {workflow.stepKeys.map((stepKey, stepIdx) => (
                    <div key={stepIdx} className="flex items-center gap-2.5">
                      <div className="flex h-6 w-6 flex-shrink-0 items-center justify-center rounded-full bg-teal/10 text-xs font-medium text-teal">
                        {stepIdx + 1}
                      </div>
                      <span className="text-sm text-muted-foreground">{t(`workflows.${workflow.key}.steps.${stepKey}`)}</span>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="mb-12 grid grid-cols-1 gap-8 lg:grid-cols-3">
          {/* Common Questions - Takes 2 columns */}
          <div className="lg:col-span-2">
            <div className="mb-6">
              <h2 className="mb-1 text-xl font-semibold text-foreground">{t("faq.title")}</h2>
              <p className="text-sm text-muted-foreground">{t("faq.description")}</p>
            </div>
            <div className="rounded-xl border border-border bg-card">
              <div className="divide-y divide-border px-6">
                {faqKeys.map((key, idx) => (
                  <AccordionItem
                    key={idx}
                    question={t(`faq.questions.${key}.question`)}
                    answer={t(`faq.questions.${key}.answer`)}
                    isOpen={openFaqIndex === idx}
                    onToggle={() => setOpenFaqIndex(openFaqIndex === idx ? null : idx)}
                  />
                ))}
              </div>
            </div>
          </div>

          {/* Support & Resources - Takes 1 column */}
          <div className="space-y-6">
            {/* Contact Support */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h3 className="mb-4 text-base font-semibold text-foreground">{t("support.title")}</h3>
              <p className="mb-5 text-sm text-muted-foreground">
                {t("support.description")}
              </p>
              <div className="space-y-3">
                <button
                  onClick={() => setContactOpen(true)}
                  className="group flex w-full items-center gap-3 rounded-lg bg-teal p-3 text-white transition-all hover:bg-teal/90"
                >
                  <Mail className="h-4 w-4" />
                  <div className="flex-1 text-left">
                    <p className="text-sm font-medium">{t("support.emailSupport")}</p>
                    <p className="text-xs opacity-90">{t("support.emailResponse")}</p>
                  </div>
                </button>
                <button
                  onClick={() => setLiveChatOpen(true)}
                  className="group flex w-full items-center gap-3 rounded-lg border border-border p-3 transition-all hover:bg-muted/50"
                >
                  <MessageCircle className="h-4 w-4 text-foreground" />
                  <div className="flex-1 text-left">
                    <p className="text-sm font-medium text-foreground">{t("support.liveChat")}</p>
                    <p className="text-xs text-muted-foreground">{t("support.liveChatHours")}</p>
                  </div>
                </button>
              </div>
            </div>

            {/* Resources */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h3 className="mb-4 text-base font-semibold text-foreground">{t("resources.title")}</h3>
              <div className="space-y-3">
                <a
                  href="https://docs.geneflow.io"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="group flex items-center justify-between rounded-lg p-3 transition-all hover:bg-muted/50"
                >
                  <div className="flex items-center gap-3">
                    <Book className="h-4 w-4 text-teal" />
                    <span className="text-sm font-medium text-foreground">{t("resources.documentation")}</span>
                  </div>
                  <ExternalLink className="h-3.5 w-3.5 text-muted-foreground" />
                </a>
                <a
                  href="https://youtube.com/@geneflow"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="group flex items-center justify-between rounded-lg p-3 transition-all hover:bg-muted/50"
                >
                  <div className="flex items-center gap-3">
                    <PlayCircle className="h-4 w-4 text-blue-deep" />
                    <span className="text-sm font-medium text-foreground">{t("resources.videoTutorials")}</span>
                  </div>
                  <ExternalLink className="h-3.5 w-3.5 text-muted-foreground" />
                </a>
                <a
                  href="https://api.geneflow.io/docs"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="group flex items-center justify-between rounded-lg p-3 transition-all hover:bg-muted/50"
                >
                  <div className="flex items-center gap-3">
                    <Globe className="h-4 w-4 text-teal" />
                    <span className="text-sm font-medium text-foreground">{t("resources.apiReference")}</span>
                  </div>
                  <ExternalLink className="h-3.5 w-3.5 text-muted-foreground" />
                </a>
                <a
                  href="https://geneflow.io/sample-data"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="group flex items-center justify-between rounded-lg p-3 transition-all hover:bg-muted/50"
                >
                  <div className="flex items-center gap-3">
                    <Download className="h-4 w-4 text-blue-deep" />
                    <span className="text-sm font-medium text-foreground">{t("resources.sampleData")}</span>
                  </div>
                  <ExternalLink className="h-3.5 w-3.5 text-muted-foreground" />
                </a>
              </div>
            </div>

            {/* Status */}
            <div className="rounded-xl border border-border bg-card p-6">
              <div className="mb-3 flex items-center justify-between">
                <h3 className="text-base font-semibold text-foreground">{t("systemStatus.title")}</h3>
                <span className="flex items-center gap-2 text-xs font-medium text-emerald-500">
                  <div className="h-2 w-2 rounded-full bg-emerald-500" />
                  {t("systemStatus.operational")}
                </span>
              </div>
              <p className="mb-3 text-sm text-muted-foreground">{t("systemStatus.description")}</p>
              <a
                href="https://status.geneflow.io"
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-1 text-sm font-medium text-teal hover:text-teal/80"
              >
                {t("systemStatus.viewStatus")}
                <ExternalLink className="h-3 w-3" />
              </a>
            </div>
          </div>
        </div>

        {/* Additional Help Section */}
        <div className="rounded-xl border border-border bg-gradient-to-br from-teal/5 to-blue-deep/5 p-8 text-center">
          <FileQuestion className="mx-auto mb-4 h-12 w-12 text-teal" />
          <h2 className="mb-2 text-xl font-semibold text-foreground">{t("stillNeedHelp.title")}</h2>
          <p className="mx-auto mb-6 max-w-[600px] text-sm text-muted-foreground">
            {t("stillNeedHelp.description")}
          </p>
          <div className="flex items-center justify-center gap-3">
            <button
              onClick={() => setContactOpen(true)}
              className="flex items-center gap-2 rounded-lg bg-teal px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-teal/90 hover:shadow-md"
            >
              <Mail className="h-4 w-4" />
              {t("stillNeedHelp.contactSupport")}
            </button>
            <a
              href="https://docs.geneflow.io"
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-2 rounded-lg border border-border px-5 py-2.5 text-sm font-medium text-foreground transition-all hover:bg-muted/50"
            >
              <Book className="h-4 w-4" />
              {t("stillNeedHelp.browseDocumentation")}
            </a>
          </div>
        </div>
      </div>

      {/* Contact Support Dialog */}
      <Dialog open={contactOpen} onOpenChange={setContactOpen}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.contact.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.contact.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("dialogs.contact.subject")}</label>
              <input
                type="text"
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
                placeholder={t("dialogs.contact.subjectPlaceholder")}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("dialogs.contact.category")}</label>
              <select className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm">
                <option>{t("dialogs.contact.selectCategory")}</option>
                <option>{t("dialogs.contact.categories.technical")}</option>
                <option>{t("dialogs.contact.categories.billing")}</option>
                <option>{t("dialogs.contact.categories.feature")}</option>
                <option>{t("dialogs.contact.categories.account")}</option>
                <option>{t("dialogs.contact.categories.other")}</option>
              </select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("dialogs.contact.message")}</label>
              <textarea
                rows={5}
                className="w-full resize-none rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
                placeholder={t("dialogs.contact.messagePlaceholder")}
              />
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => setContactOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={() => {
                console.log("Sending support message");
                setContactOpen(false);
              }}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90"
            >
              {t("dialogs.contact.send")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Live Chat Dialog */}
      <Dialog open={liveChatOpen} onOpenChange={setLiveChatOpen}>
        <DialogContent className="sm:max-w-[440px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.liveChat.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.liveChat.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <div className="h-[300px] overflow-y-auto rounded-lg border border-border bg-muted/20 p-4">
              <div className="space-y-4">
                {/* Welcome message */}
                <div className="flex justify-start">
                  <div className="max-w-[80%] rounded-lg border border-border bg-card px-4 py-2 text-sm text-foreground">
                    {t("dialogs.liveChat.welcomeMessage")}
                  </div>
                </div>
                {chatMessages.map((message, idx) => (
                  <div
                    key={idx}
                    className={cn(
                      "flex",
                      message.sender === "user" ? "justify-end" : "justify-start"
                    )}
                  >
                    <div
                      className={cn(
                        "max-w-[80%] rounded-lg px-4 py-2 text-sm",
                        message.sender === "user"
                          ? "bg-teal text-white"
                          : "bg-card border border-border text-foreground"
                      )}
                    >
                      {message.text}
                    </div>
                  </div>
                ))}
              </div>
            </div>
            <div className="mt-4 flex gap-2">
              <input
                type="text"
                value={chatInput}
                onChange={(e) => setChatInput(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && handleSendMessage()}
                className="flex-1 rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
                placeholder={t("dialogs.liveChat.placeholder")}
              />
              <button
                onClick={handleSendMessage}
                className="rounded-lg bg-teal p-2.5 text-white hover:bg-teal/90"
              >
                <Send className="h-4 w-4" />
              </button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

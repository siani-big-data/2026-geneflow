"use client";

import { useState } from "react";
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
    title: "Getting Started",
    icon: Zap,
    color: "text-teal",
    bgColor: "bg-teal/10",
    topics: [
      "Creating your first study",
      "Setting up your profile",
      "Inviting team members",
      "Understanding the dashboard",
    ],
  },
  {
    title: "Studies & Data",
    icon: FileText,
    color: "text-blue-deep",
    bgColor: "bg-blue-deep/10",
    topics: [
      "Managing study metadata",
      "Uploading chromatogram traces",
      "Organizing samples",
      "Sharing studies with collaborators",
    ],
  },
  {
    title: "Pipelines",
    icon: BarChart3,
    color: "text-teal",
    bgColor: "bg-teal/10",
    topics: [
      "Running analysis pipelines",
      "Understanding results",
      "Quality control metrics",
      "Exporting analysis data",
    ],
  },
  {
    title: "Collaboration",
    icon: Users,
    color: "text-blue-deep",
    bgColor: "bg-blue-deep/10",
    topics: [
      "Team roles and permissions",
      "Inviting collaborators",
      "Sharing data securely",
      "Managing access controls",
    ],
  },
  {
    title: "Security & Privacy",
    icon: Shield,
    color: "text-teal",
    bgColor: "bg-teal/10",
    topics: [
      "Two-factor authentication",
      "Data encryption",
      "Compliance guidelines",
      "Access logs and auditing",
    ],
  },
  {
    title: "Account & Settings",
    icon: Settings,
    color: "text-blue-deep",
    bgColor: "bg-blue-deep/10",
    topics: [
      "Profile customization",
      "Notification preferences",
      "Billing and subscriptions",
      "API access tokens",
    ],
  },
];

const commonQuestions = [
  {
    question: "How do I upload chromatogram traces to a study?",
    answer:
      "Navigate to your study's Traces page and click the 'Upload Traces' button. You can drag and drop multiple .ab1, .scf, or .abi files, or click to browse. Each trace will be automatically associated with the study and processed for quality metrics.",
  },
  {
    question: "What analysis pipelines are available?",
    answer:
      "GeneFlow offers several pre-configured pipelines including Base Calling, Quality Control, Variant Calling, Sequence Alignment, and Custom Assembly. Each pipeline can be customized with specific parameters to match your research needs. Visit the Pipelines page to explore all available options.",
  },
  {
    question: "How do I invite team members to collaborate on a study?",
    answer:
      "Open the study you want to share, click 'Share' in the top-right corner, and enter the email addresses of your collaborators. You can assign different roles (Viewer, Collaborator, Co-Investigator) with varying permission levels. Invitations will be sent via email.",
  },
  {
    question: "Can I export my analysis results?",
    answer:
      "Yes, all analysis results can be exported in multiple formats. Click the 'Export' button on any analysis page and choose from CSV, JSON, or PDF formats. You can also download raw trace files and processed data for external analysis.",
  },
  {
    question: "How is my genomic data protected?",
    answer:
      "All data is encrypted at rest using AES-256 encryption and in transit using TLS 1.3. We maintain SOC 2 Type II compliance and follow HIPAA guidelines. Access controls ensure only authorized team members can view sensitive data. Learn more in our Security documentation.",
  },
  {
    question: "What file formats are supported for trace uploads?",
    answer:
      "GeneFlow supports standard chromatogram formats including .ab1 (Applied Biosystems), .scf (Standard Chromatogram Format), and .abi files. Batch uploads of up to 1,000 files are supported, with a maximum file size of 50MB per trace.",
  },
];

const quickStartGuides = [
  {
    title: "Create Your First Study",
    description: "Step-by-step guide to setting up a new research study",
    duration: "5 min",
  },
  {
    title: "Upload & Manage Traces",
    description: "Learn how to upload and organize chromatogram data",
    duration: "8 min",
  },
  {
    title: "Run an Analysis Pipeline",
    description: "Execute your first genomic analysis workflow",
    duration: "10 min",
  },
];

const workflows = [
  {
    title: "Quality Control Workflow",
    description: "Evaluate trace quality and filter low-quality samples",
    steps: ["Upload traces", "Run QC pipeline", "Review metrics", "Filter samples"],
  },
  {
    title: "Variant Calling Workflow",
    description: "Identify genetic variants from sequencing data",
    steps: ["Align sequences", "Call variants", "Annotate results", "Export VCF"],
  },
  {
    title: "Collaborative Research Workflow",
    description: "Share studies and collaborate with research teams",
    steps: ["Create study", "Invite members", "Assign roles", "Share results"],
  },
];

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
  const [searchQuery, setSearchQuery] = useState("");
  const [openFaqIndex, setOpenFaqIndex] = useState<number | null>(null);
  const [contactOpen, setContactOpen] = useState(false);
  const [liveChatOpen, setLiveChatOpen] = useState(false);
  const [chatMessages, setChatMessages] = useState<{ sender: "user" | "support"; text: string }[]>([
    { sender: "support", text: "Hello! How can I help you today?" }
  ]);
  const [chatInput, setChatInput] = useState("");

  const handleSendMessage = () => {
    if (chatInput.trim()) {
      setChatMessages([...chatMessages, { sender: "user", text: chatInput }]);
      setChatInput("");
      // Simulate support response
      setTimeout(() => {
        setChatMessages(prev => [...prev, {
          sender: "support",
          text: "Thank you for your message. A support agent will respond shortly. In the meantime, you can check our FAQ section for quick answers."
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
            <h1 className="mb-3 text-3xl font-semibold text-foreground">How can we help you?</h1>
            <p className="mb-8 text-base text-muted-foreground">
              Search our knowledge base or browse topics to find answers
            </p>

            {/* Search Bar */}
            <div className="relative">
              <Search className="absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-muted-foreground" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search for help articles, guides, or tutorials..."
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
              <h2 className="mb-1 text-xl font-semibold text-foreground">Quick Start Guides</h2>
              <p className="text-sm text-muted-foreground">Get started with essential workflows</p>
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
                      {guide.title}
                    </h3>
                    <p className="text-xs text-muted-foreground">{guide.duration} read</p>
                  </div>
                </div>
                <p className="text-sm text-muted-foreground">{guide.description}</p>
              </div>
            ))}
          </div>
        </div>

        {/* Help Categories */}
        <div className="mb-12">
          <div className="mb-6">
            <h2 className="mb-1 text-xl font-semibold text-foreground">Browse by Topic</h2>
            <p className="text-sm text-muted-foreground">Explore help articles organized by category</p>
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
                    <h3 className="font-semibold text-foreground">{category.title}</h3>
                  </div>
                  <ul className="space-y-2.5">
                    {category.topics.map((topic, topicIdx) => (
                      <li key={topicIdx}>
                        <button
                          onClick={() => handleTopicClick(topic)}
                          className="group/item flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-teal"
                        >
                          <ChevronRight className="h-3.5 w-3.5 opacity-0 transition-opacity group-hover/item:opacity-100" />
                          <span>{topic}</span>
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
            <h2 className="mb-1 text-xl font-semibold text-foreground">Common Workflows</h2>
            <p className="text-sm text-muted-foreground">Step-by-step guides for key research tasks</p>
          </div>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            {workflows.map((workflow, idx) => (
              <div
                key={idx}
                className="rounded-xl border border-border bg-card p-5 transition-all hover:border-teal/30 hover:shadow-md"
              >
                <h3 className="mb-2 font-medium text-foreground">{workflow.title}</h3>
                <p className="mb-4 text-sm text-muted-foreground">{workflow.description}</p>
                <div className="space-y-2">
                  {workflow.steps.map((step, stepIdx) => (
                    <div key={stepIdx} className="flex items-center gap-2.5">
                      <div className="flex h-6 w-6 flex-shrink-0 items-center justify-center rounded-full bg-teal/10 text-xs font-medium text-teal">
                        {stepIdx + 1}
                      </div>
                      <span className="text-sm text-muted-foreground">{step}</span>
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
              <h2 className="mb-1 text-xl font-semibold text-foreground">Frequently Asked Questions</h2>
              <p className="text-sm text-muted-foreground">Quick answers to common questions</p>
            </div>
            <div className="rounded-xl border border-border bg-card">
              <div className="divide-y divide-border px-6">
                {commonQuestions.map((item, idx) => (
                  <AccordionItem
                    key={idx}
                    question={item.question}
                    answer={item.answer}
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
              <h3 className="mb-4 text-base font-semibold text-foreground">Contact Support</h3>
              <p className="mb-5 text-sm text-muted-foreground">
                Can't find what you're looking for? Our support team is here to help.
              </p>
              <div className="space-y-3">
                <button
                  onClick={() => setContactOpen(true)}
                  className="group flex w-full items-center gap-3 rounded-lg bg-teal p-3 text-white transition-all hover:bg-teal/90"
                >
                  <Mail className="h-4 w-4" />
                  <div className="flex-1 text-left">
                    <p className="text-sm font-medium">Email Support</p>
                    <p className="text-xs opacity-90">Response in 24 hours</p>
                  </div>
                </button>
                <button
                  onClick={() => setLiveChatOpen(true)}
                  className="group flex w-full items-center gap-3 rounded-lg border border-border p-3 transition-all hover:bg-muted/50"
                >
                  <MessageCircle className="h-4 w-4 text-foreground" />
                  <div className="flex-1 text-left">
                    <p className="text-sm font-medium text-foreground">Live Chat</p>
                    <p className="text-xs text-muted-foreground">Available 9am-5pm PT</p>
                  </div>
                </button>
              </div>
            </div>

            {/* Resources */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h3 className="mb-4 text-base font-semibold text-foreground">Resources</h3>
              <div className="space-y-3">
                <a
                  href="https://docs.geneflow.io"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="group flex items-center justify-between rounded-lg p-3 transition-all hover:bg-muted/50"
                >
                  <div className="flex items-center gap-3">
                    <Book className="h-4 w-4 text-teal" />
                    <span className="text-sm font-medium text-foreground">Documentation</span>
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
                    <span className="text-sm font-medium text-foreground">Video Tutorials</span>
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
                    <span className="text-sm font-medium text-foreground">API Reference</span>
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
                    <span className="text-sm font-medium text-foreground">Sample Data</span>
                  </div>
                  <ExternalLink className="h-3.5 w-3.5 text-muted-foreground" />
                </a>
              </div>
            </div>

            {/* Status */}
            <div className="rounded-xl border border-border bg-card p-6">
              <div className="mb-3 flex items-center justify-between">
                <h3 className="text-base font-semibold text-foreground">System Status</h3>
                <span className="flex items-center gap-2 text-xs font-medium text-emerald-500">
                  <div className="h-2 w-2 rounded-full bg-emerald-500" />
                  All Systems Operational
                </span>
              </div>
              <p className="mb-3 text-sm text-muted-foreground">All services are running normally.</p>
              <a
                href="https://status.geneflow.io"
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-1 text-sm font-medium text-teal hover:text-teal/80"
              >
                View Status Page
                <ExternalLink className="h-3 w-3" />
              </a>
            </div>
          </div>
        </div>

        {/* Additional Help Section */}
        <div className="rounded-xl border border-border bg-gradient-to-br from-teal/5 to-blue-deep/5 p-8 text-center">
          <FileQuestion className="mx-auto mb-4 h-12 w-12 text-teal" />
          <h2 className="mb-2 text-xl font-semibold text-foreground">Still need help?</h2>
          <p className="mx-auto mb-6 max-w-[600px] text-sm text-muted-foreground">
            Our support team is available to answer your questions and help you get the most out of GeneFlow.
          </p>
          <div className="flex items-center justify-center gap-3">
            <button
              onClick={() => setContactOpen(true)}
              className="flex items-center gap-2 rounded-lg bg-teal px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-teal/90 hover:shadow-md"
            >
              <Mail className="h-4 w-4" />
              Contact Support
            </button>
            <a
              href="https://docs.geneflow.io"
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-2 rounded-lg border border-border px-5 py-2.5 text-sm font-medium text-foreground transition-all hover:bg-muted/50"
            >
              <Book className="h-4 w-4" />
              Browse Documentation
            </a>
          </div>
        </div>
      </div>

      {/* Contact Support Dialog */}
      <Dialog open={contactOpen} onOpenChange={setContactOpen}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>Contact Support</DialogTitle>
            <DialogDescription>
              Send us a message and we&apos;ll respond within 24 hours.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">Subject</label>
              <input
                type="text"
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
                placeholder="Brief description of your issue"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">Category</label>
              <select className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm">
                <option>Select a category...</option>
                <option>Technical Issue</option>
                <option>Billing Question</option>
                <option>Feature Request</option>
                <option>Account Help</option>
                <option>Other</option>
              </select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">Message</label>
              <textarea
                rows={5}
                className="w-full resize-none rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
                placeholder="Describe your question or issue in detail..."
              />
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => setContactOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              Cancel
            </button>
            <button
              onClick={() => {
                console.log("Sending support message");
                setContactOpen(false);
              }}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90"
            >
              Send Message
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Live Chat Dialog */}
      <Dialog open={liveChatOpen} onOpenChange={setLiveChatOpen}>
        <DialogContent className="sm:max-w-[440px]">
          <DialogHeader>
            <DialogTitle>Live Chat Support</DialogTitle>
            <DialogDescription>
              Chat with our support team in real-time.
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <div className="h-[300px] overflow-y-auto rounded-lg border border-border bg-muted/20 p-4">
              <div className="space-y-4">
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
                placeholder="Type your message..."
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

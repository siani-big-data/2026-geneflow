"use client";

import Image from "next/image";
import { useState } from "react";
import { useTranslations } from "next-intl";
import {
  Dna,
  BarChart3,
  GitBranch,
  Users,
  Shield,
  Zap,
  ChevronRight,
  Menu,
  X,
  Check,
  ArrowRight,
  Play,
  Star,
  FlaskConical,
  FileSearch,
  Share2,
  Moon,
  Sun,
} from "lucide-react";
import { Button } from "@/components/ui";
import { LocaleSwitcher } from "@/components/shared";
import { Link } from "@/lib/navigation";
import { useTheme } from "@/providers";
import { cn } from "@/lib/utils";

// Testimonials remain static (not translatable content - names, institutions)
const testimonials = [
  {
    quote:
      "GeneFlow transformed how our lab handles sequencing data. The trace visualization alone saved us countless hours.",
    author: "Dr. Sarah Chen",
    role: "Principal Investigator",
    institution: "Stanford Genomics Lab",
    avatar: "SC",
  },
  {
    quote:
      "The study management is incredibly intuitive. We went from scattered data to organized workflows in a week.",
    author: "Dr. Marcus Williams",
    role: "Bioinformatics Lead",
    institution: "Broad Institute",
    avatar: "MW",
  },
  {
    quote:
      "Finally, a platform that understands the needs of modern genomics research. The collaboration features are game-changing.",
    author: "Dr. Elena Rodriguez",
    role: "Lab Director",
    institution: "UCSD Health",
    avatar: "ER",
  },
];

export default function LandingPage() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const { theme, setTheme, resolvedTheme } = useTheme();
  const t = useTranslations("landing");

  const toggleTheme = () => {
    if (theme === "system") {
      setTheme(resolvedTheme === "dark" ? "light" : "dark");
    } else {
      setTheme(theme === "dark" ? "light" : "dark");
    }
  };

  // Navigation items with translation keys
  const navigation = [
    { name: t("nav.features"), href: "#features" },
    { name: t("nav.howItWorks"), href: "#how-it-works" },
    { name: t("nav.pricing"), href: "#pricing" },
    { name: t("nav.about"), href: "#about" },
  ];

  // Features with translations
  const features = [
    {
      icon: FlaskConical,
      title: t("features.studyManagement"),
      description: t("features.studyManagementDesc"),
    },
    {
      icon: Dna,
      title: t("features.traceVisualization"),
      description: t("features.traceVisualizationDesc"),
    },
    {
      icon: GitBranch,
      title: t("features.pipelineOrchestration"),
      description: t("features.pipelineOrchestrationDesc"),
    },
    {
      icon: BarChart3,
      title: t("features.advancedAnalytics"),
      description: t("features.advancedAnalyticsDesc"),
    },
    {
      icon: Users,
      title: t("features.teamCollaboration"),
      description: t("features.teamCollaborationDesc"),
    },
    {
      icon: Shield,
      title: t("features.enterpriseSecurity"),
      description: t("features.enterpriseSecurityDesc"),
    },
  ];

  // How it works steps with translations
  const steps = [
    {
      step: 1,
      title: t("howItWorks.step1Title"),
      description: t("howItWorks.step1Desc"),
      icon: FileSearch,
    },
    {
      step: 2,
      title: t("howItWorks.step2Title"),
      description: t("howItWorks.step2Desc"),
      icon: GitBranch,
    },
    {
      step: 3,
      title: t("howItWorks.step3Title"),
      description: t("howItWorks.step3Desc"),
      icon: Share2,
    },
  ];

  // Stats with translations
  const stats = [
    { value: "50K+", label: t("stats.studiesAnalyzed") },
    { value: "2M+", label: t("stats.samplesProcessed") },
    { value: "500+", label: t("stats.researchInstitutions") },
    { value: "99.9%", label: t("stats.uptimeSLA") },
  ];

  // Pricing plans with translations
  const plans = [
    {
      name: t("pricing.starter"),
      price: t("pricing.free"),
      description: t("pricing.starterDesc"),
      features: [
        t("pricing.features.samplesMonth100"),
        t("pricing.features.basicTrace"),
        t("pricing.features.pipelines3"),
        t("pricing.features.communitySupport"),
        t("pricing.features.retention7"),
      ],
      cta: t("pricing.getStarted"),
      highlighted: false,
    },
    {
      name: t("pricing.professional"),
      price: "$99",
      period: t("pricing.perMonth"),
      description: t("pricing.professionalDesc"),
      features: [
        t("pricing.features.samplesMonth5000"),
        t("pricing.features.advancedTrace"),
        t("pricing.features.unlimitedPipelines"),
        t("pricing.features.prioritySupport"),
        t("pricing.features.retention1Year"),
        t("pricing.features.teamCollab10"),
        t("pricing.features.customWorkflows"),
        t("pricing.features.apiAccess"),
      ],
      cta: t("pricing.startFreeTrial"),
      highlighted: true,
    },
    {
      name: t("pricing.enterprise"),
      price: t("pricing.custom"),
      description: t("pricing.enterpriseDesc"),
      features: [
        t("pricing.features.unlimitedSamples"),
        t("pricing.features.allProfessional"),
        t("pricing.features.dedicatedInfra"),
        t("pricing.features.support247"),
        t("pricing.features.unlimitedRetention"),
        t("pricing.features.unlimitedTeam"),
        t("pricing.features.ssoSaml"),
        t("pricing.features.hipaa"),
        t("pricing.features.customIntegrations"),
      ],
      cta: t("pricing.contactSales"),
      highlighted: false,
    },
  ];

  return (
    <div className="min-h-screen bg-background">
      {/* Skip to main content link for accessibility */}
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 focus:z-[60] focus:px-4 focus:py-2 focus:bg-teal focus:text-white focus:rounded-md focus:outline-none"
      >
        Skip to main content
      </a>

      {/* Navigation */}
      <header className="fixed top-0 left-0 right-0 z-50 bg-background/80 backdrop-blur-md border-b border-border">
        <nav className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="flex h-16 items-center justify-between">
            <div className="flex items-center gap-2">
              <Image
                src="/logo.png"
                alt="GeneFlow"
                width={40}
                height={40}
                className="rounded-lg object-contain"
              />
              <span className="text-xl font-bold">GeneFlow</span>
            </div>

            {/* Desktop nav */}
            <div className="hidden md:flex md:items-center md:gap-8">
              {navigation.map((item) => (
                <a
                  key={item.name}
                  href={item.href}
                  className="text-sm font-medium text-muted-foreground hover:text-foreground transition-colors"
                >
                  {item.name}
                </a>
              ))}
            </div>

            <div className="hidden md:flex md:items-center md:gap-4">
              <button
                onClick={toggleTheme}
                className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                aria-label={resolvedTheme === "light" ? "Switch to dark mode" : "Switch to light mode"}
              >
                {resolvedTheme === "light" ? (
                  <Moon className="h-5 w-5" />
                ) : (
                  <Sun className="h-5 w-5" />
                )}
              </button>
              <LocaleSwitcher />
              <Link href="/login">
                <Button variant="ghost" size="sm">
                  {t("nav.signIn")}
                </Button>
              </Link>
              <Link href="/register">
                <Button size="sm">
                  {t("nav.getStarted")}
                  <ChevronRight className="ml-1 h-4 w-4" />
                </Button>
              </Link>
            </div>

            {/* Mobile menu buttons */}
            <div className="flex md:hidden items-center gap-2">
              <button
                onClick={toggleTheme}
                className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                aria-label={resolvedTheme === "light" ? "Switch to dark mode" : "Switch to light mode"}
              >
                {resolvedTheme === "light" ? (
                  <Moon className="h-5 w-5" />
                ) : (
                  <Sun className="h-5 w-5" />
                )}
              </button>
              <button
                className="p-2 -mr-2"
                onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
                aria-label={mobileMenuOpen ? "Close menu" : "Open menu"}
                aria-expanded={mobileMenuOpen}
              >
                {mobileMenuOpen ? (
                  <X className="h-6 w-6" />
                ) : (
                  <Menu className="h-6 w-6" />
                )}
              </button>
            </div>
          </div>
        </nav>

        {/* Mobile menu */}
        {mobileMenuOpen && (
          <div className="md:hidden border-t border-border bg-background">
            <div className="px-4 py-4 space-y-3">
              {navigation.map((item) => (
                <a
                  key={item.name}
                  href={item.href}
                  className="block text-base font-medium text-muted-foreground hover:text-foreground"
                  onClick={() => setMobileMenuOpen(false)}
                >
                  {item.name}
                </a>
              ))}
              <div className="pt-4 flex flex-col gap-2">
                <Link href="/login">
                  <Button variant="outline" className="w-full">
                    {t("nav.signIn")}
                  </Button>
                </Link>
                <Link href="/register">
                  <Button className="w-full">{t("nav.getStarted")}</Button>
                </Link>
              </div>
            </div>
          </div>
        )}
      </header>

      <main id="main-content">
        {/* Hero */}
        <section className="relative pt-32 pb-20 sm:pt-40 sm:pb-32 overflow-hidden">
        {/* Background gradient */}
        <div className="absolute inset-0 -z-10">
          <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[800px] h-[600px] bg-teal/10 rounded-full blur-3xl" />
          <div className="absolute top-20 right-0 w-[400px] h-[400px] bg-blue-deep/10 rounded-full blur-3xl" />
        </div>

        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center max-w-4xl mx-auto">
            <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-teal/10 text-teal text-sm font-medium mb-8">
              <Zap className="h-4 w-4" />
              {t("hero.badge")}
            </div>

            <h1 className="text-4xl sm:text-5xl lg:text-6xl font-bold tracking-tight mb-6">
              {t("hero.title")}
              <br />
              <span className="text-teal">{t("hero.titleHighlight")}</span>
            </h1>

            <p className="text-lg sm:text-xl text-muted-foreground max-w-2xl mx-auto mb-10">
              {t("hero.description")}
            </p>

            <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
              <Link href="/register">
                <Button size="lg" className="w-full sm:w-auto">
                  {t("hero.startTrial")}
                  <ArrowRight className="ml-2 h-5 w-5" />
                </Button>
              </Link>
              <Button
                variant="outline"
                size="lg"
                className="w-full sm:w-auto gap-2"
              >
                <Play className="h-5 w-5" />
                {t("hero.watchDemo")}
              </Button>
            </div>

            <p className="mt-6 text-sm text-muted-foreground">
              {t("hero.noCreditCard")}
            </p>
          </div>

          {/* Hero image 3D */}
          <div className="mt-16 relative [perspective:2000px]">
            <div
              className="rounded-xl border border-border bg-card shadow-2xl overflow-hidden
                         [transform:rotateX(10deg)_rotateY(-5deg)]
                         hover:[transform:rotateX(5deg)_rotateY(-2deg)]
                         transition-transform duration-500 ease-out
                         animate-[float_6s_ease-in-out_infinite]"
              style={{ transformStyle: 'preserve-3d' }}
            >
              {/* Glow effect */}
              <div className="absolute -inset-1 bg-gradient-to-r from-teal/20 via-blue-deep/20 to-teal/20 rounded-xl blur-xl opacity-50 -z-10 animate-[pulse_4s_ease-in-out_infinite]" />

              <div className="bg-muted/50 px-4 py-3 border-b border-border flex items-center gap-2">
                <div className="flex gap-1.5">
                  <div className="w-3 h-3 rounded-full bg-destructive/60" />
                  <div className="w-3 h-3 rounded-full bg-yellow-500/60" />
                  <div className="w-3 h-3 rounded-full bg-green-500/60" />
                </div>
                <div className="flex-1 text-center text-sm text-muted-foreground">
                  GeneFlow Dashboard
                </div>
              </div>
              <Image
                src={resolvedTheme === "dark" ? "/img/dashboard-dark.png" : "/img/dashboard-light.png"}
                alt="GeneFlow Dashboard Preview"
                width={1920}
                height={1080}
                className="w-full h-auto"
                priority
              />
            </div>

            {/* Floating cards with 3D */}
            <div
              className="absolute -left-4 top-1/4 hidden lg:block animate-[floatLeft_5s_ease-in-out_infinite]"
              style={{ transform: 'translateZ(60px)' }}
            >
              <div className="bg-card border border-border rounded-lg p-4 shadow-lg backdrop-blur-sm">
                <div className="flex items-center gap-3">
                  <div className="h-10 w-10 rounded-full bg-green-500/10 flex items-center justify-center">
                    <Check className="h-5 w-5 text-green-500" />
                  </div>
                  <div>
                    <p className="font-medium text-sm">{t("hero.pipelineComplete")}</p>
                    <p className="text-xs text-muted-foreground">
                      {t("hero.pipelineDetails")}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <div
              className="absolute -right-4 top-1/3 hidden lg:block animate-[floatRight_4s_ease-in-out_infinite]"
              style={{ transform: 'translateZ(40px)' }}
            >
              <div className="bg-card border border-border rounded-lg p-4 shadow-lg backdrop-blur-sm">
                <div className="flex items-center gap-3">
                  <div className="h-10 w-10 rounded-full bg-teal/10 flex items-center justify-center">
                    <BarChart3 className="h-5 w-5 text-teal" />
                  </div>
                  <div>
                    <p className="font-medium text-sm">{t("hero.qualityScore")}</p>
                    <p className="text-2xl font-bold text-teal">98.5%</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Logos */}
      <section className="py-12 border-y border-border bg-muted/30">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <p className="text-center text-sm text-muted-foreground mb-8">
            {t("logos.trusted")}
          </p>
          <div className="flex flex-wrap items-center justify-center gap-x-12 gap-y-6">
            {[
              "ULPGC",
              "SIANI",
            ].map((name) => (
              <div
                key={name}
                className="text-lg font-semibold text-muted-foreground"
              >
                {name}
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Features */}
      <section id="features" className="py-20 sm:py-32">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <h2 className="text-3xl sm:text-4xl font-bold mb-4">
              {t("features.title")}
            </h2>
            <p className="text-lg text-muted-foreground">
              {t("features.description")}
            </p>
          </div>

          <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
            {features.map((feature) => (
              <div
                key={feature.title}
                className="group relative p-6 rounded-xl border border-border bg-card hover:border-teal/50 hover:shadow-lg transition-all"
              >
                <div className="h-12 w-12 rounded-lg bg-teal/10 flex items-center justify-center mb-4 group-hover:bg-teal/20 transition-colors">
                  <feature.icon className="h-6 w-6 text-teal" />
                </div>
                <h3 className="text-lg font-semibold mb-2">{feature.title}</h3>
                <p className="text-muted-foreground">{feature.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* How it works */}
      <section id="how-it-works" className="py-20 sm:py-32 bg-muted/30">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <h2 className="text-3xl sm:text-4xl font-bold mb-4">
              {t("howItWorks.title")}
            </h2>
            <p className="text-lg text-muted-foreground">
              {t("howItWorks.description")}
            </p>
          </div>

          <div className="grid gap-8 md:grid-cols-3">
            {steps.map((item, index) => (
              <div key={item.step} className="relative">
                {index < steps.length - 1 && (
                  <div className="hidden md:block absolute top-12 left-1/2 w-full h-0.5 bg-border" />
                )}
                <div className="relative bg-card rounded-xl border border-border p-8 text-center">
                  <div className="inline-flex h-12 w-12 items-center justify-center rounded-full bg-teal text-white text-lg font-bold mb-6">
                    {item.step}
                  </div>
                  <div className="h-12 w-12 rounded-lg bg-muted flex items-center justify-center mx-auto mb-4">
                    <item.icon className="h-6 w-6 text-muted-foreground" />
                  </div>
                  <h3 className="text-lg font-semibold mb-2">{item.title}</h3>
                  <p className="text-muted-foreground">{item.description}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Stats */}
      <section className="py-20 sm:py-32">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
            {stats.map((stat) => (
              <div key={stat.label} className="text-center">
                <div className="text-4xl sm:text-5xl font-bold text-teal mb-2">
                  {stat.value}
                </div>
                <div className="text-muted-foreground">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Testimonials */}
      <section id="about" className="py-20 sm:py-32 bg-muted/30">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <h2 className="text-3xl sm:text-4xl font-bold mb-4">
              {t("testimonials.title")}
            </h2>
            <p className="text-lg text-muted-foreground">
              {t("testimonials.description")}
            </p>
          </div>

          <div className="grid gap-8 md:grid-cols-3">
            {testimonials.map((testimonial) => (
              <div
                key={testimonial.author}
                className="bg-card rounded-xl border border-border p-6"
              >
                <div className="flex gap-1 mb-4">
                  {[...Array(5)].map((_, i) => (
                    <Star
                      key={i}
                      className="h-5 w-5 fill-yellow-400 text-yellow-400"
                    />
                  ))}
                </div>
                <blockquote className="text-foreground mb-6">
                  &ldquo;{testimonial.quote}&rdquo;
                </blockquote>
                <div className="flex items-center gap-3">
                  <div className="h-10 w-10 rounded-full bg-teal/10 flex items-center justify-center text-teal font-semibold text-sm">
                    {testimonial.avatar}
                  </div>
                  <div>
                    <div className="font-medium">{testimonial.author}</div>
                    <div className="text-sm text-muted-foreground">
                      {testimonial.role}, {testimonial.institution}
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Pricing */}
      <section id="pricing" className="py-20 sm:py-32">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <h2 className="text-3xl sm:text-4xl font-bold mb-4">
              {t("pricing.title")}
            </h2>
            <p className="text-lg text-muted-foreground">
              {t("pricing.description")}
            </p>
          </div>

          <div className="grid gap-8 lg:grid-cols-3 max-w-5xl mx-auto">
            {plans.map((plan) => (
              <div
                key={plan.name}
                className={cn(
                  "relative rounded-xl border p-8",
                  plan.highlighted
                    ? "border-teal bg-card shadow-lg scale-105"
                    : "border-border bg-card"
                )}
              >
                {plan.highlighted && (
                  <div className="absolute -top-4 left-1/2 -translate-x-1/2 px-4 py-1 bg-teal text-white text-sm font-medium rounded-full">
                    {t("pricing.mostPopular")}
                  </div>
                )}
                <div className="text-center mb-6">
                  <h3 className="text-xl font-semibold mb-2">{plan.name}</h3>
                  <div className="flex items-baseline justify-center gap-1">
                    <span className="text-4xl font-bold">{plan.price}</span>
                    {plan.period && (
                      <span className="text-muted-foreground">
                        {plan.period}
                      </span>
                    )}
                  </div>
                  <p className="text-sm text-muted-foreground mt-2">
                    {plan.description}
                  </p>
                </div>
                <ul className="space-y-3 mb-8">
                  {plan.features.map((feature) => (
                    <li key={feature} className="flex items-start gap-3">
                      <Check className="h-5 w-5 text-teal shrink-0 mt-0.5" />
                      <span className="text-sm">{feature}</span>
                    </li>
                  ))}
                </ul>
                <Button
                  className="w-full"
                  variant={plan.highlighted ? "default" : "outline"}
                >
                  {plan.cta}
                </Button>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-20 sm:py-32 bg-teal">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl sm:text-4xl font-bold text-white mb-4">
            {t("cta.title")}
          </h2>
          <p className="text-lg text-white/80 max-w-2xl mx-auto mb-8">
            {t("cta.description")}
          </p>
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
            <Link href="/register">
              <Button
                size="lg"
                variant="secondary"
                className="w-full sm:w-auto"
              >
                {t("cta.getStartedFree")}
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
            </Link>
            <Button
              size="lg"
              variant="outline"
              className="w-full sm:w-auto bg-transparent text-white border-white hover:bg-white/10"
            >
              {t("cta.scheduleDemo")}
            </Button>
          </div>
        </div>
      </section>
      </main>

      {/* Footer */}
      <footer className="py-12 border-t border-border">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-4 mb-12">
            <div>
              <div className="flex items-center gap-2 mb-4">
                <Image
                  src="/logo.png"
                  alt="GeneFlow"
                  width={36}
                  height={36}
                  className="rounded-lg object-contain"
                />
                <span className="text-lg font-bold">GeneFlow</span>
              </div>
              <p className="text-sm text-muted-foreground mb-4">
                {t("footer.description")}
              </p>
              <div className="flex gap-4 text-sm">
                <a
                  href="#"
                  className="text-muted-foreground hover:text-teal transition-colors font-medium"
                >
                  Twitter
                </a>
                <a
                  href="#"
                  className="text-muted-foreground hover:text-teal transition-colors font-medium"
                >
                  GitHub
                </a>
                <a
                  href="#"
                  className="text-muted-foreground hover:text-teal transition-colors font-medium"
                >
                  LinkedIn
                </a>
              </div>
            </div>

            <div>
              <h3 className="font-semibold mb-4">{t("footer.product")}</h3>
              <ul className="space-y-2 text-sm">
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    {t("footer.features")}
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    {t("footer.pricing")}
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    {t("footer.integrations")}
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    {t("footer.changelog")}
                  </a>
                </li>
              </ul>
            </div>

            <div>
              <h3 className="font-semibold mb-4">{t("footer.resources")}</h3>
              <ul className="space-y-2 text-sm">
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    {t("footer.documentation")}
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    {t("footer.apiReference")}
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    {t("footer.blog")}
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    {t("footer.community")}
                  </a>
                </li>
              </ul>
            </div>

            <div>
              <h3 className="font-semibold mb-4">{t("footer.company")}</h3>
              <ul className="space-y-2 text-sm">
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    {t("footer.about")}
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    {t("footer.careers")}
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    {t("footer.contact")}
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    {t("footer.privacy")}
                  </a>
                </li>
              </ul>
            </div>
          </div>

          <div className="pt-8 border-t border-border flex flex-col sm:flex-row items-center justify-between gap-4">
            <p className="text-sm text-muted-foreground">
              &copy; {new Date().getFullYear()} {t("footer.copyright")}
            </p>
            <div className="flex gap-6 text-sm">
              <a
                href="#"
                className="text-muted-foreground hover:text-foreground"
              >
                {t("footer.terms")}
              </a>
              <a
                href="#"
                className="text-muted-foreground hover:text-foreground"
              >
                {t("footer.privacy")}
              </a>
              <a
                href="#"
                className="text-muted-foreground hover:text-foreground"
              >
                {t("footer.cookies")}
              </a>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}

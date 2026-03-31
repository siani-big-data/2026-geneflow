"use client";

import Link from "next/link";
import Image from "next/image";
import { useState } from "react";
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
import { useTheme } from "@/providers";
import { cn } from "@/lib/utils";

const navigation = [
  { name: "Features", href: "#features" },
  { name: "How it works", href: "#how-it-works" },
  { name: "Pricing", href: "#pricing" },
  { name: "About", href: "#about" },
];

const features = [
  {
    icon: FlaskConical,
    title: "Study Management",
    description:
      "Organize and track genomic studies with comprehensive metadata, sample tracking, and collaboration tools.",
  },
  {
    icon: Dna,
    title: "Trace Visualization",
    description:
      "Analyze Sanger sequencing traces with our interactive chromatogram viewer featuring quality scores and base calling.",
  },
  {
    icon: GitBranch,
    title: "Pipeline Orchestration",
    description:
      "Design, execute, and monitor bioinformatics pipelines with real-time progress tracking and resource management.",
  },
  {
    icon: BarChart3,
    title: "Advanced Analytics",
    description:
      "Generate insights from your genomic data with built-in statistical analysis and customizable visualizations.",
  },
  {
    icon: Users,
    title: "Team Collaboration",
    description:
      "Share studies, assign roles, and collaborate with your team in real-time with granular access controls.",
  },
  {
    icon: Shield,
    title: "Enterprise Security",
    description:
      "HIPAA-compliant infrastructure with end-to-end encryption, audit logs, and SOC 2 Type II certification.",
  },
];

const steps = [
  {
    step: 1,
    title: "Upload Your Data",
    description:
      "Import sequencing files, FASTA/FASTQ data, or connect directly to your sequencer output.",
    icon: FileSearch,
  },
  {
    step: 2,
    title: "Configure Pipelines",
    description:
      "Choose from pre-built workflows or create custom pipelines tailored to your research needs.",
    icon: GitBranch,
  },
  {
    step: 3,
    title: "Analyze & Collaborate",
    description:
      "Explore results with interactive visualizations and share findings with your team.",
    icon: Share2,
  },
];

const stats = [
  { value: "50K+", label: "Studies Analyzed" },
  { value: "2M+", label: "Samples Processed" },
  { value: "500+", label: "Research Institutions" },
  { value: "99.9%", label: "Uptime SLA" },
];

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
      "The pipeline orchestration is incredibly intuitive. We went from manual processing to automated workflows in a week.",
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

const plans = [
  {
    name: "Starter",
    price: "Free",
    description: "Perfect for individual researchers and small projects",
    features: [
      "Up to 100 samples/month",
      "Basic trace visualization",
      "3 concurrent pipelines",
      "Community support",
      "7-day data retention",
    ],
    cta: "Get Started",
    highlighted: false,
  },
  {
    name: "Professional",
    price: "$99",
    period: "/month",
    description: "For research teams that need more power and collaboration",
    features: [
      "Up to 5,000 samples/month",
      "Advanced trace analysis",
      "Unlimited pipelines",
      "Priority support",
      "1-year data retention",
      "Team collaboration (up to 10)",
      "Custom workflows",
      "API access",
    ],
    cta: "Start Free Trial",
    highlighted: true,
  },
  {
    name: "Enterprise",
    price: "Custom",
    description: "For institutions requiring maximum scale and compliance",
    features: [
      "Unlimited samples",
      "All Professional features",
      "Dedicated infrastructure",
      "24/7 premium support",
      "Unlimited retention",
      "Unlimited team size",
      "SSO & SAML",
      "HIPAA compliance",
      "Custom integrations",
    ],
    cta: "Contact Sales",
    highlighted: false,
  },
];

export default function LandingPage() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const { theme, setTheme, resolvedTheme } = useTheme();

  const toggleTheme = () => {
    if (theme === "system") {
      setTheme(resolvedTheme === "dark" ? "light" : "dark");
    } else {
      setTheme(theme === "dark" ? "light" : "dark");
    }
  };

  return (
    <div className="min-h-screen bg-background">
      {/* Navigation */}
      <header className="fixed top-0 left-0 right-0 z-50 bg-background/80 backdrop-blur-md border-b border-border">
        <nav className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="flex h-16 items-center justify-between">
            <div className="flex items-center gap-2">
              <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-teal text-white">
                <Dna className="h-5 w-5" />
              </div>
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
              <Link href="/login">
                <Button variant="ghost" size="sm">
                  Sign in
                </Button>
              </Link>
              <Link href="/register">
                <Button size="sm">
                  Get Started
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
                    Sign in
                  </Button>
                </Link>
                <Link href="/register">
                  <Button className="w-full">Get Started</Button>
                </Link>
              </div>
            </div>
          </div>
        )}
      </header>

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
              Now with AI-powered variant calling
            </div>

            <h1 className="text-4xl sm:text-5xl lg:text-6xl font-bold tracking-tight mb-6">
              Modern Bioinformatics
              <br />
              <span className="text-teal">Made Simple</span>
            </h1>

            <p className="text-lg sm:text-xl text-muted-foreground max-w-2xl mx-auto mb-10">
              The all-in-one platform for genomic study management, trace
              visualization, and pipeline orchestration. Accelerate your
              research with powerful tools designed for scientists.
            </p>

            <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
              <Link href="/register">
                <Button size="lg" className="w-full sm:w-auto">
                  Start Free Trial
                  <ArrowRight className="ml-2 h-5 w-5" />
                </Button>
              </Link>
              <Button
                variant="outline"
                size="lg"
                className="w-full sm:w-auto gap-2"
              >
                <Play className="h-5 w-5" />
                Watch Demo
              </Button>
            </div>

            <p className="mt-6 text-sm text-muted-foreground">
              No credit card required. Free plan available forever.
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
                src={resolvedTheme === "dark" ? "/dashboard-dark.png" : "/dashboard-light.png"}
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
                    <p className="font-medium text-sm">Pipeline Complete</p>
                    <p className="text-xs text-muted-foreground">
                      WGS Analysis - 2,340 samples
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
                    <p className="font-medium text-sm">Quality Score</p>
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
            Trusted by leading research institutions worldwide
          </p>
          <div className="flex flex-wrap items-center justify-center gap-x-12 gap-y-6">
            {[
              "Stanford Medicine",
              "Broad Institute",
              "NIH",
              "EMBL",
              "Sanger Institute",
            ].map((name) => (
              <div
                key={name}
                className="text-lg font-semibold text-muted-foreground/50"
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
              Everything you need for genomics research
            </h2>
            <p className="text-lg text-muted-foreground">
              A comprehensive suite of tools designed to streamline your
              workflow from sample to insight.
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
              Get started in minutes
            </h2>
            <p className="text-lg text-muted-foreground">
              Our intuitive workflow gets you from raw data to actionable
              insights faster than ever.
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
              Loved by researchers worldwide
            </h2>
            <p className="text-lg text-muted-foreground">
              See what scientists and bioinformaticians are saying about
              GeneFlow.
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
              Simple, transparent pricing
            </h2>
            <p className="text-lg text-muted-foreground">
              Start free and scale as your research grows. No hidden fees.
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
                    Most Popular
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
            Ready to accelerate your research?
          </h2>
          <p className="text-lg text-white/80 max-w-2xl mx-auto mb-8">
            Join thousands of researchers using GeneFlow to transform their
            genomics workflow.
          </p>
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
            <Link href="/register">
              <Button
                size="lg"
                variant="secondary"
                className="w-full sm:w-auto"
              >
                Get Started Free
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
            </Link>
            <Button
              size="lg"
              variant="outline"
              className="w-full sm:w-auto bg-transparent text-white border-white hover:bg-white/10"
            >
              Schedule a Demo
            </Button>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="py-12 border-t border-border">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-4 mb-12">
            <div>
              <div className="flex items-center gap-2 mb-4">
                <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-teal text-white">
                  <Dna className="h-4 w-4" />
                </div>
                <span className="text-lg font-bold">GeneFlow</span>
              </div>
              <p className="text-sm text-muted-foreground mb-4">
                Modern bioinformatics platform for the next generation of
                genomics research.
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
              <h4 className="font-semibold mb-4">Product</h4>
              <ul className="space-y-2 text-sm">
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    Features
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    Pricing
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    Integrations
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    Changelog
                  </a>
                </li>
              </ul>
            </div>

            <div>
              <h4 className="font-semibold mb-4">Resources</h4>
              <ul className="space-y-2 text-sm">
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    Documentation
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    API Reference
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    Blog
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    Community
                  </a>
                </li>
              </ul>
            </div>

            <div>
              <h4 className="font-semibold mb-4">Company</h4>
              <ul className="space-y-2 text-sm">
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    About
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    Careers
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    Contact
                  </a>
                </li>
                <li>
                  <a
                    href="#"
                    className="text-muted-foreground hover:text-foreground"
                  >
                    Privacy
                  </a>
                </li>
              </ul>
            </div>
          </div>

          <div className="pt-8 border-t border-border flex flex-col sm:flex-row items-center justify-between gap-4">
            <p className="text-sm text-muted-foreground">
              &copy; {new Date().getFullYear()} GeneFlow. All rights reserved.
            </p>
            <div className="flex gap-6 text-sm">
              <a
                href="#"
                className="text-muted-foreground hover:text-foreground"
              >
                Terms
              </a>
              <a
                href="#"
                className="text-muted-foreground hover:text-foreground"
              >
                Privacy
              </a>
              <a
                href="#"
                className="text-muted-foreground hover:text-foreground"
              >
                Cookies
              </a>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}

"use client";

import { useState } from "react";
import Link from "next/link";
import {
  Search,
  Filter,
  Globe,
  Users,
  FileText,
  Award,
  BookOpen,
  Eye,
  ChevronDown,
  Grid3X3,
  List,
} from "lucide-react";
import { cn } from "@/lib/utils";

const researchFields = [
  "All Fields",
  "Cancer Research",
  "Cardiovascular Disease",
  "Diabetes Research",
  "Rare Disease",
  "Microbiome",
  "Pharmacogenomics",
  "Population Genetics",
  "Infectious Disease",
];

const publicStudies = [
  {
    id: "GF-2026-089",
    title: "Genome-Wide Association Study - Type 2 Diabetes Cohort",
    institution: "Stanford Medical Center",
    pi: "Dr. Sarah Martinez",
    field: "Diabetes Research",
    description:
      "Comprehensive GWAS investigating genetic variants associated with Type 2 Diabetes in a diverse population cohort of 1,247 participants.",
    traces: 8934,
    samples: 1247,
    collaborators: 5,
    visibility: "Public",
    featured: true,
    views: 2847,
    citations: 12,
    lastUpdated: "2 hours ago",
    tags: ["GWAS", "Type 2 Diabetes", "Population Study"],
  },
  {
    id: "GF-2026-085",
    title: "RNA-Seq Analysis - Cancer Biomarkers Discovery",
    institution: "Johns Hopkins University",
    pi: "Dr. Emily Chen",
    field: "Cancer Research",
    description:
      "Identification and validation of novel RNA biomarkers for early detection of pancreatic cancer using transcriptome sequencing.",
    traces: 6123,
    samples: 856,
    collaborators: 8,
    visibility: "Public",
    featured: true,
    views: 3452,
    citations: 24,
    lastUpdated: "5 hours ago",
    tags: ["RNA-Seq", "Biomarkers", "Pancreatic Cancer"],
  },
  {
    id: "GF-2026-078",
    title: "Whole Genome Sequencing - Cardiovascular Risk Assessment",
    institution: "Mayo Clinic",
    pi: "Dr. Lisa Anderson",
    field: "Cardiovascular Disease",
    description:
      "Large-scale WGS study examining genetic predisposition to cardiovascular disease across diverse ethnic populations.",
    traces: 15234,
    samples: 2341,
    collaborators: 12,
    visibility: "Public",
    featured: false,
    views: 1923,
    citations: 8,
    lastUpdated: "1 day ago",
    tags: ["WGS", "Cardiovascular", "Risk Assessment"],
  },
  {
    id: "GF-2026-075",
    title: "Microbiome Analysis - IBD Cohort Study",
    institution: "University of California San Francisco",
    pi: "Dr. Robert Kim",
    field: "Microbiome",
    description:
      "Metagenomic sequencing study of gut microbiome composition in inflammatory bowel disease patients.",
    traces: 4521,
    samples: 567,
    collaborators: 6,
    visibility: "Public",
    featured: false,
    views: 1547,
    citations: 15,
    lastUpdated: "2 days ago",
    tags: ["Metagenomics", "IBD", "Gut Microbiome"],
  },
  {
    id: "GF-2026-071",
    title: "Pharmacogenomics - Personalized Drug Response",
    institution: "Harvard Medical School",
    pi: "Dr. Maria Garcia",
    field: "Pharmacogenomics",
    description:
      "Investigation of genetic variants affecting drug metabolism and response in cardiovascular medications.",
    traces: 5832,
    samples: 893,
    collaborators: 4,
    visibility: "Public",
    featured: false,
    views: 2134,
    citations: 19,
    lastUpdated: "3 days ago",
    tags: ["PGx", "Drug Response", "Precision Medicine"],
  },
  {
    id: "GF-2026-068",
    title: "Single Cell RNA-Seq - Tumor Heterogeneity",
    institution: "Memorial Sloan Kettering",
    pi: "Dr. David Lee",
    field: "Cancer Research",
    description:
      "Single-cell transcriptomics revealing intratumoral heterogeneity in triple-negative breast cancer.",
    traces: 3421,
    samples: 214,
    collaborators: 7,
    visibility: "Public",
    featured: true,
    views: 4123,
    citations: 31,
    lastUpdated: "4 days ago",
    tags: ["scRNA-Seq", "TNBC", "Heterogeneity"],
  },
  {
    id: "GF-2026-063",
    title: "Rare Disease Panel - Mendelian Disorders",
    institution: "Baylor College of Medicine",
    pi: "Dr. Michael Torres",
    field: "Rare Disease",
    description:
      "Targeted sequencing panel for diagnosis of rare Mendelian disorders in pediatric patients.",
    traces: 2156,
    samples: 342,
    collaborators: 5,
    visibility: "Public",
    featured: false,
    views: 1876,
    citations: 9,
    lastUpdated: "5 days ago",
    tags: ["Rare Disease", "Mendelian", "Pediatric"],
  },
  {
    id: "GF-2026-058",
    title: "Population Genetics - Human Migration Patterns",
    institution: "Stanford University",
    pi: "Dr. Jennifer Park",
    field: "Population Genetics",
    description:
      "Genomic analysis of ancient DNA to trace human migration patterns across continents.",
    traces: 7845,
    samples: 1523,
    collaborators: 15,
    visibility: "Public",
    featured: false,
    views: 3256,
    citations: 42,
    lastUpdated: "1 week ago",
    tags: ["Population Genetics", "Ancient DNA", "Migration"],
  },
];

export default function DiscoverPage() {
  const [viewMode, setViewMode] = useState<"grid" | "list">("grid");
  const [selectedField, setSelectedField] = useState("All Fields");
  const [searchQuery, setSearchQuery] = useState("");
  const [showFilters, setShowFilters] = useState(false);

  const filteredStudies = publicStudies.filter((study) => {
    const matchesField =
      selectedField === "All Fields" || study.field === selectedField;
    const matchesSearch =
      searchQuery === "" ||
      study.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      study.institution.toLowerCase().includes(searchQuery.toLowerCase()) ||
      study.pi.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesField && matchesSearch;
  });

  const featuredStudies = publicStudies.filter((study) => study.featured);

  return (
    <div className="space-y-8">
      {/* Hero Section */}
      <div className="-mx-16 -mt-10 border-b border-border bg-gradient-to-b from-card to-background px-16 py-12">
        <div className="mx-auto max-w-3xl text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full bg-teal/10 px-3 py-1 text-sm font-medium text-teal">
            <Globe className="h-4 w-4" />
            Open Science Initiative
          </div>
          <h1 className="mb-4 text-3xl font-semibold text-foreground">
            Discover Public Sequencing Studies
          </h1>
          <p className="mb-8 text-base leading-relaxed text-muted-foreground">
            Explore cutting-edge genetic research from leading institutions
            worldwide. Access public datasets, collaborate with researchers, and
            advance open science.
          </p>

          {/* Search Bar */}
          <div className="relative mx-auto max-w-2xl">
            <Search className="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-muted-foreground" />
            <input
              type="text"
              placeholder="Search studies, institutions, or researchers..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full rounded-xl border border-border bg-card py-4 pl-12 pr-4 text-base shadow-sm transition-all duration-200 placeholder:text-muted-foreground/60 hover:shadow-md focus:border-teal/30 focus:outline-none focus:ring-2 focus:ring-teal/20 dark:border-border/50 dark:bg-card/50 dark:hover:bg-card"
            />
          </div>
        </div>
      </div>

      {/* Featured Studies */}
      {featuredStudies.length > 0 &&
        searchQuery === "" &&
        selectedField === "All Fields" && (
          <div>
            <div className="mb-4 flex items-center gap-2">
              <Award className="h-5 w-5 text-amber-500" />
              <h2 className="text-lg font-semibold text-foreground">
                Featured Studies
              </h2>
            </div>
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
              {featuredStudies.slice(0, 3).map((study) => (
                <Link
                  key={study.id}
                  href={`/studies/${study.id}`}
                  className="group rounded-xl border border-teal/20 bg-gradient-to-br from-teal/5 via-card to-blue-deep/5 p-6 transition-all duration-300 hover:-translate-y-1 hover:border-teal/40 hover:shadow-xl hover:shadow-teal/5"
                >
                  <div className="mb-4 flex items-start justify-between">
                    <span className="text-sm font-semibold tracking-tight text-blue-deep">
                      {study.id}
                    </span>
                    <div className="rounded-lg border border-amber-500/20 bg-amber-500/10 p-2">
                      <Award className="h-4 w-4 text-amber-500 transition-transform duration-200 group-hover:rotate-12 group-hover:scale-110" />
                    </div>
                  </div>
                  <h3 className="mb-2 line-clamp-2 font-semibold text-foreground transition-colors duration-200 group-hover:text-teal">
                    {study.title}
                  </h3>
                  <p className="mb-4 text-sm font-medium text-muted-foreground">
                    {study.institution}
                  </p>
                  <div className="flex items-center gap-5 text-xs text-muted-foreground">
                    <span className="flex items-center gap-1.5">
                      <FileText className="h-3.5 w-3.5" strokeWidth={2} />
                      <span className="font-medium">
                        {study.traces.toLocaleString()}
                      </span>
                    </span>
                    <span className="flex items-center gap-1.5">
                      <Eye className="h-3.5 w-3.5" strokeWidth={2} />
                      <span className="font-medium">
                        {study.views.toLocaleString()}
                      </span>
                    </span>
                    <span className="flex items-center gap-1.5">
                      <BookOpen className="h-3.5 w-3.5" strokeWidth={2} />
                      <span className="font-medium">{study.citations}</span>
                    </span>
                  </div>
                </Link>
              ))}
            </div>
          </div>
        )}

      {/* Filters and View Controls */}
      <div className="flex flex-col items-start justify-between gap-4 lg:flex-row lg:items-center">
        <div className="flex flex-wrap items-center gap-2">
          <button
            onClick={() => setShowFilters(!showFilters)}
            className={cn(
              "flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-medium transition-all",
              showFilters
                ? "border-teal/30 bg-teal/10 text-teal"
                : "border-border bg-card text-foreground hover:bg-muted/50"
            )}
          >
            <Filter className="h-4 w-4" />
            Filters
            <ChevronDown
              className={cn(
                "h-3.5 w-3.5 transition-transform",
                showFilters && "rotate-180"
              )}
            />
          </button>

          {/* Research Field Filter */}
          <div className="relative">
            <select
              value={selectedField}
              onChange={(e) => setSelectedField(e.target.value)}
              className="cursor-pointer appearance-none rounded-lg border border-border bg-card py-2 pl-4 pr-10 text-sm font-medium text-foreground transition-all hover:bg-muted/50 focus:outline-none focus:ring-2 focus:ring-teal/20"
            >
              {researchFields.map((field) => (
                <option key={field} value={field}>
                  {field}
                </option>
              ))}
            </select>
            <ChevronDown className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          </div>

          {selectedField !== "All Fields" && (
            <button
              onClick={() => setSelectedField("All Fields")}
              className="text-sm text-muted-foreground transition-colors hover:text-foreground"
            >
              Clear filters
            </button>
          )}
        </div>

        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground">
            {filteredStudies.length}{" "}
            {filteredStudies.length === 1 ? "study" : "studies"}
          </span>
          <div className="h-4 w-px bg-border" />
          <div className="flex items-center gap-1 rounded-lg bg-muted/30 p-1">
            <button
              onClick={() => setViewMode("grid")}
              className={cn(
                "rounded p-1.5 transition-all",
                viewMode === "grid"
                  ? "bg-card text-teal shadow-sm"
                  : "text-muted-foreground hover:text-foreground"
              )}
            >
              <Grid3X3 className="h-4 w-4" />
            </button>
            <button
              onClick={() => setViewMode("list")}
              className={cn(
                "rounded p-1.5 transition-all",
                viewMode === "list"
                  ? "bg-card text-teal shadow-sm"
                  : "text-muted-foreground hover:text-foreground"
              )}
            >
              <List className="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>

      {/* Advanced Filters Panel */}
      {showFilters && (
        <div className="rounded-xl border border-border bg-card p-6">
          <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Visibility
              </label>
              <div className="space-y-2">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    defaultChecked
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">Public Studies</span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">
                    Collaborative Projects
                  </span>
                </label>
              </div>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Study Type
              </label>
              <div className="space-y-2">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    defaultChecked
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">Whole Genome</span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    defaultChecked
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">RNA-Seq</span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    defaultChecked
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">Targeted Panels</span>
                </label>
              </div>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                Sample Size
              </label>
              <div className="space-y-2">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">
                    &lt; 100 samples
                  </span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">
                    100 - 1,000 samples
                  </span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">
                    &gt; 1,000 samples
                  </span>
                </label>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Studies Grid */}
      {viewMode === "grid" ? (
        <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
          {filteredStudies.map((study) => (
            <Link
              key={study.id}
              href={`/studies/${study.id}`}
              className="group overflow-hidden rounded-xl border border-border bg-card transition-all duration-200 hover:-translate-y-0.5 hover:border-teal/40 hover:shadow-lg hover:shadow-black/5 active:scale-[0.99]"
            >
              <div className="p-6">
                <div className="mb-3 flex items-start justify-between">
                  <span className="text-sm font-medium text-blue-deep">
                    {study.id}
                  </span>
                  <div className="flex items-center gap-1.5 rounded-full bg-emerald-500/10 px-2.5 py-1">
                    <Globe className="h-3 w-3 text-emerald-500" />
                    <span className="text-xs font-medium text-emerald-500">
                      {study.visibility}
                    </span>
                  </div>
                </div>

                <h3 className="mb-2 line-clamp-2 font-medium text-foreground transition-colors duration-200 group-hover:text-teal">
                  {study.title}
                </h3>

                <div className="mb-4 space-y-1">
                  <p className="text-sm text-muted-foreground">
                    {study.institution}
                  </p>
                  <p className="text-sm text-muted-foreground">{study.pi}</p>
                </div>

                <p className="mb-4 line-clamp-2 text-sm text-muted-foreground">
                  {study.description}
                </p>

                <div className="mb-4 flex flex-wrap items-center gap-1.5">
                  <span className="rounded-md bg-teal/10 px-2.5 py-1 text-xs font-medium text-teal">
                    {study.field}
                  </span>
                  {study.tags.slice(0, 2).map((tag) => (
                    <span
                      key={tag}
                      className="rounded-md bg-muted px-2.5 py-1 text-xs text-muted-foreground"
                    >
                      {tag}
                    </span>
                  ))}
                </div>

                <div className="mb-4 grid grid-cols-2 gap-3">
                  <div className="rounded-lg bg-muted/30 p-3">
                    <p className="mb-1 text-xs text-muted-foreground">Traces</p>
                    <p className="text-sm font-semibold text-foreground">
                      {study.traces.toLocaleString()}
                    </p>
                  </div>
                  <div className="rounded-lg bg-muted/30 p-3">
                    <p className="mb-1 text-xs text-muted-foreground">Samples</p>
                    <p className="text-sm font-semibold text-foreground">
                      {study.samples.toLocaleString()}
                    </p>
                  </div>
                </div>

                <div className="flex items-center justify-between border-t border-border pt-4">
                  <div className="flex items-center gap-1 text-xs text-muted-foreground">
                    <Users className="h-3.5 w-3.5" />
                    <span>{study.collaborators}</span>
                  </div>
                  <div className="flex items-center gap-3 text-xs text-muted-foreground">
                    <span className="flex items-center gap-1">
                      <Eye className="h-3.5 w-3.5" />
                      {study.views.toLocaleString()}
                    </span>
                    <span className="flex items-center gap-1">
                      <BookOpen className="h-3.5 w-3.5" />
                      {study.citations}
                    </span>
                  </div>
                </div>
              </div>
            </Link>
          ))}
        </div>
      ) : (
        <div className="space-y-4">
          {filteredStudies.map((study) => (
            <Link
              key={study.id}
              href={`/studies/${study.id}`}
              className="group flex gap-6 rounded-xl border border-border bg-card p-6 transition-all duration-200 hover:border-teal/30 hover:shadow-md"
            >
              <div className="flex-1">
                <div className="mb-3 flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <span className="text-sm font-medium text-blue-deep">
                      {study.id}
                    </span>
                    <div className="flex items-center gap-1.5 rounded-full bg-emerald-500/10 px-2.5 py-1">
                      <Globe className="h-3 w-3 text-emerald-500" />
                      <span className="text-xs font-medium text-emerald-500">
                        {study.visibility}
                      </span>
                    </div>
                    <span className="rounded-md bg-teal/10 px-2.5 py-1 text-xs font-medium text-teal">
                      {study.field}
                    </span>
                  </div>
                </div>

                <h3 className="mb-2 text-lg font-medium text-foreground transition-colors duration-200 group-hover:text-teal">
                  {study.title}
                </h3>

                <div className="mb-3 flex items-center gap-4 text-sm text-muted-foreground">
                  <span>{study.institution}</span>
                  <span>•</span>
                  <span>{study.pi}</span>
                </div>

                <p className="mb-4 line-clamp-2 text-sm text-muted-foreground">
                  {study.description}
                </p>

                <div className="flex items-center gap-4 text-sm">
                  <span className="text-muted-foreground">
                    <span className="font-medium text-foreground">
                      {study.traces.toLocaleString()}
                    </span>{" "}
                    traces
                  </span>
                  <span className="text-muted-foreground">
                    <span className="font-medium text-foreground">
                      {study.samples.toLocaleString()}
                    </span>{" "}
                    samples
                  </span>
                  <span className="flex items-center gap-1 text-muted-foreground">
                    <Users className="h-4 w-4" />
                    {study.collaborators}
                  </span>
                  <span className="flex items-center gap-1 text-muted-foreground">
                    <Eye className="h-4 w-4" />
                    {study.views.toLocaleString()}
                  </span>
                  <span className="flex items-center gap-1 text-muted-foreground">
                    <BookOpen className="h-4 w-4" />
                    {study.citations}
                  </span>
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}

      {/* Empty State */}
      {filteredStudies.length === 0 && (
        <div className="py-16 text-center">
          <div className="mb-4 inline-flex items-center justify-center rounded-full bg-muted/50 p-4">
            <Search className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mb-2 text-lg font-medium text-foreground">
            No studies found
          </h3>
          <p className="mb-6 text-sm text-muted-foreground">
            Try adjusting your search or filters to find more studies
          </p>
          <button
            onClick={() => {
              setSearchQuery("");
              setSelectedField("All Fields");
            }}
            className="rounded-lg bg-teal px-4 py-2 text-white transition-all hover:bg-teal/90"
          >
            Clear all filters
          </button>
        </div>
      )}
    </div>
  );
}

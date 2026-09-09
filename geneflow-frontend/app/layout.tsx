import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { ThemeProvider, QueryProvider } from "@/providers";
import { TooltipProvider } from "@/components/ui";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: {
    default: "GeneFlow",
    template: "%s | GeneFlow",
  },
  description: "Modern bioinformatics platform for genomic study management, trace visualization, and pipeline orchestration.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="es"
      className={`${geistSans.variable} ${geistMono.variable}`}
      suppressHydrationWarning
    >
      {/*
        `suppressHydrationWarning` is set because browser extensions such as
        ColorZilla (`cz-shortcut-listen`) and Grammarly mutate <body> after
        the server payload arrives, producing a benign hydration mismatch
        we don't control and can't fix in our own tree.
      */}
      <body
        className="min-h-screen bg-background font-sans antialiased"
        suppressHydrationWarning
      >
        <ThemeProvider defaultTheme="system" storageKey="geneflow-theme">
          <QueryProvider>
            <TooltipProvider>
              {children}
            </TooltipProvider>
          </QueryProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}

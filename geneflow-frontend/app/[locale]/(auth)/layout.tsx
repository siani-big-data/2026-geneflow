"use client";

import Link from "next/link";
import Image from "next/image";
import { ArrowLeft, Moon, Sun } from "lucide-react";
import { useTheme } from "@/providers";
import { GuestGuard } from "@/components/auth";
import {useTranslations} from "next-intl";

export default function AuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const t = useTranslations("auth");
  const { theme, setTheme, resolvedTheme } = useTheme();

  const toggleTheme = () => {
    if (theme === "system") {
      setTheme(resolvedTheme === "dark" ? "light" : "dark");
    } else {
      setTheme(theme === "dark" ? "light" : "dark");
    }
  };

  return (
    <div className="flex h-screen bg-slate-200 dark:bg-slate-900 p-3 gap-3 overflow-hidden">
      {/* Side Panel - Image */}
      <div className="hidden lg:block w-1/2 h-full rounded-2xl overflow-hidden flex-shrink-0">
        <Image
          src="/img/hero-side.png"
          alt="GeneFlow Platform"
          width={1920}
          height={1080}
          className="w-full h-full object-cover"
          priority
        />
      </div>

      {/* Main Panel - Form */}
      <div className="flex-1 flex flex-col bg-slate-50 dark:bg-slate-800 rounded-2xl h-full overflow-y-auto">
        {/* Header */}
        <header className="flex justify-between items-center p-6">
          <Link
            href="/"
            className="inline-flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400 hover:text-teal transition-colors"
          >
            <ArrowLeft className="h-4 w-4" />
            {t("backToHome")}
          </Link>

          <button
            onClick={toggleTheme}
            className="flex items-center justify-center w-10 h-10 rounded-lg border border-slate-300 dark:border-slate-600 bg-white/50 dark:bg-white/10 text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-white/80 dark:hover:bg-white/20 transition-all"
            aria-label={resolvedTheme === "light" ? "Switch to dark mode" : "Switch to light mode"}
          >
            {resolvedTheme === "light" ? (
              <Moon className="h-5 w-5" />
            ) : (
              <Sun className="h-5 w-5" />
            )}
          </button>
        </header>

        {/* Content */}
        <div className="flex-1 flex items-center justify-center p-6">
          <div className="w-full max-w-[400px]">
            <GuestGuard>{children}</GuestGuard>
          </div>
        </div>

        {/* Footer */}
        <footer className="p-6 text-center text-sm text-slate-500 dark:text-slate-400">
          <div className="flex items-center justify-center gap-2 mb-2">
            <Image
              src="/logo.png"
              alt="GeneFlow"
              width={28}
              height={28}
              className="rounded object-contain"
            />
            <span className="font-semibold text-slate-700 dark:text-slate-200">GeneFlow</span>
          </div>
          <p>&copy; {new Date().getFullYear()} GeneFlow. All rights reserved.</p>
        </footer>
      </div>
    </div>
  );
}

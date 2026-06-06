import {
  LayoutDashboard,
  Globe,
  Beaker,
  Activity,
  Settings,
  HelpCircle,
  type LucideIcon,
} from "lucide-react";

export interface NavItem {
  /** Translation key for the navigation item name */
  nameKey: string;
  href: string;
  icon: LucideIcon;
}

export const mainNavigation: NavItem[] = [
  { nameKey: "dashboard", href: "/dashboard", icon: LayoutDashboard },
  { nameKey: "discover", href: "/discover", icon: Globe },
  { nameKey: "studies", href: "/studies", icon: Beaker },
  { nameKey: "pipelines", href: "/pipelines", icon: Activity },
];

export const bottomNavigation: NavItem[] = [
  { nameKey: "settings", href: "/settings", icon: Settings },
  { nameKey: "help", href: "/help", icon: HelpCircle },
];

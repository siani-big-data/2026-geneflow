import {
  LayoutDashboard,
  Globe,
  Beaker,
  Waves,
  Activity,
  BarChart3,
  Settings,
  HelpCircle,
  type LucideIcon,
} from "lucide-react";

export interface NavItem {
  name: string;
  href: string;
  icon: LucideIcon;
}

export const mainNavigation: NavItem[] = [
  { name: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
  { name: "Discover", href: "/discover", icon: Globe },
  { name: "Studies", href: "/studies", icon: Beaker },
  { name: "Traces", href: "/traces", icon: Waves },
  { name: "Pipelines", href: "/pipelines", icon: Activity },
  { name: "Analysis", href: "/analysis", icon: BarChart3 },
];

export const bottomNavigation: NavItem[] = [
  { name: "Settings", href: "/settings", icon: Settings },
  { name: "Help", href: "/help", icon: HelpCircle },
];

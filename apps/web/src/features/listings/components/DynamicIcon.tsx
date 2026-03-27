import { icons, type LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

const keyMap: Record<string, string> = {
  "log-in": "LogIn",
  "log-out": "LogOut",
  "dog": "Dog",
  "cigarette": "Cigarette",
  "party-popper": "PartyPopper",
  "users": "Users",
  "wifi": "Wifi",
  "tv": "Tv",
  "car": "Car",
  "snowflake": "Snowflake",
  "flame": "Flame",
  "waves": "Waves",
  "utensils": "Utensils",
  "coffee": "Coffee",
  "bath": "Bath",
  "bed": "Bed",
  "sofa": "Sofa",
  "dumbbell": "Dumbbell",
  "laptop": "Laptop",
  "washing-machine": "WashingMachine",
  "accessibility": "Accessibility",
  "trees": "Trees",
  "fence": "Fence",
  "shield-check": "ShieldCheck",
  "alarm-smoke": "AlarmSmoke",
  "siren": "Siren",
  "fire-extinguisher": "FireExtinguisher",
  "alert-triangle": "AlertTriangle",
  "construction": "Construction",
  "volume-2": "Volume2",
};

function resolveName(iconKey: string): string {
  if (keyMap[iconKey]) return keyMap[iconKey];

  return iconKey
    .split(/[-_]/)
    .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
    .join("");
}

const iconMap = icons as Record<string, LucideIcon | undefined>;

const DefaultFallback: LucideIcon =
  iconMap.CircleHelp ?? iconMap.HelpCircle ?? iconMap.Circle ?? icons.Circle;

type DynamicIconProps = {
  iconKey: string;
  className?: string;
  fallback?: LucideIcon;
};

export function DynamicIcon({ iconKey, className, fallback }: DynamicIconProps) {
  const resolved = resolveName(iconKey);
  const Icon = iconMap[resolved];

  if (Icon) {
    return <Icon className={cn("h-4 w-4", className)} />;
  }

  const FallbackIcon = fallback ?? DefaultFallback;
  return <FallbackIcon className={cn("h-4 w-4", className)} />;
}

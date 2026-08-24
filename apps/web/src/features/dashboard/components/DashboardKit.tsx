import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import type { LucideIcon } from "lucide-react";
import { ArrowRight } from "lucide-react";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
} from "@/components/ui/card";
import { cn } from "@/lib/utils";

// ── Stat card ────────────────────────────────────────────────

type StatTone = "default" | "primary" | "accent" | "success" | "warning";

const toneBg: Record<StatTone, string> = {
  default: "bg-secondary",
  primary: "bg-primary/10",
  accent: "bg-accent/10",
  success: "bg-success/10",
  warning: "bg-amber-500/10",
};

const toneFg: Record<StatTone, string> = {
  default: "text-muted-foreground",
  primary: "text-primary",
  accent: "text-accent",
  success: "text-success",
  warning: "text-amber-600",
};

export function StatCard({
  label,
  value,
  icon: Icon,
  to,
  hint,
  tone = "default",
}: {
  label: string;
  value: ReactNode;
  icon: LucideIcon;
  to?: string;
  hint?: string;
  tone?: StatTone;
}) {
  const inner = (
    <Card className={cn("h-full", to && "transition-shadow hover:shadow-md")}>
      <CardContent className="p-5">
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">{label}</p>
          <span
            className={cn(
              "flex h-9 w-9 items-center justify-center rounded-lg",
              toneBg[tone],
            )}
          >
            <Icon className={cn("h-4 w-4", toneFg[tone])} />
          </span>
        </div>
        <p className="mt-2 text-2xl font-bold tracking-tight">{value}</p>
        {hint && <p className="mt-0.5 text-xs text-muted-foreground">{hint}</p>}
      </CardContent>
    </Card>
  );

  return to ? (
    <Link to={to} className="block">
      {inner}
    </Link>
  ) : (
    inner
  );
}

// ── Section card (titled panel with optional action) ─────────

export function SectionCard({
  title,
  icon: Icon,
  description,
  action,
  children,
  className,
}: {
  title: string;
  icon?: LucideIcon;
  description?: string;
  action?: ReactNode;
  children: ReactNode;
  className?: string;
}) {
  return (
    <Card className={className}>
      <CardHeader className="flex flex-row items-start justify-between gap-3 space-y-0">
        <div className="min-w-0">
          <CardTitle className="text-base flex items-center gap-2">
            {Icon && <Icon className="h-4 w-4 text-muted-foreground" />}
            {title}
          </CardTitle>
          {description && <CardDescription>{description}</CardDescription>}
        </div>
        {action}
      </CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  );
}

// ── Quick-action tile ────────────────────────────────────────

export function QuickAction({
  label,
  description,
  to,
  icon: Icon,
}: {
  label: string;
  description: string;
  to: string;
  icon: LucideIcon;
}) {
  return (
    <Link to={to} className="group">
      <Card className="h-full transition-shadow hover:shadow-md">
        <CardContent className="flex items-center gap-3 p-4">
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/10">
            <Icon className="h-5 w-5 text-accent" />
          </span>
          <div className="min-w-0 flex-1">
            <p className="font-medium leading-tight">{label}</p>
            <p className="truncate text-xs text-muted-foreground">{description}</p>
          </div>
          <ArrowRight className="h-4 w-4 shrink-0 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
        </CardContent>
      </Card>
    </Link>
  );
}

// ── Inline empty hint ────────────────────────────────────────

export function EmptyHint({
  children,
  cta,
}: {
  children: ReactNode;
  cta?: { to: string; label: string };
}) {
  return (
    <div className="rounded-lg border border-dashed p-6 text-center">
      <p className="text-sm text-muted-foreground">{children}</p>
      {cta && (
        <Link
          to={cta.to}
          className="mt-2 inline-flex items-center gap-1 text-sm font-medium text-primary hover:underline"
        >
          {cta.label}
          <ArrowRight className="h-3.5 w-3.5" />
        </Link>
      )}
    </div>
  );
}

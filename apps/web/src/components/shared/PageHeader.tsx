import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

type PageHeaderProps = {
  /** Optional leading icon, rendered inside a tinted rounded square. */
  icon?: LucideIcon;
  title: string;
  description?: React.ReactNode;
  /** Right-aligned actions (buttons, links). */
  children?: React.ReactNode;
  className?: string;
};

/**
 * Shared page header used across the authenticated app surfaces so every
 * top-level page presents the same title / description / action rhythm.
 * The icon sits in a tinted rounded square to anchor the heading visually.
 */
export function PageHeader({
  icon: Icon,
  title,
  description,
  children,
  className,
}: PageHeaderProps) {
  return (
    <div
      className={cn(
        "flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between",
        className,
      )}
    >
      <div className="flex items-start gap-3 min-w-0">
        {Icon && (
          <span className="hidden sm:flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
            <Icon className="h-5 w-5" />
          </span>
        )}
        <div className="min-w-0">
          <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            <span className="truncate">{title}</span>
          </h1>
          {description && (
            <p className="mt-1 text-sm text-muted-foreground">{description}</p>
          )}
        </div>
      </div>
      {children && (
        <div className="flex shrink-0 items-center gap-2">{children}</div>
      )}
    </div>
  );
}

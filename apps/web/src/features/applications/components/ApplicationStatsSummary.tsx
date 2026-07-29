import { CheckCircle2, Clock, FileText, XCircle } from "lucide-react";
import { cn } from "@/lib/utils";

type StatKey = "All" | "Pending" | "Approved" | "Rejected";

type Props = {
  counts: Record<StatKey, number>;
  className?: string;
};

const statItems: {
  key: StatKey;
  label: string;
  icon: typeof Clock;
  accent?: boolean;
}[] = [
  { key: "All", label: "Total", icon: FileText },
  { key: "Pending", label: "Pending", icon: Clock, accent: true },
  { key: "Approved", label: "Approved", icon: CheckCircle2 },
  { key: "Rejected", label: "Declined", icon: XCircle },
];

export function ApplicationStatsSummary({ counts, className }: Props) {
  return (
    <div className={cn("grid grid-cols-2 gap-3 sm:grid-cols-4", className)}>
      {statItems.map(({ key, label, icon: Icon, accent }) => (
        <div
          key={key}
          className={cn(
            "rounded-xl border bg-card p-3 shadow-sm",
            accent && counts.Pending > 0 && "border-accent/40 bg-accent/5",
          )}
        >
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <Icon className="h-3.5 w-3.5 shrink-0" />
            {label}
          </div>
          <p className="mt-1 text-2xl font-semibold tabular-nums">{counts[key]}</p>
        </div>
      ))}
    </div>
  );
}

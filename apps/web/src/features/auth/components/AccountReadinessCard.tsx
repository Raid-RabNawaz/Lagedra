import { Link } from "react-router-dom";
import {
  ArrowRight,
  Check,
  CheckCircle2,
  Clock,
  ClipboardCheck,
} from "lucide-react";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button-variants";
import type { ProfileSignalStatus } from "@/features/auth/lib/profileCompleteness";
import type { ReadinessAudience } from "@/features/auth/lib/accountReadiness";
import { useAccountReadiness } from "@/features/auth/hooks/useAccountReadiness";
import { cn } from "@/lib/utils";

export function AccountReadinessCard({
  audience,
  className,
}: {
  audience: ReadinessAudience;
  className?: string;
}) {
  const { readiness, isLoading } = useAccountReadiness(audience);

  if (!readiness) return null;

  const next = readiness.steps.find((s) => s.status !== "complete" && s.id !== "eligible");

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle className="text-base flex items-center justify-between gap-3">
          <span className="flex items-center gap-2">
            <ClipboardCheck className="h-4 w-4 text-muted-foreground" />
            {readiness.title}
          </span>
          <Badge variant={readiness.isEligible ? "success" : "secondary"}>
            {isLoading ? "…" : `${readiness.percent}%`}
          </Badge>
        </CardTitle>
      </CardHeader>
      <CardContent>
        <p className="mb-4 text-sm text-muted-foreground">{readiness.description}</p>

        <div className="mb-4 h-2 w-full overflow-hidden rounded-full bg-secondary">
          <div
            className="h-full rounded-full bg-primary transition-all"
            style={{ width: `${readiness.percent}%` }}
          />
        </div>

        <div className="space-y-2.5">
          {readiness.steps.map((step) => (
            <Link
              key={step.id}
              to={step.href}
              className="flex items-start gap-3 rounded-lg p-1 -mx-1 hover:bg-muted/40"
            >
              <StatusCircle status={step.status} />
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span
                    className={cn(
                      "text-sm font-medium",
                      step.status === "empty" ? "text-muted-foreground" : "text-foreground",
                    )}
                  >
                    {step.label}
                  </span>
                  {step.status === "pending" && (
                    <span className="text-[11px] font-semibold uppercase tracking-wide text-warning">
                      Pending
                    </span>
                  )}
                  {step.status === "complete" && step.id === "eligible" && (
                    <CheckCircle2 className="h-3.5 w-3.5 text-success" />
                  )}
                </div>
                <p className="text-xs text-muted-foreground">{step.detail}</p>
              </div>
            </Link>
          ))}
        </div>

        {next && (
          <Link
            to={next.href}
            className={cn(buttonVariants({ variant: "outline" }), "mt-4 w-full")}
          >
            Continue: {next.label}
            <ArrowRight className="ml-auto h-4 w-4" />
          </Link>
        )}
      </CardContent>
    </Card>
  );
}

function StatusCircle({ status }: { status: ProfileSignalStatus }) {
  if (status === "complete") {
    return (
      <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-success text-success-foreground">
        <Check className="h-3 w-3" />
      </span>
    );
  }
  if (status === "pending") {
    return (
      <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full border-2 border-warning bg-warning/15 text-warning">
        <Clock className="h-3 w-3" />
      </span>
    );
  }
  return <span className="mt-0.5 h-5 w-5 shrink-0 rounded-full border-2 border-border" />;
}

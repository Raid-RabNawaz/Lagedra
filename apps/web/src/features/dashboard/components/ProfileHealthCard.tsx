import { Link } from "react-router-dom";
import {
  User,
  ArrowRight,
  Check,
  CheckCircle2,
  Clock,
  ShieldCheck,
} from "lucide-react";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { buttonVariants } from "@/components/ui/button-variants";
import type { UserProfileDto } from "@/api/types";
import {
  type ProfileSignalStatus,
  emailVerificationStatus,
  governmentIdVerificationStatus,
  phoneVerificationStatus,
  presenceStatus,
} from "@/features/auth/lib/profileCompleteness";
import { cn } from "@/lib/utils";

// Every row derives its state from the SAME helpers the verification panel
// uses, so the checklist can never say "empty" while the badge says "Pending".
const PROFILE_CHECKS: {
  label: string;
  status: (u: UserProfileDto) => ProfileSignalStatus;
}[] = [
  {
    label: "Personal information",
    status: (u) => (u.firstName && u.lastName ? "complete" : "empty"),
  },
  { label: "Phone number", status: phoneVerificationStatus },
  { label: "Location", status: (u) => presenceStatus(u.city) },
  { label: "About / Bio", status: (u) => presenceStatus(u.bio) },
  { label: "Government ID", status: governmentIdVerificationStatus },
];

// A pending signal is half-way there, so it earns half credit — the meter
// reflects real progress instead of jumping 0 → 100 only on verification.
const STATUS_WEIGHT: Record<ProfileSignalStatus, number> = {
  empty: 0,
  pending: 0.5,
  complete: 1,
};

/**
 * Shared "account health" panel — profile completeness + verification status.
 * Surfaced on every dashboard view so trust-building actions are always one
 * click away regardless of whether the user is traveling or hosting.
 */
export function ProfileHealthCard({ user }: { user: UserProfileDto }) {
  const score = PROFILE_CHECKS.reduce(
    (acc, check) => acc + STATUS_WEIGHT[check.status(user)],
    0,
  );
  const pct = Math.round((score / PROFILE_CHECKS.length) * 100);

  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center justify-between">
            <span className="flex items-center gap-2">
              <User className="h-4 w-4 text-muted-foreground" />
              Complete your profile
            </span>
            <Badge variant={pct === 100 ? "success" : "secondary"}>{pct}%</Badge>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="mb-4 h-2 w-full overflow-hidden rounded-full bg-secondary">
            <div
              className="h-full rounded-full bg-primary transition-all"
              style={{ width: `${pct}%` }}
            />
          </div>

          <div className="space-y-2.5">
            {PROFILE_CHECKS.map((check) => {
              const status = check.status(user);
              return (
                <div key={check.label} className="flex items-center gap-3">
                  <StatusCircle status={status} />
                  <span
                    className={cn(
                      "text-sm",
                      status === "empty" ? "text-muted-foreground" : "text-foreground",
                    )}
                  >
                    {check.label}
                  </span>
                  {status === "pending" && (
                    <span className="ml-auto text-[11px] font-semibold uppercase tracking-wide text-warning">
                      Pending
                    </span>
                  )}
                </div>
              );
            })}
          </div>

          <Separator className="my-4" />

          <Link
            to="/app/profile"
            className={cn(buttonVariants({ variant: "outline" }), "w-full")}
          >
            <User className="h-4 w-4" />
            Edit profile
            <ArrowRight className="ml-auto h-4 w-4" />
          </Link>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <ShieldCheck className="h-4 w-4 text-muted-foreground" />
            Verification status
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="mb-4 text-sm text-muted-foreground">
            Verified accounts gain higher trust scores, access to more listings,
            and priority support.
          </p>

          <div className="space-y-3">
            <VerificationItem label="Email" status={emailVerificationStatus(user)} />
            <VerificationItem label="Phone" status={phoneVerificationStatus(user)} />
            <VerificationItem
              label="Government ID"
              status={governmentIdVerificationStatus(user)}
            />
          </div>

          <Separator className="my-4" />

          <Link
            to="/app/verification"
            className={cn(buttonVariants({ variant: "outline" }), "w-full")}
          >
            <ShieldCheck className="h-4 w-4" />
            Manage verification
            <ArrowRight className="ml-auto h-4 w-4" />
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}

/** Checklist bullet: outline circle → amber clock (pending) → green check. */
function StatusCircle({ status }: { status: ProfileSignalStatus }) {
  if (status === "complete") {
    return (
      <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-success text-success-foreground">
        <Check className="h-3 w-3" />
      </span>
    );
  }
  if (status === "pending") {
    return (
      <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full border-2 border-warning bg-warning/15 text-warning">
        <Clock className="h-3 w-3" />
      </span>
    );
  }
  return <span className="h-5 w-5 shrink-0 rounded-full border-2 border-border" />;
}

function VerificationItem({
  label,
  status,
}: {
  label: string;
  status: ProfileSignalStatus;
}) {
  return (
    <div className="flex items-center justify-between rounded-lg border p-3">
      <span className="text-sm font-medium">{label}</span>
      {status === "complete" ? (
        <Badge variant="success" className="text-xs">
          <CheckCircle2 className="mr-1 h-3 w-3" />
          Verified
        </Badge>
      ) : status === "pending" ? (
        <Badge variant="warning" className="text-xs">
          <Clock className="mr-1 h-3 w-3" />
          Pending
        </Badge>
      ) : (
        <Badge variant="secondary" className="text-xs">
          Not started
        </Badge>
      )}
    </div>
  );
}

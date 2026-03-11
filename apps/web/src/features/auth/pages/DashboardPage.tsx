import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  Shield,
  Clock,
  TrendingUp,
  Calendar,
  User,
  ArrowRight,
  CheckCircle2,
  AlertCircle,
} from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { useAuthStore } from "@/app/auth/authStore";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { cn } from "@/lib/utils";
import { Loader } from "@/components/shared/Loader";

export const DashboardPage = () => {
  const user = useAuthStore((state) => state.user);
  const setUser = useAuthStore((state) => state.setUser);
  const [isLoading, setIsLoading] = useState(!user);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (user) return;

    let cancelled = false;
    authApi
      .getCurrentUser()
      .then((profile) => {
        if (!cancelled) setUser(profile);
      })
      .catch(() => {
        if (!cancelled) setError("Could not load your dashboard details.");
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [user, setUser]);

  if (isLoading) return <Loader label="Loading dashboard..." />;
  if (error) {
    return (
      <div className="flex items-center justify-center py-16">
        <div className="text-center">
          <AlertCircle className="mx-auto h-10 w-10 text-destructive mb-3" />
          <p className="text-destructive font-medium">{error}</p>
        </div>
      </div>
    );
  }

  const displayName = user?.displayName || user?.firstName || user?.email?.split("@")[0] || "there";
  const memberSince = user?.memberSince
    ? new Date(user.memberSince).toLocaleDateString("en-US", {
        month: "long",
        year: "numeric",
      })
    : "N/A";

  const profileComplete = Boolean(
    user?.firstName && user?.lastName && user?.phoneNumber && user?.city,
  );

  const stats = [
    {
      label: "Role",
      value: String(user?.role ?? "N/A"),
      icon: Shield,
      accent: false,
    },
    {
      label: "Member since",
      value: memberSince,
      icon: Calendar,
      accent: false,
    },
    {
      label: "Response rate",
      value: user?.responseRatePercent != null ? `${user.responseRatePercent}%` : "N/A",
      icon: TrendingUp,
      accent: false,
    },
    {
      label: "Response time",
      value: user?.responseTimeMinutes != null ? `${user.responseTimeMinutes} min` : "N/A",
      icon: Clock,
      accent: false,
    },
  ];

  return (
    <div className="space-y-8">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">
            Welcome back, {displayName}
          </h1>
          <p className="mt-1 text-muted-foreground">
            Here&apos;s an overview of your account.
          </p>
        </div>
        <Badge variant={profileComplete ? "success" : "secondary"} className="self-start sm:self-auto">
          {profileComplete ? (
            <>
              <CheckCircle2 className="mr-1 h-3 w-3" />
              Profile complete
            </>
          ) : (
            <>
              <AlertCircle className="mr-1 h-3 w-3" />
              Profile incomplete
            </>
          )}
        </Badge>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat) => (
          <Card key={stat.label}>
            <CardContent className="p-5">
              <div className="flex items-center justify-between">
                <p className="text-sm text-muted-foreground">{stat.label}</p>
                <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-secondary">
                  <stat.icon className="h-4 w-4 text-muted-foreground" />
                </div>
              </div>
              <p className="mt-2 text-xl font-semibold">{stat.value}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Complete your profile</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground mb-4">
              A complete profile helps build trust with other users and unlocks full platform features.
            </p>

            <div className="space-y-3">
              <ProfileCheckItem
                label="Personal information"
                done={Boolean(user?.firstName && user?.lastName)}
              />
              <ProfileCheckItem label="Phone number" done={Boolean(user?.phoneNumber)} />
              <ProfileCheckItem label="Location" done={Boolean(user?.city)} />
              <ProfileCheckItem label="About / Bio" done={Boolean(user?.bio)} />
              <ProfileCheckItem label="Government ID" done={Boolean(user?.isGovernmentIdVerified)} />
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
            <CardTitle className="text-lg">Verification status</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground mb-4">
              Verified accounts gain higher trust scores, access to more listings, and priority support.
            </p>

            <div className="space-y-3">
              <VerificationItem
                label="Email verified"
                status={user?.isActive ? "verified" : "pending"}
              />
              <VerificationItem
                label="Phone verified"
                status={user?.isPhoneVerified ? "verified" : "pending"}
              />
              <VerificationItem
                label="Government ID"
                status={user?.isGovernmentIdVerified ? "verified" : "pending"}
              />
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

function ProfileCheckItem({ label, done }: { label: string; done: boolean }) {
  return (
    <div className="flex items-center gap-3">
      <div
        className={`flex h-5 w-5 shrink-0 items-center justify-center rounded-full ${
          done ? "bg-success text-success-foreground" : "border-2 border-border"
        }`}
      >
        {done && <CheckCircle2 className="h-3 w-3" />}
      </div>
      <span className={`text-sm ${done ? "text-foreground" : "text-muted-foreground"}`}>
        {label}
      </span>
    </div>
  );
}

function VerificationItem({
  label,
  status,
}: {
  label: string;
  status: "verified" | "pending" | "not_started";
}) {
  return (
    <div className="flex items-center justify-between rounded-lg border p-3">
      <span className="text-sm font-medium">{label}</span>
      {status === "verified" ? (
        <Badge variant="success" className="text-xs">
          <CheckCircle2 className="mr-1 h-3 w-3" />
          Verified
        </Badge>
      ) : (
        <Badge variant="secondary" className="text-xs">Pending</Badge>
      )}
    </div>
  );
}

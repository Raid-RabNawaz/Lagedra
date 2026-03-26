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
  Users,
  Home,
  Building2,
  Briefcase,
  Scale,
  FileCheck,
  Plus,
} from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { useAuthStore } from "@/app/auth/authStore";
import { roles, roleLabel } from "@/app/auth/roles";
import { isAdmin } from "@/app/auth/permissions";
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

  const currentRole = String(user?.role ?? "");

  const stats = [
    {
      label: "Role",
      value: roleLabel(currentRole),
      icon: Shield,
    },
    {
      label: "Member since",
      value: memberSince,
      icon: Calendar,
    },
    {
      label: "Response rate",
      value: user?.responseRatePercent != null ? `${user.responseRatePercent}%` : "N/A",
      icon: TrendingUp,
    },
    {
      label: "Response time",
      value: user?.responseTimeMinutes != null ? `${user.responseTimeMinutes} min` : "N/A",
      icon: Clock,
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

      <RoleQuickActions role={currentRole} />

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

type QuickAction = { label: string; description: string; to: string; icon: typeof Home };

function RoleQuickActions({ role }: { role: string }) {
  let actions: QuickAction[] = [];

  if (role === roles.tenant) {
    actions = [
      { label: "Browse listings", description: "Find your next mid-term rental", to: "/listings", icon: Home },
      { label: "My applications", description: "Track your rental applications", to: "#", icon: FileCheck },
    ];
  } else if (role === roles.landlord) {
    actions = [
      { label: "Browse listings", description: "See what's on the market", to: "/listings", icon: Home },
      { label: "My listings", description: "Manage your rental properties", to: "/app/listings", icon: Building2 },
      { label: "Create listing", description: "Add a new property", to: "/app/listings/new", icon: Plus },
      { label: "Applications", description: "Review tenant applications", to: "#", icon: FileCheck },
    ];
  } else if (role === roles.arbitrator) {
    actions = [
      { label: "Assigned cases", description: "Review and resolve disputes", to: "#", icon: Scale },
    ];
  } else if (isAdmin(role)) {
    actions = [
      { label: "Manage users", description: "View and manage all platform users", to: "/app/admin/users", icon: Users },
      { label: "Browse listings", description: "View marketplace listings", to: "/listings", icon: Home },
    ];
  } else if (role === roles.insurancePartner) {
    actions = [
      { label: "Active policies", description: "View insurance policies", to: "#", icon: Shield },
    ];
  } else if (role === roles.institutionPartner) {
    actions = [
      { label: "Browse listings", description: "Find rentals for your members", to: "/listings", icon: Home },
      { label: "Organization", description: "Manage your organization", to: "#", icon: Briefcase },
    ];
  }

  if (actions.length === 0) return null;

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {actions.map((action) => (
        <Link key={action.label} to={action.to}>
          <Card className="transition-shadow hover:shadow-md h-full">
            <CardContent className="flex items-start gap-4 p-5">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/10">
                <action.icon className="h-5 w-5 text-accent" />
              </div>
              <div>
                <p className="font-medium">{action.label}</p>
                <p className="text-sm text-muted-foreground">{action.description}</p>
              </div>
            </CardContent>
          </Card>
        </Link>
      ))}
    </div>
  );
}

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

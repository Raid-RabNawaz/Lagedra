import { useEffect, useState } from "react";
import { AlertCircle } from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { useAuthStore } from "@/app/auth/authStore";
import { useModeStore } from "@/app/auth/modeStore";
import { supportsModeSwitching } from "@/app/auth/permissions";
import { Loader } from "@/components/shared/Loader";
import { TravelingDashboard } from "@/features/dashboard/components/TravelingDashboard";
import { HostingDashboard } from "@/features/dashboard/components/HostingDashboard";
import { RoleDashboard } from "@/features/dashboard/components/RoleDashboard";

export const DashboardPage = () => {
  const user = useAuthStore((state) => state.user);
  const setUser = useAuthStore((state) => state.setUser);
  const [isLoading, setIsLoading] = useState(!user);
  const [error, setError] = useState<string | null>(null);

  const mode = useModeStore((s) => s.mode);

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
  if (error || !user) {
    return (
      <div className="flex items-center justify-center py-16">
        <div className="text-center">
          <AlertCircle className="mx-auto mb-3 h-10 w-10 text-destructive" />
          <p className="font-medium text-destructive">
            {error ?? "Could not load your dashboard."}
          </p>
        </div>
      </div>
    );
  }

  const displayName =
    user.displayName || user.firstName || user.email?.split("@")[0] || "there";
  const canSwitch = supportsModeSwitching(user.role);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">
          Welcome back, {displayName}
        </h1>
        <p className="mt-1 text-muted-foreground">
          {canSwitch
            ? mode === "host"
              ? "Here's what's happening with your properties."
              : "Here's everything for your trips and requests."
            : "Here's an overview of your account."}
        </p>
      </div>

      {canSwitch ? (
        mode === "host" ? (
          <HostingDashboard user={user} />
        ) : (
          <TravelingDashboard user={user} />
        )
      ) : (
        <RoleDashboard user={user} />
      )}
    </div>
  );
};

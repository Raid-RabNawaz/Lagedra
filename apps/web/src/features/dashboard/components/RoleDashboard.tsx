import {
  Shield,
  Calendar,
  TrendingUp,
  Clock,
  Home,
  Scale,
  BookOpen,
  Users,
  Building2,
  Briefcase,
  Mail,
  ClipboardCheck,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { roles, roleLabel } from "@/app/auth/roles";
import { isAdmin } from "@/app/auth/permissions";
import type { UserProfileDto } from "@/api/types";
import { ProtocolFeeReconciliationBanner } from "@/features/admin/components/ProtocolFeeReconciliationBanner";
import { ProfileHealthCard } from "./ProfileHealthCard";
import { StatCard, QuickAction } from "./DashboardKit";

const monthYear = new Intl.DateTimeFormat("en-US", {
  month: "long",
  year: "numeric",
});

/**
 * Dashboard for roles that don't switch between traveling and hosting
 * (arbitrators, partner orgs, insurance partners, and the admin fallback).
 * Surfaces account stats, role-tailored quick actions and the shared account
 * health panel.
 */
export function RoleDashboard({ user }: { user: UserProfileDto }) {
  const role = String(user.role);
  const memberSince = user.memberSince
    ? monthYear.format(new Date(user.memberSince))
    : "N/A";

  return (
    <div className="space-y-6">
      {isAdmin(role) && (
        <ProtocolFeeReconciliationBanner hideWhenHealthy showSettingsLink />
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Role" value={roleLabel(role)} icon={Shield} tone="primary" />
        <StatCard label="Member since" value={memberSince} icon={Calendar} />
        <StatCard
          label="Response rate"
          value={user.responseRatePercent != null ? `${user.responseRatePercent}%` : "—"}
          icon={TrendingUp}
        />
        <StatCard
          label="Response time"
          value={user.responseTimeMinutes != null ? `${user.responseTimeMinutes} min` : "—"}
          icon={Clock}
        />
      </div>

      <RoleQuickActions role={role} />

      <ProfileHealthCard user={user} />
    </div>
  );
}

type Action = { label: string; description: string; to: string; icon: LucideIcon };

function RoleQuickActions({ role }: { role: string }) {
  let actions: Action[] = [];

  if (role === roles.arbitrator) {
    actions = [
      { label: "My cases", description: "Review and resolve disputes", to: "/app/arbitration", icon: Scale },
      { label: "Lease agreements", description: "Manage lease templates", to: "/app/lease-agreements", icon: BookOpen },
    ];
  } else if (isAdmin(role)) {
    actions = [
      { label: "Manage users", description: "View and manage platform users", to: "/app/admin/users", icon: Users },
      { label: "Listing review", description: "Approve pending listings", to: "/app/admin/listing-review", icon: ClipboardCheck },
      { label: "Verify partners", description: "Approve & suspend partner orgs", to: "/app/admin/partners", icon: Building2 },
      { label: "Browse listings", description: "View the marketplace", to: "/listings", icon: Home },
    ];
  } else if (role === roles.insurancePartner) {
    actions = [
      { label: "Insurance queue", description: "Review insurance items", to: "/app/admin/insurance-queue", icon: Shield },
      { label: "Browse listings", description: "View the marketplace", to: "/listings", icon: Home },
    ];
  } else if (role === roles.institutionPartner) {
    actions = [
      { label: "Partner portal", description: "Manage your organization", to: "/app/partner", icon: Briefcase },
      { label: "Invite a guest", description: "Onboard members & book for them", to: "/app/partner/guests", icon: Users },
      { label: "Reservations", description: "Bookings for your members", to: "/app/partner/reservations", icon: Mail },
      { label: "Browse listings", description: "Find rentals for members", to: "/listings", icon: Home },
    ];
  }

  if (actions.length === 0) return null;

  return (
    <div>
      <h2 className="mb-3 text-sm font-semibold uppercase tracking-wider text-muted-foreground">
        Quick actions
      </h2>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {actions.map((a) => (
          <QuickAction key={a.label} {...a} />
        ))}
      </div>
    </div>
  );
}

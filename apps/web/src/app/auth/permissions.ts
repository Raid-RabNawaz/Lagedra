import type { UserRole } from "./roles";
import { roles } from "./roles";

export type NavItem = {
  to: string;
  label: string;
  icon: string;
  end?: boolean;
};

export type NavGroup = {
  label: string;
  items: NavItem[];
};

// ── Sidebar groups (AppShell dashboard) ─────────────────────

const mainGroup: NavGroup = {
  label: "Main",
  items: [
    { to: "/app", label: "Dashboard", icon: "LayoutDashboard", end: true },
  ],
};

const memberBookingsGroup: NavGroup = {
  label: "Bookings",
  items: [
    { to: "/app/deals", label: "My reservations", icon: "CalendarCheck" },
    { to: "/app/my-applications", label: "My applications", icon: "FileText" },
    { to: "/app/arbitration", label: "My cases", icon: "Scale" },
  ],
};

const memberListingsGroup: NavGroup = {
  label: "Listings",
  items: [
    { to: "/app/listings", label: "My listings", icon: "Building2" },
    { to: "/app/listings/new", label: "Create listing", icon: "Plus" },
  ],
};

const memberApplicationsGroup: NavGroup = {
  label: "Applications",
  items: [
    { to: "/app/applications", label: "Inbox", icon: "Inbox" },
  ],
};

const arbitratorCasesGroup: NavGroup = {
  label: "Arbitration",
  items: [
    { to: "/app/arbitration", label: "My cases", icon: "Scale" },
  ],
};

const adminOpsGroup: NavGroup = {
  label: "Operations",
  items: [
    { to: "/app/admin/insurance-queue", label: "Insurance Queue", icon: "ShieldAlert" },
    { to: "/app/admin/fraud-flags", label: "Fraud Flags", icon: "Flag" },
    { to: "/app/admin/arbitration-backlog", label: "Arbitration Backlog", icon: "Scale" },
    { to: "/app/admin/evidence-review", label: "Evidence Review", icon: "FileSearch" },
    { to: "/app/admin/manual-verification", label: "Manual Verification", icon: "UserCheck" },
    { to: "/app/admin/compliance-violations", label: "Violations", icon: "AlertTriangle" },
    { to: "/app/admin/restrictions", label: "Restrictions", icon: "Ban" },
  ],
};

const adminConfigGroup: NavGroup = {
  label: "Configuration",
  items: [
    { to: "/app/admin/users", label: "Users", icon: "Users" },
    { to: "/app/admin/partners", label: "Partner Orgs", icon: "Building2" },
    { to: "/app/admin/definitions", label: "Definitions", icon: "Settings" },
    { to: "/app/admin/jurisdiction-packs", label: "Jurisdiction Packs", icon: "BookOpen" },
    { to: "/app/admin/dual-control", label: "Dual Control", icon: "ShieldCheck" },
  ],
};

const partnerGroup: NavGroup = {
  label: "Partner portal",
  items: [
    { to: "/app/partner", label: "Dashboard", icon: "Building2", end: true },
    { to: "/app/partner/members", label: "Members", icon: "Users" },
    { to: "/app/partner/referrals", label: "Referral links", icon: "Link2" },
    { to: "/app/partner/reservations", label: "Reservations", icon: "CalendarCheck" },
    { to: "/app/partner/guests", label: "Invite guests", icon: "Mail" },
    { to: "/app/partner/endorsements", label: "Endorsements", icon: "ShieldCheck" },
  ],
};

const adminContentGroup: NavGroup = {
  label: "Content",
  items: [
    { to: "/app/admin/blog", label: "Blog Posts", icon: "FileText" },
    { to: "/app/admin/seo", label: "SEO Pages", icon: "Globe" },
  ],
};

const adminInsightsGroup: NavGroup = {
  label: "Insights",
  items: [
    { to: "/app/admin/analytics", label: "Dashboard", icon: "BarChart3" },
    { to: "/app/admin/listing-analytics", label: "Listing Analytics", icon: "TrendingUp" },
    { to: "/app/admin/audit", label: "Audit Log", icon: "ScrollText" },
  ],
};

const accountGroup: NavGroup = {
  label: "Account",
  items: [
    { to: "/app/profile", label: "Profile", icon: "User" },
    { to: "/app/saved", label: "Saved listings", icon: "Heart" },
    { to: "/app/trust-ledger", label: "Trust Ledger", icon: "BookOpen" },
  ],
};

const memberAccountGroup: NavGroup = {
  label: "Account",
  items: [
    { to: "/app/profile", label: "Profile", icon: "User" },
    { to: "/app/payout-setup", label: "Payout setup", icon: "Wallet" },
    { to: "/app/saved", label: "Saved listings", icon: "Heart" },
    { to: "/app/trust-ledger", label: "Trust Ledger", icon: "BookOpen" },
  ],
};

const memberSidebarGroups: NavGroup[] = [
  mainGroup,
  memberBookingsGroup,
  memberListingsGroup,
  memberApplicationsGroup,
  memberAccountGroup,
];

const roleSidebarGroups: Record<UserRole, NavGroup[]> = {
  [roles.member]: memberSidebarGroups,
  [roles.arbitrator]: [mainGroup, arbitratorCasesGroup, accountGroup],
  [roles.platformAdmin]: [...memberSidebarGroups, adminOpsGroup, adminConfigGroup, adminContentGroup, adminInsightsGroup],
  [roles.insurancePartner]: [mainGroup, accountGroup],
  [roles.institutionPartner]: [mainGroup, partnerGroup, accountGroup],
};

export function getSidebarGroupsForRole(role: UserRole | string | number): NavGroup[] {
  return roleSidebarGroups[role as UserRole] ?? [mainGroup, accountGroup];
}

// ── Bottom tabs (MarketplaceLayout mobile) ──────────────────

const sharedBottomTabs: NavItem[] = [
  { to: "/listings", label: "Explore", icon: "Search", end: true },
  { to: "/app/saved", label: "Saved", icon: "Heart" },
];

const memberBottomTabs: NavItem[] = [
  ...sharedBottomTabs,
  { to: "/app/deals", label: "Reservations", icon: "CalendarCheck" },
  { to: "/app/applications", label: "Inbox", icon: "Inbox" },
  { to: "/app", label: "Dashboard", icon: "LayoutDashboard", end: true },
  { to: "/app/profile", label: "Profile", icon: "User" },
];

const roleBottomTabs: Record<UserRole, NavItem[]> = {
  [roles.member]: memberBottomTabs,
  [roles.arbitrator]: [
    ...sharedBottomTabs,
    { to: "/app/profile", label: "Profile", icon: "User" },
  ],
  [roles.platformAdmin]: memberBottomTabs,
  [roles.insurancePartner]: [
    ...sharedBottomTabs,
    { to: "/app/profile", label: "Profile", icon: "User" },
  ],
  [roles.institutionPartner]: [
    ...sharedBottomTabs,
    { to: "/app/profile", label: "Profile", icon: "User" },
  ],
};

const guestBottomTabs: NavItem[] = [
  { to: "/listings", label: "Explore", icon: "Search", end: true },
  { to: "/auth/login", label: "Sign in", icon: "LogIn" },
  { to: "/auth/register", label: "Sign up", icon: "UserPlus" },
];

export function getBottomTabsForRole(role: UserRole | string | number | null): NavItem[] {
  if (!role) return guestBottomTabs;
  return roleBottomTabs[role as UserRole] ?? guestBottomTabs;
}

// ── Flat nav helper (kept for compatibility) ────────────────

export function getNavItemsForRole(role: UserRole | string | number): NavItem[] {
  const groups = getSidebarGroupsForRole(role);
  return groups.flatMap((g) => g.items);
}

// ── Role checks ─────────────────────────────────────────────

const adminRoles: Set<string> = new Set([roles.platformAdmin]);

export function isAdmin(role: UserRole | string | number): boolean {
  return adminRoles.has(String(role));
}

export function hasRole(
  userRole: UserRole | string | number | undefined | null,
  allowedRoles: readonly (UserRole | string)[],
): boolean {
  if (!userRole) return false;
  const r = String(userRole);
  if (r === roles.platformAdmin) return true;
  return allowedRoles.includes(r as UserRole);
}

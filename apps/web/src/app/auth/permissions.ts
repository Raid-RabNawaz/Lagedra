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

const tenantBookingsGroup: NavGroup = {
  label: "Bookings",
  items: [
    { to: "/app/my-applications", label: "My applications", icon: "FileText" },
  ],
};

const landlordListingsGroup: NavGroup = {
  label: "Listings",
  items: [
    { to: "/app/listings", label: "My listings", icon: "Building2" },
    { to: "/app/listings/new", label: "Create listing", icon: "Plus" },
  ],
};

const landlordApplicationsGroup: NavGroup = {
  label: "Applications",
  items: [
    { to: "/app/applications", label: "Inbox", icon: "Inbox" },
  ],
};

const adminGroup: NavGroup = {
  label: "Admin",
  items: [
    { to: "/app/admin/users", label: "Users", icon: "Users" },
    { to: "/app/admin/definitions", label: "Definitions", icon: "Settings" },
  ],
};

const accountGroup: NavGroup = {
  label: "Account",
  items: [
    { to: "/app/profile", label: "Profile", icon: "User" },
    { to: "/app/saved", label: "Saved listings", icon: "Heart" },
  ],
};

const roleSidebarGroups: Record<UserRole, NavGroup[]> = {
  [roles.tenant]: [mainGroup, tenantBookingsGroup, accountGroup],
  [roles.landlord]: [mainGroup, landlordListingsGroup, landlordApplicationsGroup, accountGroup],
  [roles.arbitrator]: [mainGroup, accountGroup],
  [roles.platformAdmin]: [mainGroup, landlordListingsGroup, landlordApplicationsGroup, adminGroup, accountGroup],
  [roles.insurancePartner]: [mainGroup, accountGroup],
  [roles.institutionPartner]: [mainGroup, accountGroup],
};

export function getSidebarGroupsForRole(role: UserRole | string | number): NavGroup[] {
  return roleSidebarGroups[role as UserRole] ?? [mainGroup, accountGroup];
}

// ── Bottom tabs (MarketplaceLayout mobile) ──────────────────

const sharedBottomTabs: NavItem[] = [
  { to: "/listings", label: "Explore", icon: "Search", end: true },
  { to: "/app/saved", label: "Saved", icon: "Heart" },
];

const roleBottomTabs: Record<UserRole, NavItem[]> = {
  [roles.tenant]: [
    ...sharedBottomTabs,
    { to: "/app/my-applications", label: "Applications", icon: "FileText" },
    { to: "/app/profile", label: "Profile", icon: "User" },
  ],
  [roles.landlord]: [
    ...sharedBottomTabs,
    { to: "/app/applications", label: "Inbox", icon: "Inbox" },
    { to: "/app", label: "Dashboard", icon: "LayoutDashboard", end: true },
    { to: "/app/profile", label: "Profile", icon: "User" },
  ],
  [roles.arbitrator]: [
    ...sharedBottomTabs,
    { to: "/app/profile", label: "Profile", icon: "User" },
  ],
  [roles.platformAdmin]: [
    ...sharedBottomTabs,
    { to: "/app/applications", label: "Inbox", icon: "Inbox" },
    { to: "/app", label: "Dashboard", icon: "LayoutDashboard", end: true },
    { to: "/app/profile", label: "Profile", icon: "User" },
  ],
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

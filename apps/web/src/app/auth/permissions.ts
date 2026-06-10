import type { UserRole } from "./roles";
import type { AppMode } from "./modeStore";
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
//
// Convention: every nav item's `icon` MUST also exist in the iconMap
// inside AppShell.tsx — unmapped names silently fall back to a
// dashboard square, which is invisible at code-review time. When you
// add a new entry, add the icon to the iconMap in the same change.

const memberMainGroup: NavGroup = {
  label: "Main",
  items: [
    { to: "/app", label: "Dashboard", icon: "LayoutDashboard", end: true },
    { to: "/listings", label: "Browse listings", icon: "Search", end: true },
  ],
};

const compactMainGroup: NavGroup = {
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
    { to: "/app/my-inquiries", label: "My conversations", icon: "MessageCircle" },
    { to: "/app/arbitration", label: "My cases", icon: "Scale" },
  ],
};

const memberHostingGroup: NavGroup = {
  label: "Hosting",
  items: [
    { to: "/app/listings", label: "My listings", icon: "Building2", end: true },
    { to: "/app/listings/new", label: "Create listing", icon: "Plus" },
    { to: "/app/inquiries", label: "Guest inquiries", icon: "MessageSquare" },
    { to: "/app/applications", label: "Booking requests", icon: "Inbox" },
    { to: "/app/deals", label: "Bookings", icon: "CalendarCheck" },
    { to: "/app/arbitration", label: "My cases", icon: "Scale" },
  ],
};

const arbitratorCasesGroup: NavGroup = {
  label: "Arbitration",
  items: [
    { to: "/app/arbitration", label: "My cases", icon: "Scale" },
    { to: "/app/jurisdiction-packs", label: "Jurisdiction Packs", icon: "BookOpen" },
  ],
};

// ── Admin sidebar groups ────────────────────────────────────
//
// Admins inherit every member-side group (so they can dogfood the
// product from both guest and host perspectives). Below sit the
// admin-only sections — split into focused queues rather than one
// 8-item "Operations" dump, and prefixed with "Admin · " so they
// stand out visually from member groups in the sidebar without
// requiring a structural change to <SidebarNav>.

const adminTrustSafetyGroup: NavGroup = {
  label: "Admin · Trust & safety",
  items: [
    { to: "/app/admin/listing-review", label: "Listing review", icon: "ClipboardCheck" },
    { to: "/app/admin/manual-verification", label: "ID verification", icon: "UserCheck" },
    { to: "/app/admin/insurance-queue", label: "Insurance queue", icon: "ShieldAlert" },
    { to: "/app/admin/fraud-flags", label: "Fraud flags", icon: "Flag" },
    { to: "/app/admin/compliance-violations", label: "Violations", icon: "AlertTriangle" },
    { to: "/app/admin/restrictions", label: "User restrictions", icon: "Ban" },
  ],
};

const adminArbitrationGroup: NavGroup = {
  label: "Admin · Arbitration",
  items: [
    { to: "/app/admin/arbitration-backlog", label: "Case backlog", icon: "Scale" },
    { to: "/app/admin/evidence-review", label: "Evidence review", icon: "FileSearch" },
  ],
};

const adminPeopleGroup: NavGroup = {
  label: "Admin · People",
  items: [
    { to: "/app/admin/users", label: "Users", icon: "Users" },
    { to: "/app/admin/partners", label: "Partner orgs", icon: "Building2" },
  ],
};

const adminConfigGroup: NavGroup = {
  label: "Admin · Configuration",
  items: [
    { to: "/app/admin/definitions", label: "Definitions", icon: "Settings" },
    { to: "/app/admin/jurisdiction-packs", label: "Jurisdiction packs", icon: "BookOpen" },
    { to: "/app/admin/dual-control", label: "Dual control", icon: "ShieldCheck" },
    { to: "/app/admin/settings", label: "Fees & settings", icon: "SlidersHorizontal" },
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
  label: "Admin · Content",
  items: [
    { to: "/app/admin/blog", label: "Blog posts", icon: "FileText" },
    { to: "/app/admin/seo", label: "SEO pages", icon: "Globe" },
  ],
};

const adminInsightsGroup: NavGroup = {
  label: "Admin · Insights",
  items: [
    { to: "/app/admin/analytics", label: "Platform analytics", icon: "BarChart3" },
    { to: "/app/admin/listing-analytics", label: "Listing analytics", icon: "TrendingUp" },
    { to: "/app/admin/audit", label: "Audit log", icon: "ScrollText" },
  ],
};

// Account groups are role-tailored: members get host-side controls
// (payout setup) and tenant-side affordances (saved listings, trust
// ledger), while operational roles only see the universally-applicable
// surfaces.

const memberAccountGroup: NavGroup = {
  label: "Account",
  items: [
    { to: "/app/profile", label: "Profile", icon: "User" },
    { to: "/app/notifications", label: "Notifications", icon: "Bell" },
    { to: "/app/notification-preferences", label: "Notification settings", icon: "SlidersHorizontal" },
    { to: "/app/payout-setup", label: "Payout setup", icon: "Wallet" },
    { to: "/app/saved", label: "Saved listings", icon: "Heart" },
    { to: "/app/trust-ledger", label: "Trust Ledger", icon: "BookOpen" },
  ],
};

const memberGuestAccountGroup: NavGroup = {
  label: "Account",
  items: [
    { to: "/app/profile", label: "Profile", icon: "User" },
    { to: "/app/notifications", label: "Notifications", icon: "Bell" },
    { to: "/app/notification-preferences", label: "Notification settings", icon: "SlidersHorizontal" },
    { to: "/app/saved", label: "Saved listings", icon: "Heart" },
    { to: "/app/trust-ledger", label: "Trust Ledger", icon: "BookOpen" },
  ],
};

const memberHostAccountGroup: NavGroup = {
  label: "Account",
  items: [
    { to: "/app/profile", label: "Profile", icon: "User" },
    { to: "/app/notifications", label: "Notifications", icon: "Bell" },
    { to: "/app/notification-preferences", label: "Notification settings", icon: "SlidersHorizontal" },
    { to: "/app/payout-setup", label: "Payout setup", icon: "Wallet" },
    { to: "/app/trust-ledger", label: "Trust Ledger", icon: "BookOpen" },
  ],
};

const compactAccountGroup: NavGroup = {
  label: "Account",
  items: [
    { to: "/app/profile", label: "Profile", icon: "User" },
    { to: "/app/notifications", label: "Notifications", icon: "Bell" },
    { to: "/app/notification-preferences", label: "Notification settings", icon: "SlidersHorizontal" },
    { to: "/app/trust-ledger", label: "Trust Ledger", icon: "BookOpen" },
  ],
};

const memberGuestSidebarGroups: NavGroup[] = [
  memberMainGroup,
  memberBookingsGroup,
  memberGuestAccountGroup,
];

const memberHostSidebarGroups: NavGroup[] = [
  memberMainGroup,
  memberHostingGroup,
  memberHostAccountGroup,
];

const memberAllSidebarGroups: NavGroup[] = [
  memberMainGroup,
  memberBookingsGroup,
  memberHostingGroup,
  memberAccountGroup,
];

// Single source of truth for the admin sidebar block — keeps the
// "view as guest"/"view as host" mode toggles and the no-mode default
// in sync. Order goes: highest-frequency queues first (trust & safety
// triage → arbitration), then directory-style screens, then config,
// content, and finally analytics.
const adminSidebarGroups: NavGroup[] = [
  adminTrustSafetyGroup,
  adminArbitrationGroup,
  adminPeopleGroup,
  adminConfigGroup,
  adminContentGroup,
  adminInsightsGroup,
];

const roleSidebarGroups: Record<UserRole, NavGroup[]> = {
  [roles.member]: memberAllSidebarGroups,
  [roles.arbitrator]: [compactMainGroup, arbitratorCasesGroup, compactAccountGroup],
  [roles.platformAdmin]: [...memberAllSidebarGroups, ...adminSidebarGroups],
  [roles.insurancePartner]: [compactMainGroup, compactAccountGroup],
  [roles.institutionPartner]: [compactMainGroup, partnerGroup, compactAccountGroup],
};

const memberModeGroups: Record<AppMode, NavGroup[]> = {
  guest: memberGuestSidebarGroups,
  host: memberHostSidebarGroups,
};

export function getSidebarGroupsForRole(
  role: UserRole | string | number,
  mode?: AppMode,
): NavGroup[] {
  if (mode && String(role) === roles.member) {
    return memberModeGroups[mode];
  }
  if (mode && String(role) === roles.platformAdmin) {
    return [...memberModeGroups[mode], ...adminSidebarGroups];
  }
  return roleSidebarGroups[role as UserRole] ?? [compactMainGroup, compactAccountGroup];
}

// ── Bottom tabs (MarketplaceLayout mobile) ──────────────────

const sharedBottomTabs: NavItem[] = [
  { to: "/listings", label: "Explore", icon: "Search", end: true },
  { to: "/app/saved", label: "Saved", icon: "Heart" },
];

const memberGuestBottomTabs: NavItem[] = [
  ...sharedBottomTabs,
  { to: "/app/deals", label: "Reservations", icon: "CalendarCheck" },
  { to: "/app", label: "Dashboard", icon: "LayoutDashboard", end: true },
  { to: "/app/profile", label: "Profile", icon: "User" },
];

const memberHostBottomTabs: NavItem[] = [
  ...sharedBottomTabs,
  { to: "/app/applications", label: "Requests", icon: "Inbox" },
  { to: "/app/deals", label: "Bookings", icon: "CalendarCheck" },
  { to: "/app", label: "Dashboard", icon: "LayoutDashboard", end: true },
  { to: "/app/profile", label: "Profile", icon: "User" },
];

const memberBottomTabs: NavItem[] = [
  ...sharedBottomTabs,
  { to: "/app/deals", label: "Reservations", icon: "CalendarCheck" },
  { to: "/app/applications", label: "Requests", icon: "Inbox" },
  { to: "/app", label: "Dashboard", icon: "LayoutDashboard", end: true },
  { to: "/app/profile", label: "Profile", icon: "User" },
];

const compactAuthedBottomTabs: NavItem[] = [
  { to: "/listings", label: "Explore", icon: "Search", end: true },
  { to: "/app", label: "Dashboard", icon: "LayoutDashboard", end: true },
  { to: "/app/notifications", label: "Notifications", icon: "Bell" },
  { to: "/app/profile", label: "Profile", icon: "User" },
];

const arbitratorBottomTabs: NavItem[] = [
  { to: "/app/arbitration", label: "Cases", icon: "Scale" },
  { to: "/app", label: "Dashboard", icon: "LayoutDashboard", end: true },
  { to: "/app/notifications", label: "Notifications", icon: "Bell" },
  { to: "/app/profile", label: "Profile", icon: "User" },
];

const roleBottomTabs: Record<UserRole, NavItem[]> = {
  [roles.member]: memberBottomTabs,
  [roles.arbitrator]: arbitratorBottomTabs,
  [roles.platformAdmin]: memberBottomTabs,
  [roles.insurancePartner]: compactAuthedBottomTabs,
  [roles.institutionPartner]: [
    { to: "/listings", label: "Explore", icon: "Search", end: true },
    { to: "/app/partner", label: "Portal", icon: "Building2", end: true },
    { to: "/app/notifications", label: "Notifications", icon: "Bell" },
    { to: "/app/profile", label: "Profile", icon: "User" },
  ],
};

const guestBottomTabs: NavItem[] = [
  { to: "/listings", label: "Explore", icon: "Search", end: true },
  { to: "/auth/login", label: "Sign in", icon: "LogIn" },
  { to: "/auth/register", label: "Sign up", icon: "UserPlus" },
];

const memberModeBottomTabs: Record<AppMode, NavItem[]> = {
  guest: memberGuestBottomTabs,
  host: memberHostBottomTabs,
};

export function getBottomTabsForRole(
  role: UserRole | string | number | null,
  mode?: AppMode,
): NavItem[] {
  if (!role) return guestBottomTabs;
  if (mode && (String(role) === roles.member || String(role) === roles.platformAdmin)) {
    return memberModeBottomTabs[mode];
  }
  return roleBottomTabs[role as UserRole] ?? guestBottomTabs;
}

// ── Flat nav helper (kept for compatibility) ────────────────

export function getNavItemsForRole(
  role: UserRole | string | number,
  mode?: AppMode,
): NavItem[] {
  const groups = getSidebarGroupsForRole(role, mode);
  return groups.flatMap((g) => g.items);
}

// ── Mode switch eligibility ─────────────────────────────────

const modeSwitchableRoles: Set<string> = new Set([roles.member, roles.platformAdmin]);

export function supportsModeSwitching(role: UserRole | string | number | null | undefined): boolean {
  if (!role) return false;
  return modeSwitchableRoles.has(String(role));
}

// ── Role checks ─────────────────────────────────────────────

const adminRoles: Set<string> = new Set([roles.platformAdmin]);

const arbitrationRoles: readonly UserRole[] = [
  roles.member,
  roles.arbitrator,
  roles.platformAdmin,
];

export function isAdmin(role: UserRole | string | number): boolean {
  return adminRoles.has(String(role));
}

export function canAccessArbitration(role: UserRole | string | number | undefined | null): boolean {
  if (!role) return false;
  if (String(role) === roles.platformAdmin) return true;
  return arbitrationRoles.includes(String(role) as UserRole);
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

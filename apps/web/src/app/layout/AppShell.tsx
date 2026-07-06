import { createElement } from "react";
import { Link, NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import {
  LogOut,
  Menu,
  X,
  LayoutDashboard,
  User,
  Users,
  Search,
  Building2,
  Plus,
  Heart,
  Inbox,
  FileText,
  Pin,
  PinOff,
  UserPlus,
  LogIn,
  Settings,
  CalendarCheck,
  BookOpen,
  Scale,
  Link2,
  Mail,
  ShieldCheck,
  ShieldAlert,
  ClipboardCheck,
  Bell,
  Wallet,
  Flag,
  FileSearch,
  UserCheck,
  AlertTriangle,
  Ban,
  Globe,
  BarChart3,
  TrendingUp,
  ScrollText,
  SlidersHorizontal,
  MessageSquare,
  MessageCircle,
} from "lucide-react";
import logoSvg from "@/assets/logo.svg";
import { useState, useCallback, useEffect } from "react";
import { authApi } from "@/features/auth/services/authApi";
import { useAuthStore } from "@/app/auth/authStore";
import { getSidebarGroupsForRole, supportsModeSwitching, type NavItem } from "@/app/auth/permissions";
import { resolveModeSwitchRedirect } from "@/app/auth/modeNavigation";
import { roleLabel } from "@/app/auth/roles";
import { useModeStore } from "@/app/auth/modeStore";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { AuthedHeaderActions } from "@/app/layout/AuthedHeaderActions";
import { cn } from "@/lib/utils";

const SIDEBAR_PINNED_KEY = "lagedra.sidebar.pinned";

const iconMap: Record<string, typeof LayoutDashboard> = {
  LayoutDashboard,
  User,
  Users,
  Search,
  Building2,
  Plus,
  Heart,
  Inbox,
  FileText,
  UserPlus,
  LogIn,
  Settings,
  CalendarCheck,
  BookOpen,
  Scale,
  Link2,
  Mail,
  ShieldCheck,
  ShieldAlert,
  ClipboardCheck,
  Bell,
  Wallet,
  Flag,
  FileSearch,
  UserCheck,
  AlertTriangle,
  Ban,
  Globe,
  BarChart3,
  TrendingUp,
  ScrollText,
  SlidersHorizontal,
  MessageSquare,
  MessageCircle,
};

function resolveIcon(name: string) {
  return iconMap[name] ?? LayoutDashboard;
}

export const AppShell = () => {
  const user = useAuthStore((s) => s.user);
  const setUser = useAuthStore((s) => s.setUser);

  // Pinned = keep the sidebar fully expanded. Unpinned = show an icon-only
  // rail that hover-expands (as an overlay) and re-collapses on mouse-leave.
  // Defaults to unpinned so the space-saving rail is the out-of-the-box
  // behavior; the choice is remembered per browser.
  const [pinned, setPinned] = useState(() => {
    if (typeof window === "undefined") return false;
    return window.localStorage.getItem(SIDEBAR_PINNED_KEY) === "true";
  });
  const [hovered, setHovered] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [loggingOut, setLoggingOut] = useState(false);

  // Effective expanded state drives what the sidebar renders: full labels when
  // pinned or while the cursor is peeking, icon-only otherwise.
  const expanded = pinned || hovered;
  const collapsed = !expanded;

  const mode = useModeStore((s) => s.mode);
  const showModeSwitch = supportsModeSwitching(user?.role);
  const location = useLocation();
  const navigate = useNavigate();

  // Keep the active page aligned with Travelling vs Hosting. Without this,
  // toggling mode updates the sidebar but leaves you on e.g. `/app/applications`
  // while the UI says "Travelling".
  useEffect(() => {
    if (!showModeSwitch) return;
    const redirect = resolveModeSwitchRedirect(location.pathname, mode);
    if (redirect) {
      navigate(redirect, { replace: true });
    }
  }, [mode, location.pathname, navigate, showModeSwitch]);

  const groups = getSidebarGroupsForRole(user?.role ?? "", showModeSwitch ? mode : undefined);

  const togglePinned = useCallback(() => {
    setPinned((v) => {
      const next = !v;
      window.localStorage.setItem(SIDEBAR_PINNED_KEY, String(next));
      // Reset the transient hover state so unpinning immediately collapses
      // the rail (instead of waiting for the next mouse-leave).
      if (!next) {
        setHovered(false);
      }
      return next;
    });
  }, []);

  // Mobile slide-out keeps its own inline Log out button (sidebar
  // pattern), so we still need a logout handler here in addition to
  // the one bundled inside <AuthedHeaderActions>.
  const onLogoutMobile = async () => {
    setLoggingOut(true);
    try {
      await authApi.logout();
    } finally {
      setUser(null);
      window.location.assign("/auth/login");
    }
  };

  const displayName =
    user?.displayName || user?.firstName || user?.email?.split("@")[0] || "User";
  const initials = displayName
    .split(" ")
    .filter((s) => s.length > 0)
    .slice(0, 2)
    .map((s) => s[0]?.toUpperCase())
    .join("");

  const sidebarContent = (onNav?: () => void) => {
    // First group whose label starts with "Admin ·" marks the
    // boundary between member-facing sections and admin-only
    // sections. We render a horizontal divider above it so the
    // mode switch between "your account" and "platform tools"
    // is visually unambiguous, instead of having one long
    // undifferentiated wall of group headers.
    const firstAdminIndex = groups.findIndex((g) => g.label.startsWith("Admin"));

    return (
      <nav className="flex flex-col gap-6 p-3">
        {groups.map((group, idx) => {
          const isFirstAdmin = firstAdminIndex >= 0 && idx === firstAdminIndex;
          // Strip the "Admin · " prefix from the rendered label —
          // the divider + separate visual block already conveys
          // the grouping, and shorter labels read better in the
          // narrow sidebar.
          const displayLabel = group.label.startsWith("Admin · ")
            ? group.label.slice("Admin · ".length)
            : group.label;

          return (
            <div key={group.label}>
              {isFirstAdmin && !collapsed && (
                <div className="-mt-2 mb-3 flex items-center gap-2 px-3">
                  <span className="text-[10px] font-bold uppercase tracking-[0.15em] text-primary">
                    Platform admin
                  </span>
                  <div className="h-px flex-1 bg-border" />
                </div>
              )}
              {isFirstAdmin && collapsed && (
                <div className="mb-2 mx-2 h-px bg-border" />
              )}
              {!collapsed && (
                <p className="mb-1 px-3 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                  {displayLabel}
                </p>
              )}
              <div className="flex flex-col gap-0.5">
                {group.items.map((item) => (
                  <SidebarLink
                    key={item.to}
                    item={item}
                    collapsed={collapsed}
                    onClick={onNav}
                  />
                ))}
              </div>
            </div>
          );
        })}
      </nav>
    );
  };

  return (
    <div className="flex min-h-screen flex-col bg-secondary/30">
      {/* ── Top bar ────────────────────────────── */}
      <header className="sticky top-0 z-50 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="flex h-14 items-center justify-between px-4">
          <div className="flex items-center gap-3">
            {/* Mobile hamburger */}
            <Button
              variant="ghost"
              size="icon"
              className="lg:hidden"
              onClick={() => setMobileOpen((v) => !v)}
              aria-label="Toggle sidebar"
            >
              {mobileOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
            </Button>

            <Link to="/app" className="flex items-center gap-2">
              <img src={logoSvg} alt="Lagedra" className="h-6" />
            </Link>
          </div>

          <div className="flex items-center gap-2">
            <Link
              to="/listings"
              className="hidden sm:flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-medium text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors"
            >
              <Search className="h-4 w-4" />
              Browse listings
            </Link>

            {/* Mode switch + bell + user dropdown live here. The
                Dashboard link is omitted from the dropdown inside the
                AppShell because the sidebar's Main group already pins
                it — adding it again would be a duplicate affordance. */}
            <AuthedHeaderActions />
          </div>
        </div>
      </header>

      <div className="flex flex-1 min-h-0">
        {/* ── Desktop sidebar (collapsible rail) ──────────────────── */}
        {/* The spacer keeps layout width reserved at the *rail* size when
            unpinned, so hover-expansion overlays the content instead of
            reflowing the whole page on every peek. */}
        <div
          className={cn(
            "relative hidden lg:block shrink-0 transition-[width] duration-200 ease-in-out",
            pinned ? "w-60" : "w-[68px]",
          )}
        >
          <aside
            onMouseEnter={() => setHovered(true)}
            onMouseLeave={() => setHovered(false)}
            className={cn(
              "fixed left-0 top-14 bottom-0 z-40 flex flex-col border-r bg-background transition-[width] duration-200 ease-in-out",
              expanded ? "w-60" : "w-[68px]",
              !pinned && hovered && "shadow-xl",
            )}
          >
            <div className="flex-1 overflow-y-auto py-2">
              {sidebarContent()}
            </div>

            <div className="border-t p-2">
              <button
                onClick={togglePinned}
                className={cn(
                  "flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors cursor-pointer",
                  collapsed && "justify-center px-2",
                )}
                title={
                  pinned
                    ? "Unpin sidebar (auto-collapse to icons)"
                    : "Pin sidebar (keep expanded)"
                }
              >
                {pinned ? (
                  <>
                    <PinOff className="h-4 w-4 shrink-0" />
                    {expanded && <span>Unpin</span>}
                  </>
                ) : (
                  <>
                    <Pin className="h-4 w-4 shrink-0" />
                    {expanded && <span>Pin sidebar</span>}
                  </>
                )}
              </button>
            </div>
          </aside>
        </div>

        {/* ── Mobile slide-out ─────────────────── */}
        {mobileOpen && (
          <>
            <div
              className="fixed inset-0 z-40 bg-black/30 lg:hidden"
              onClick={() => setMobileOpen(false)}
            />
            <aside className="fixed left-0 top-14 bottom-0 z-50 w-64 overflow-y-auto border-r bg-background lg:hidden animate-fade-in">
              <div className="flex items-center gap-3 border-b px-3 py-3">
                <Avatar className="h-9 w-9">
                  {user?.profilePhotoUrl ? (
                    <AvatarImage src={user.profilePhotoUrl} alt={displayName} />
                  ) : null}
                  <AvatarFallback className="text-xs">{initials}</AvatarFallback>
                </Avatar>
                <div>
                  <p className="text-sm font-medium">{displayName}</p>
                  <p className="text-xs text-muted-foreground">
                    {roleLabel(String(user?.role ?? ""))}
                  </p>
                </div>
              </div>

              {sidebarContent(() => setMobileOpen(false))}

              <div className="border-t p-3">
                <button
                  onClick={() => {
                    setMobileOpen(false);
                    void onLogoutMobile();
                  }}
                  disabled={loggingOut}
                  className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors cursor-pointer disabled:opacity-50"
                >
                  <LogOut className="h-4 w-4" />
                  Log out
                </button>
              </div>
            </aside>
          </>
        )}

        {/* ── Main content ─────────────────────── */}
        <main className="flex-1 overflow-y-auto">
          <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
};

function SidebarLink({
  item,
  collapsed,
  onClick,
}: {
  item: NavItem;
  collapsed: boolean;
  onClick?: () => void;
}) {
  const icon = resolveIcon(item.icon);

  return (
    <NavLink
      to={item.to}
      end={item.end}
      onClick={onClick}
      title={collapsed ? item.label : undefined}
      className={({ isActive }) =>
        cn(
          "flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
          collapsed && "justify-center px-2",
          isActive
            ? "bg-secondary text-foreground"
            : "text-muted-foreground hover:bg-secondary hover:text-foreground",
        )
      }
    >
      {createElement(icon, { className: "h-4 w-4 shrink-0" })}
      {!collapsed && <span className="truncate">{item.label}</span>}
    </NavLink>
  );
}


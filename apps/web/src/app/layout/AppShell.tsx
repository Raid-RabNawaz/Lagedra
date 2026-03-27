import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import {
  Shield,
  LogOut,
  Menu,
  X,
  LayoutDashboard,
  User,
  Users,
  ChevronDown,
  Key,
  Search,
  Building2,
  Plus,
  Heart,
  Inbox,
  FileText,
  PanelLeftClose,
  PanelLeft,
  UserPlus,
  LogIn,
  Settings,
} from "lucide-react";
import { useRef, useState, useEffect, useCallback } from "react";
import { authApi } from "@/features/auth/services/authApi";
import { useAuthStore } from "@/app/auth/authStore";
import { getSidebarGroupsForRole, type NavItem } from "@/app/auth/permissions";
import { roleLabel } from "@/app/auth/roles";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

const SIDEBAR_KEY = "lagedra.sidebar";

const iconMap: Record<string, typeof LayoutDashboard> = {
  LayoutDashboard,
  User,
  Users,
  Key,
  Search,
  Building2,
  Plus,
  Heart,
  Inbox,
  FileText,
  UserPlus,
  LogIn,
  Settings,
};

function resolveIcon(name: string) {
  return iconMap[name] ?? LayoutDashboard;
}

export const AppShell = () => {
  const user = useAuthStore((s) => s.user);
  const setUser = useAuthStore((s) => s.setUser);
  const navigate = useNavigate();

  const [collapsed, setCollapsed] = useState(() => {
    if (typeof window === "undefined") return false;
    return window.localStorage.getItem(SIDEBAR_KEY) === "true";
  });
  const [mobileOpen, setMobileOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [loggingOut, setLoggingOut] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);

  const groups = getSidebarGroupsForRole(user?.role ?? "");

  const toggleCollapsed = useCallback(() => {
    setCollapsed((v) => {
      const next = !v;
      window.localStorage.setItem(SIDEBAR_KEY, String(next));
      return next;
    });
  }, []);

  useEffect(() => {
    if (!userMenuOpen) return;
    const handler = (e: MouseEvent) => {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) {
        setUserMenuOpen(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [userMenuOpen]);

  const onLogout = async () => {
    setLoggingOut(true);
    try {
      await authApi.logout();
    } finally {
      setUser(null);
      navigate("/auth/login", { replace: true });
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

  const sidebarContent = (onNav?: () => void) => (
    <nav className="flex flex-col gap-6 p-3">
      {groups.map((group) => (
        <div key={group.label}>
          {!collapsed && (
            <p className="mb-1 px-3 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              {group.label}
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
      ))}
    </nav>
  );

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
              <Shield className="h-6 w-6 text-accent" />
              <span className="text-lg font-bold tracking-tight">Lagedra</span>
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

            {/* User dropdown */}
            <div className="relative" ref={userMenuRef}>
              <button
                onClick={() => setUserMenuOpen((v) => !v)}
                className="flex items-center gap-2 rounded-full py-1 pl-1 pr-3 transition-colors hover:bg-secondary cursor-pointer"
              >
                <Avatar className="h-8 w-8">
                  {user?.profilePhotoUrl ? (
                    <AvatarImage src={user.profilePhotoUrl} alt={displayName} />
                  ) : null}
                  <AvatarFallback className="text-xs">{initials}</AvatarFallback>
                </Avatar>
                <div className="hidden sm:block text-left">
                  <p className="text-sm font-medium leading-none">{displayName}</p>
                  <p className="text-[10px] text-muted-foreground leading-tight">
                    {roleLabel(String(user?.role ?? ""))}
                  </p>
                </div>
                <ChevronDown
                  className={cn(
                    "h-3.5 w-3.5 text-muted-foreground transition-transform hidden sm:block",
                    userMenuOpen && "rotate-180",
                  )}
                />
              </button>

              {userMenuOpen && (
                <div className="absolute right-0 top-full mt-2 w-56 rounded-xl border bg-background p-1.5 shadow-lg animate-fade-in">
                  <div className="px-3 py-2 border-b mb-1">
                    <p className="text-sm font-medium">{user?.email}</p>
                    <Badge variant="secondary" className="mt-1 text-[10px]">
                      {roleLabel(String(user?.role ?? ""))}
                    </Badge>
                  </div>

                  <Link
                    to="/app/profile"
                    onClick={() => setUserMenuOpen(false)}
                    className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors"
                  >
                    <User className="h-4 w-4" />
                    Profile & settings
                  </Link>

                  <button
                    onClick={() => {
                      setUserMenuOpen(false);
                      void onLogout();
                    }}
                    disabled={loggingOut}
                    className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors cursor-pointer disabled:opacity-50"
                  >
                    <LogOut className="h-4 w-4" />
                    {loggingOut ? "Logging out..." : "Log out"}
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      </header>

      <div className="flex flex-1 min-h-0">
        {/* ── Desktop sidebar ──────────────────── */}
        <aside
          className={cn(
            "hidden lg:flex flex-col border-r bg-background transition-[width] duration-200 ease-in-out shrink-0",
            collapsed ? "w-[68px]" : "w-60",
          )}
        >
          <div className="flex-1 overflow-y-auto py-2">
            {sidebarContent()}
          </div>

          <div className="border-t p-2">
            <button
              onClick={toggleCollapsed}
              className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors cursor-pointer"
              title={collapsed ? "Expand sidebar" : "Collapse sidebar"}
            >
              {collapsed ? (
                <PanelLeft className="h-4 w-4 mx-auto" />
              ) : (
                <>
                  <PanelLeftClose className="h-4 w-4" />
                  <span>Collapse</span>
                </>
              )}
            </button>
          </div>
        </aside>

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
                    void onLogout();
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
  const Icon = resolveIcon(item.icon);

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
      <Icon className="h-4 w-4 shrink-0" />
      {!collapsed && <span className="truncate">{item.label}</span>}
    </NavLink>
  );
}

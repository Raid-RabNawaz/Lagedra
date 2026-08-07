import { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  ArrowLeftRight,
  ChevronDown,
  LayoutDashboard,
  LogOut,
  User,
} from "lucide-react";
import { useAuthStore } from "@/app/auth/authStore";
import { useModeStore } from "@/app/auth/modeStore";
import { supportsModeSwitching } from "@/app/auth/permissions";
import {
  isPreLaunchLimitedHost,
  PRE_LAUNCH_HOST_HOME,
} from "@/app/auth/preLaunchAccess";
import { roleLabel } from "@/app/auth/roles";
import { usePublicConfigStore } from "@/app/config/publicConfigStore";
import { authApi } from "@/features/auth/services/authApi";
import { NotificationBell } from "@/features/notifications/components/NotificationBell";
import { useNotificationHub } from "@/features/notifications/hooks/useNotificationHub";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

type Props = {
  /**
   * When true, the user dropdown surfaces a "Dashboard" entry that links
   * to `/app`. Helpful from the public marketplace where the sidebar
   * isn't visible; redundant inside the AppShell where the same link
   * lives in the sidebar's Main group.
   */
  showDashboardInMenu?: boolean;
  /**
   * Hide the inline name/role block next to the avatar. The dropdown
   * still shows the same info — this just trims the trigger down to an
   * avatar + chevron, which fits better in dense header layouts.
   */
  compact?: boolean;
};

/**
 * Shared header actions for any layout that wants the "logged-in
 * chrome" — Member-only Travelling/Hosting switch, notification bell,
 * and the user dropdown (email, role, Dashboard, Profile, Log out).
 *
 * Centralising it here keeps the AppShell top bar and the marketplace
 * top bar visually identical for authenticated users — previously the
 * marketplace only rendered a flat "avatar → Dashboard" link, which
 * broke continuity when navigating between `/listings` and `/app`.
 */
export function AuthedHeaderActions({
  showDashboardInMenu = false,
  compact = false,
}: Props) {
  const user = useAuthStore((s) => s.user);
  const setUser = useAuthStore((s) => s.setUser);
  const navigate = useNavigate();

  const mode = useModeStore((s) => s.mode);
  const toggleMode = useModeStore((s) => s.toggleMode);
  const preLaunchEnabled = usePublicConfigStore((s) => s.preLaunchEnabled);
  const preLaunchLimited = isPreLaunchLimitedHost(preLaunchEnabled, user?.role);
  const showModeSwitch = !preLaunchLimited && supportsModeSwitching(user?.role);

  // Keep the SignalR hub primed wherever this component mounts so the
  // notification badge stays live across `/app` and the marketplace.
  // The hook is ref-counted internally — a second subscriber on the
  // same page just shares the singleton connection.
  useNotificationHub();

  const [open, setOpen] = useState(false);
  const [loggingOut, setLoggingOut] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [open]);

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
  const initials =
    displayName
      .split(" ")
      .filter((s) => s.length > 0)
      .slice(0, 2)
      .map((s) => s[0]?.toUpperCase())
      .join("") || "U";

  return (
    <div className="flex items-center gap-2">
      {showModeSwitch && <ModeSwitch mode={mode} onToggle={toggleMode} />}

      {!preLaunchLimited && <NotificationBell />}

      <div className="relative" ref={menuRef}>
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          className="flex items-center gap-2 rounded-full py-1 pl-1 pr-3 transition-colors hover:bg-secondary cursor-pointer"
          aria-haspopup="menu"
          aria-expanded={open}
        >
          <Avatar className="h-8 w-8">
            {user?.profilePhotoUrl ? (
              <AvatarImage src={user.profilePhotoUrl} alt={displayName} />
            ) : null}
            <AvatarFallback className="text-xs">{initials}</AvatarFallback>
          </Avatar>
          {!compact && (
            <div className="hidden sm:block text-left">
              <p className="text-sm font-medium leading-none">{displayName}</p>
              <p className="text-[10px] text-muted-foreground leading-tight">
                {roleLabel(String(user?.role ?? ""))}
              </p>
            </div>
          )}
          <ChevronDown
            className={cn(
              "h-3.5 w-3.5 text-muted-foreground transition-transform hidden sm:block",
              open && "rotate-180",
            )}
          />
        </button>

        {open && (
          <div
            role="menu"
            className="absolute right-0 top-full mt-2 w-56 rounded-xl border bg-background p-1.5 shadow-lg animate-fade-in z-50"
          >
            <div className="px-3 py-2 border-b mb-1">
              <p className="text-sm font-medium truncate">{user?.email}</p>
              <Badge variant="secondary" className="mt-1 text-[10px]">
                {roleLabel(String(user?.role ?? ""))}
              </Badge>
            </div>

            {(showDashboardInMenu || preLaunchLimited) && (
              <Link
                to={preLaunchLimited ? PRE_LAUNCH_HOST_HOME : "/app"}
                onClick={() => setOpen(false)}
                className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors"
              >
                <LayoutDashboard className="h-4 w-4" />
                {preLaunchLimited ? "My listings" : "Dashboard"}
              </Link>
            )}

            <Link
              to="/app/profile"
              onClick={() => setOpen(false)}
              className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors"
            >
              <User className="h-4 w-4" />
              Profile & settings
            </Link>

            <button
              type="button"
              onClick={() => {
                setOpen(false);
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
  );
}

function ModeSwitch({
  mode,
  onToggle,
}: {
  mode: "guest" | "host";
  onToggle: () => void;
}) {
  const isHost = mode === "host";

  return (
    <button
      type="button"
      onClick={onToggle}
      className="group flex items-center gap-2 rounded-full border px-3 py-1.5 text-sm font-medium transition-colors hover:bg-secondary cursor-pointer"
      title={isHost ? "Switch to guest mode" : "Switch to hosting"}
    >
      <span
        className={cn(
          "relative flex h-7 w-12 shrink-0 items-center rounded-full transition-colors",
          isHost ? "bg-primary" : "bg-muted",
        )}
      >
        <span
          className={cn(
            "absolute h-5 w-5 rounded-full bg-white shadow-sm transition-transform",
            isHost ? "translate-x-6" : "translate-x-1",
          )}
        />
      </span>
      <span className="hidden sm:inline whitespace-nowrap">
        {isHost ? "Hosting" : "Travelling"}
      </span>
      <ArrowLeftRight className="h-3.5 w-3.5 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity hidden sm:block" />
    </button>
  );
}

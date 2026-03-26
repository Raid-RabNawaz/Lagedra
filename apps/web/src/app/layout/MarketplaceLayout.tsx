import { Link, NavLink, Outlet } from "react-router-dom";
import {
  Shield,
  LogIn,
  UserPlus,
  Search,
  Heart,
  FileText,
  Inbox,
  User,
  LayoutDashboard,
} from "lucide-react";
import { useAuthStore } from "@/app/auth/authStore";
import { getBottomTabsForRole, type NavItem } from "@/app/auth/permissions";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const iconMap: Record<string, typeof Search> = {
  Search,
  Heart,
  FileText,
  Inbox,
  User,
  LayoutDashboard,
  LogIn,
  UserPlus,
};

function resolveIcon(name: string) {
  return iconMap[name] ?? Search;
}

export const MarketplaceLayout = () => {
  const user = useAuthStore((s) => s.user);
  const accessToken = useAuthStore((s) => s.accessToken);
  const isLoggedIn = Boolean(accessToken);

  const bottomTabs = getBottomTabsForRole(isLoggedIn ? (user?.role ?? null) : null);

  const displayName =
    user?.displayName || user?.firstName || user?.email?.split("@")[0] || "";
  const initials = displayName
    .split(" ")
    .filter((s) => s.length > 0)
    .slice(0, 2)
    .map((s) => s[0]?.toUpperCase())
    .join("");

  return (
    <div className="flex min-h-screen flex-col">
      {/* ── Desktop top bar ──────────────────── */}
      <header className="sticky top-0 z-50 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
          <Link to="/listings" className="flex items-center gap-2">
            <Shield className="h-7 w-7 text-accent" />
            <span className="text-xl font-bold tracking-tight">Lagedra</span>
          </Link>

          <div className="hidden sm:flex items-center gap-1">
            {isLoggedIn ? (
              <>
                <NavLink
                  to="/listings"
                  end
                  className={({ isActive }) =>
                    cn(
                      "flex items-center gap-1.5 rounded-full px-4 py-2 text-sm font-medium transition-colors",
                      isActive
                        ? "bg-secondary text-foreground"
                        : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                    )
                  }
                >
                  <Search className="h-4 w-4" />
                  Explore
                </NavLink>
                <NavLink
                  to="/app/saved"
                  className={({ isActive }) =>
                    cn(
                      "flex items-center gap-1.5 rounded-full px-4 py-2 text-sm font-medium transition-colors",
                      isActive
                        ? "bg-secondary text-foreground"
                        : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                    )
                  }
                >
                  <Heart className="h-4 w-4" />
                  Saved
                </NavLink>
              </>
            ) : (
              <NavLink
                to="/listings"
                end
                className={({ isActive }) =>
                  cn(
                    "flex items-center gap-1.5 rounded-full px-4 py-2 text-sm font-medium transition-colors",
                    isActive
                      ? "bg-secondary text-foreground"
                      : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                  )
                }
              >
                <Search className="h-4 w-4" />
                Browse
              </NavLink>
            )}
          </div>

          <div className="flex items-center gap-2">
            {isLoggedIn ? (
              <Link
                to="/app"
                className="flex items-center gap-2 rounded-full py-1 pl-1 pr-3 transition-colors hover:bg-secondary"
              >
                <Avatar className="h-8 w-8">
                  {user?.profilePhotoUrl ? (
                    <AvatarImage src={user.profilePhotoUrl} alt={displayName} />
                  ) : null}
                  <AvatarFallback className="text-xs">{initials || "U"}</AvatarFallback>
                </Avatar>
                <span className="text-sm font-medium hidden sm:inline">Dashboard</span>
              </Link>
            ) : (
              <>
                <Link
                  to="/auth/login"
                  className={cn(
                    buttonVariants({ variant: "ghost", size: "sm" }),
                    "gap-1.5 hidden sm:flex",
                  )}
                >
                  <LogIn className="h-4 w-4" />
                  Sign in
                </Link>
                <Link
                  to="/auth/register"
                  className={cn(buttonVariants({ variant: "accent", size: "sm" }), "gap-1.5")}
                >
                  <UserPlus className="h-4 w-4" />
                  Sign up
                </Link>
              </>
            )}
          </div>
        </div>
      </header>

      {/* ── Page content ─────────────────────── */}
      <main className="flex-1 flex flex-col min-h-0 pb-16 sm:pb-0">
        <Outlet />
      </main>

      {/* ── Desktop footer ───────────────────── */}
      <footer className="hidden sm:block border-t bg-background">
        <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
          <div className="flex flex-col items-center gap-4 sm:flex-row sm:justify-between">
            <div className="flex items-center gap-2">
              <Shield className="h-5 w-5 text-accent" />
              <span className="font-semibold">Lagedra</span>
            </div>
            <p className="text-xs text-muted-foreground">
              &copy; {new Date().getFullYear()} Lagedra &middot; Mid-term rental trust
              protocol
            </p>
          </div>
        </div>
      </footer>

      {/* ── Mobile bottom tab bar ────────────── */}
      <nav className="fixed bottom-0 left-0 right-0 z-50 border-t bg-background/95 backdrop-blur sm:hidden">
        <div className="flex items-stretch justify-around">
          {bottomTabs.map((tab) => (
            <BottomTab key={tab.to} item={tab} />
          ))}
        </div>
      </nav>
    </div>
  );
};

function BottomTab({ item }: { item: NavItem }) {
  const Icon = resolveIcon(item.icon);

  return (
    <NavLink
      to={item.to}
      end={item.end}
      className={({ isActive }) =>
        cn(
          "flex flex-1 flex-col items-center gap-0.5 py-2 text-[10px] font-medium transition-colors",
          isActive ? "text-accent" : "text-muted-foreground",
        )
      }
    >
      <Icon className="h-5 w-5" />
      <span>{item.label}</span>
    </NavLink>
  );
}

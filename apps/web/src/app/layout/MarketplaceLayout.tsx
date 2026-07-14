import { createElement } from "react";
import { Link, NavLink, Outlet } from "react-router-dom";
import {
  LogIn,
  UserPlus,
  Search,
  Heart,
  FileText,
  Inbox,
  User,
  LayoutDashboard,
  CalendarCheck,
  Bell,
  Facebook,
  Instagram,
  Linkedin,
} from "lucide-react";
import logoSvg from "@/assets/logo.svg";
import { useAuthStore } from "@/app/auth/authStore";
import { getBottomTabsForRole, supportsModeSwitching, type NavItem } from "@/app/auth/permissions";
import { useModeStore } from "@/app/auth/modeStore";
import { AuthedHeaderActions } from "@/app/layout/AuthedHeaderActions";
import { buttonVariants } from "@/components/ui/button-variants";
import { cn } from "@/lib/utils";

const iconMap: Record<string, typeof Search> = {
  Search,
  Heart,
  FileText,
  Inbox,
  User,
  LayoutDashboard,
  CalendarCheck,
  Bell,
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
  const mode = useModeStore((s) => s.mode);

  const role = isLoggedIn ? (user?.role ?? null) : null;
  const bottomTabs = getBottomTabsForRole(
    role,
    supportsModeSwitching(role) ? mode : undefined,
  );

  return (
    <div className="flex min-h-screen flex-col bg-background">
      {/* ── Top bar ──────────────────── */}
      <header className="sticky top-0 z-50 border-b border-border/60 bg-background">
        <div className="mx-auto flex h-[68px] max-w-7xl items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
          <Link to="/listings" className="flex items-center gap-2 shrink-0">
            <img src={logoSvg} alt="Lagedra" className="h-7" />
          </Link>

          <nav className="hidden md:flex items-center gap-1">
            <HeaderLink to="/listings" end label="Home" />
            <HeaderLink to="/listings/search" label="Explore" />
            {isLoggedIn ? <HeaderLink to="/app/saved" label="Saved" /> : null}
            <HeaderLink to="/listings/search?propertyType=Apartment" label="Apartments" />
            <HeaderLink to="/listings/search?propertyType=House" label="Houses" />
          </nav>

          {/*
           * Authed shoppers get the same mode-switch + bell + user
           * dropdown as the AppShell so the chrome stays identical when
           * navigating between the marketplace and the dashboard. The
           * dropdown here also surfaces a "Dashboard" entry because
           * there's no sidebar to fall back on outside `/app`.
           */}
          <div className="flex items-center gap-2">
            {isLoggedIn && user ? (
              <AuthedHeaderActions showDashboardInMenu />
            ) : (
              <>
                <Link
                  to="/auth/login"
                  className={cn(
                    buttonVariants({ variant: "ghost", size: "sm" }),
                    "gap-1.5 hidden sm:inline-flex font-semibold",
                  )}
                >
                  <LogIn className="h-4 w-4" />
                  Sign in
                </Link>
                <Link
                  to="/join"
                  className={cn(
                    buttonVariants({ variant: "default", size: "sm" }),
                    "gap-1.5 rounded-full px-5 font-semibold",
                  )}
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

      {/* ── Footer ───────────────────── */}
      <footer className="hidden sm:block border-t border-border/60 bg-surface">
        <div className="mx-auto max-w-7xl px-4 py-14 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 gap-10 md:grid-cols-12">
            <div className="md:col-span-4">
              <Link to="/listings" className="flex items-center gap-2">
                <img src={logoSvg} alt="Lagedra" className="h-7" />
              </Link>
              <p className="mt-4 max-w-xs text-sm text-muted-foreground">
                Move-in ready, mid-term rentals — backed by the Lagedra trust protocol.
              </p>
              <div className="mt-6 flex items-center gap-3">
                <SocialLink href="#" label="Facebook">
                  <Facebook className="h-4 w-4" />
                </SocialLink>
                <SocialLink href="#" label="Instagram">
                  <Instagram className="h-4 w-4" />
                </SocialLink>
                <SocialLink href="#" label="LinkedIn">
                  <Linkedin className="h-4 w-4" />
                </SocialLink>
              </div>
            </div>

            <FooterColumn
              title="Locations"
              links={[
                { label: "Browse all", to: "/listings/search" },
                { label: "Apartments", to: "/listings/search?propertyType=Apartment" },
                { label: "Houses", to: "/listings/search?propertyType=House" },
                { label: "Studios", to: "/listings/search?propertyType=Studio" },
                { label: "Lofts", to: "/listings/search?propertyType=Loft" },
              ]}
            />

            <FooterColumn
              title="Support"
              links={[
                { label: "Help center", to: "#" },
                { label: "Trust & safety", to: "#" },
                { label: "Cancellation options", to: "#" },
                { label: "Report a concern", to: "#" },
                { label: "Contact", to: "#" },
              ]}
            />

            <FooterColumn
              title="Lagedra Cares"
              links={[
                { label: "About us", to: "#" },
                { label: "Careers", to: "#" },
                { label: "Lagedra Trust Protocol", to: "#" },
                { label: "Press", to: "#" },
                { label: "Investors", to: "#" },
              ]}
            />
          </div>

          <div className="mt-12 flex flex-col gap-3 border-t border-border/60 pt-6 text-xs text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
            <p>
              &copy; {new Date().getFullYear()} Lagedra &middot; Mid-term rental trust protocol
            </p>
            <div className="flex flex-wrap items-center gap-x-5 gap-y-2">
              <a href="#" className="hover:text-foreground">Privacy</a>
              <a href="#" className="hover:text-foreground">Terms</a>
              <a href="#" className="hover:text-foreground">Cookies</a>
            </div>
          </div>
        </div>
      </footer>

      {/* ── Mobile bottom tab bar ────────────── */}
      <nav className="fixed bottom-0 left-0 right-0 z-50 border-t bg-background sm:hidden">
        <div className="flex items-stretch justify-around">
          {bottomTabs.map((tab) => (
            <BottomTab key={tab.to} item={tab} />
          ))}
        </div>
      </nav>
    </div>
  );
};

function HeaderLink({
  to,
  end,
  label,
}: {
  to: string;
  end?: boolean;
  label: string;
}) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) =>
        cn(
          "rounded-full px-4 py-2 text-sm font-medium transition-colors",
          isActive
            ? "bg-secondary text-foreground"
            : "text-muted-foreground hover:bg-secondary hover:text-foreground",
        )
      }
    >
      {label}
    </NavLink>
  );
}

function FooterColumn({
  title,
  links,
}: {
  title: string;
  links: { label: string; to: string }[];
}) {
  return (
    <div className="md:col-span-2">
      <h4 className="text-sm font-semibold text-foreground">{title}</h4>
      <ul className="mt-4 space-y-3">
        {links.map((link) => (
          <li key={link.label}>
            <Link
              to={link.to}
              className="text-sm text-muted-foreground transition-colors hover:text-foreground"
            >
              {link.label}
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}

function SocialLink({
  href,
  label,
  children,
}: {
  href: string;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <a
      href={href}
      aria-label={label}
      className="flex h-9 w-9 items-center justify-center rounded-full bg-background text-muted-foreground ring-1 ring-border transition-colors hover:text-primary hover:ring-primary/40"
    >
      {children}
    </a>
  );
}

function BottomTab({ item }: { item: NavItem }) {
  const icon = resolveIcon(item.icon);

  return (
    <NavLink
      to={item.to}
      end={item.end}
      className={({ isActive }) =>
        cn(
          "flex flex-1 flex-col items-center gap-0.5 py-2 text-[10px] font-medium transition-colors",
          isActive ? "text-primary" : "text-muted-foreground",
        )
      }
    >
      {createElement(icon, { className: "h-5 w-5" })}
      <span>{item.label}</span>
    </NavLink>
  );
}

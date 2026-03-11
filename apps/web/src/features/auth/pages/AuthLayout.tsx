import { Link, Outlet, useLocation } from "react-router-dom";
import { Shield } from "lucide-react";
import { cn } from "@/lib/utils";

const navLinks = [
  { to: "/auth/login", label: "Log in" },
  { to: "/auth/register", label: "Sign up" },
] as const;

export const AuthLayout = () => {
  const { pathname } = useLocation();

  return (
    <div className="flex min-h-screen flex-col bg-background">
      <header className="flex items-center justify-between border-b px-6 py-4 md:px-10">
        <Link to="/" className="flex items-center gap-2 text-accent hover:opacity-90 transition-opacity">
          <Shield className="h-7 w-7" />
          <span className="text-xl font-bold tracking-tight text-foreground">Lagedra</span>
        </Link>
        <nav className="flex items-center gap-1">
          {navLinks.map((link) => (
            <Link
              key={link.to}
              to={link.to}
              className={cn(
                "rounded-full px-4 py-2 text-sm font-medium transition-colors",
                pathname === link.to
                  ? "bg-secondary text-foreground"
                  : "text-muted-foreground hover:bg-secondary hover:text-foreground",
              )}
            >
              {link.label}
            </Link>
          ))}
        </nav>
      </header>

      <main className="flex flex-1 items-center justify-center px-4 py-12">
        <div className="w-full max-w-[440px]">
          <Outlet />
        </div>
      </main>

      <footer className="border-t px-6 py-6 md:px-10">
        <div className="flex flex-col items-center gap-2 text-xs text-muted-foreground sm:flex-row sm:justify-between">
          <p>&copy; {new Date().getFullYear()} Lagedra. Mid-term rental trust protocol.</p>
          <div className="flex gap-4">
            <Link to="/auth/forgot-password" className="hover:text-foreground transition-colors">
              Forgot password?
            </Link>
          </div>
        </div>
      </footer>
    </div>
  );
};

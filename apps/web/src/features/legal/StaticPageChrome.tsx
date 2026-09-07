import { useEffect, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { JoinLogo } from "@/features/join/components/JoinLogo";

type StaticPageChromeProps = {
  title: string;
  metaTitle: string;
  lede?: string;
  children: ReactNode;
};

export function StaticPageChrome({ title, metaTitle, lede, children }: StaticPageChromeProps) {
  useEffect(() => {
    const previous = window.document.title;
    window.document.title = metaTitle;
    return () => {
      window.document.title = previous;
    };
  }, [metaTitle]);

  return (
    <div className="min-h-screen bg-white">
      <header className="sticky top-0 z-10 border-b border-[#E5E5EE] bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <Link to="/listings" className="hover:opacity-90">
            <JoinLogo />
          </Link>
          <nav className="flex flex-wrap items-center justify-end gap-4 text-sm font-semibold text-[#3D3D4E]">
            <Link to="/about" className="hover:text-[#1A1A2E]">
              About
            </Link>
            <Link to="/how-it-works" className="hover:text-[#1A1A2E]">
              How it works
            </Link>
            <Link to="/faq" className="hover:text-[#1A1A2E]">
              FAQ
            </Link>
            <Link to="/join" className="hover:text-[#1A1A2E]">
              Join
            </Link>
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-3xl px-6 py-12 sm:py-16">
        <p className="text-xs font-semibold uppercase tracking-wider text-[#ABABBE]">Lagedra</p>
        <h1 className="mt-3 text-4xl font-extrabold tracking-tight text-[#1A1A2E] sm:text-5xl">
          {title}
        </h1>
        {lede ? <p className="mt-4 text-lg leading-relaxed text-[#3D3D4E]">{lede}</p> : null}
        <div className="mt-10">{children}</div>
      </main>

      <footer className="border-t border-[#E5E5EE] px-6 py-8">
        <div className="mx-auto flex max-w-5xl flex-col gap-3 text-xs text-[#ABABBE] sm:flex-row sm:items-center sm:justify-between">
          <p>&copy; {new Date().getFullYear()} Lagedra. Mid-term rental trust protocol.</p>
          <StaticPageFooterLinks />
        </div>
      </footer>
    </div>
  );
}

export function StaticPageFooterLinks({ className }: { className?: string }) {
  return (
    <div className={className ?? "flex flex-wrap gap-4"}>
      <Link to="/about" className="hover:text-[#1A1A2E]">
        About
      </Link>
      <Link to="/how-it-works" className="hover:text-[#1A1A2E]">
        How it works
      </Link>
      <Link to="/faq" className="hover:text-[#1A1A2E]">
        FAQ
      </Link>
      <Link to="/contact" className="hover:text-[#1A1A2E]">
        Contact
      </Link>
      <Link to="/tc" className="hover:text-[#1A1A2E]">
        Terms
      </Link>
      <Link to="/privacy" className="hover:text-[#1A1A2E]">
        Privacy
      </Link>
      <Link to="/sms" className="hover:text-[#1A1A2E]">
        Text messages
      </Link>
    </div>
  );
}

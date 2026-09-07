import { useEffect } from "react";
import { Link } from "react-router-dom";
import { JoinLogo } from "@/features/join/components/JoinLogo";
import type { LegalDocument } from "./legalTypes";

type LegalPageLayoutProps = {
  document: LegalDocument;
  other: { label: string; to: string };
};

export function LegalPageLayout({ document, other }: LegalPageLayoutProps) {
  useEffect(() => {
    const previous = window.document.title;
    window.document.title = document.metaTitle;
    return () => {
      window.document.title = previous;
    };
  }, [document.metaTitle]);

  useEffect(() => {
    const hash = window.location.hash.replace("#", "");
    if (!hash) return;
    const el = window.document.getElementById(hash);
    el?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, [document.metaTitle]);

  return (
    <div className="min-h-screen bg-white">
      <header className="sticky top-0 z-10 border-b border-[#E5E5EE] bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <Link to="/listings" className="hover:opacity-90">
            <JoinLogo />
          </Link>
          <nav className="flex items-center gap-4 text-sm font-semibold text-[#3D3D4E]">
            <Link to={other.to} className="hover:text-[#1A1A2E]">
              {other.label}
            </Link>
            <Link to="/sms" className="hover:text-[#1A1A2E]">
              Text messages
            </Link>
            <Link to="/join" className="hover:text-[#1A1A2E]">
              Join
            </Link>
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-6 py-12 sm:py-16">
        <div className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-wider text-[#ABABBE]">Legal</p>
          <h1 className="mt-3 text-4xl font-extrabold tracking-tight text-[#1A1A2E] sm:text-5xl">
            {document.title}
          </h1>
          <p className="mt-4 text-lg leading-relaxed text-[#3D3D4E]">{document.lede}</p>
          <p className="mt-3 text-sm text-[#ABABBE]">
            Effective {document.effectiveDateLabel}
          </p>
        </div>

        <div className="mt-10 grid gap-10 lg:grid-cols-[220px_minmax(0,1fr)] lg:gap-14">
          <aside className="lg:sticky lg:top-24 lg:self-start">
            <p className="text-xs font-semibold uppercase tracking-wider text-[#ABABBE]">
              On this page
            </p>
            <ol className="mt-3 space-y-2 text-sm">
              {document.sections.map((section) => (
                <li key={section.id}>
                  <a
                    href={`#${section.id}`}
                    className="text-[#3D3D4E] hover:text-[#5B3FE0]"
                  >
                    {section.title}
                  </a>
                </li>
              ))}
            </ol>
          </aside>

          <article className="max-w-3xl space-y-10 text-[15px] leading-7 text-[#3D3D4E] [&_a]:font-medium [&_a]:text-[#5B3FE0] [&_a]:underline [&_a]:underline-offset-2 [&_h2]:text-xl [&_h2]:font-bold [&_h2]:tracking-tight [&_h2]:text-[#1A1A2E] [&_li]:pl-1 [&_p+p]:mt-4 [&_strong]:font-semibold [&_strong]:text-[#1A1A2E] [&_ul]:mt-3 [&_ul]:list-disc [&_ul]:space-y-2 [&_ul]:pl-5">
            {document.sections.map((section) => (
              <section key={section.id} id={section.id} className="scroll-mt-24">
                <h2>{section.title}</h2>
                <div className="mt-3">{section.content}</div>
              </section>
            ))}
          </article>
        </div>
      </main>

      <footer className="border-t border-[#E5E5EE] px-6 py-8">
        <div className="mx-auto flex max-w-5xl flex-col gap-3 text-xs text-[#ABABBE] sm:flex-row sm:items-center sm:justify-between">
          <p>&copy; {new Date().getFullYear()} Lagedra. Mid-term rental trust protocol.</p>
          <div className="flex flex-wrap gap-4">
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
            <a href="mailto:info@lagedra.com" className="hover:text-[#1A1A2E]">
              info@lagedra.com
            </a>
          </div>
        </div>
      </footer>
    </div>
  );
}

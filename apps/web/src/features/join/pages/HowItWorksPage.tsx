import { useEffect } from "react";
import { Link } from "react-router-dom";
import { Check, ShieldCheck } from "lucide-react";
import { BackLink } from "@/components/shared/BackLink";
import { usePublicConfigStore } from "@/app/config/publicConfigStore";
import { JoinLogo } from "../components/JoinLogo";
import { FaqAccordion } from "../components/FaqAccordion";
import { howItWorksContent } from "../joinContent";

export const HowItWorksPage = () => {
  const preLaunchEnabled = usePublicConfigStore((s) => s.preLaunchEnabled);

  useEffect(() => {
    const hash = window.location.hash.replace("#", "");
    if (!hash) return;
    window.document.getElementById(hash)?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, []);

  return (
    <div className="min-h-screen bg-white">
      <header className="sticky top-0 z-10 border-b border-[#E5E5EE] bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-4xl items-center justify-between px-6 py-4">
          <JoinLogo />
          <BackLink
            fallbackTo="/join"
            label="Back"
            className="text-sm font-semibold text-[#3D3D4E] transition-colors hover:text-[#1A1A2E]"
          />
        </div>
      </header>

      <main className="mx-auto max-w-4xl px-6 py-14 sm:py-20">
        {/* Hero */}
        <div className="text-center">
          <span className="inline-flex rounded-full bg-[#F3F0FE] px-4 py-1.5 text-sm font-semibold text-[#5B3FE0]">
            {howItWorksContent.badge}
          </span>
          <h1 className="mx-auto mt-6 max-w-3xl text-4xl font-extrabold leading-tight tracking-tight text-[#1A1A2E] sm:text-6xl">
            {howItWorksContent.title}
          </h1>
          <p className="mx-auto mt-5 max-w-2xl text-lg text-[#3D3D4E]">
            {howItWorksContent.subtitle}
          </p>
        </div>

        {/* From request to move-in */}
        <section className="mt-20">
          <p className="text-center text-xs font-semibold uppercase tracking-wider text-[#ABABBE]">
            {howItWorksContent.flowHeading}
          </p>
          <div className="mt-8 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
            {howItWorksContent.flow.map((step) => (
              <div
                key={step.num}
                className="rounded-2xl border border-[#E5E5EE] bg-white p-5"
              >
                <span className="flex h-9 w-9 items-center justify-center rounded-full bg-[#F3F0FE] text-sm font-bold text-[#5B3FE0]">
                  {step.num}
                </span>
                <p className="mt-4 font-bold text-[#1A1A2E]">{step.title}</p>
                <p className="mt-1.5 text-sm text-[#3D3D4E]">{step.body}</p>
              </div>
            ))}
          </div>
        </section>

        {/* Why different */}
        <section className="mt-20 rounded-3xl bg-[#5B3FE0] px-8 py-12 text-white sm:px-12">
          <h2 className="text-3xl font-extrabold tracking-tight">
            {howItWorksContent.differentHeading}
          </h2>
          <p className="mt-4 max-w-2xl text-white/85">{howItWorksContent.differentBody}</p>
          <div className="mt-8 flex flex-wrap gap-3">
            {howItWorksContent.differentPills.map((pill) => (
              <span
                key={pill}
                className="inline-flex items-center gap-2 rounded-full bg-white/15 px-4 py-2 text-sm font-medium"
              >
                <Check className="h-4 w-4" strokeWidth={3} />
                {pill}
              </span>
            ))}
          </div>
        </section>

        {/* FAQ */}
        <section id="faq" className="mt-20 scroll-mt-24">
          <h2 className="text-center text-3xl font-extrabold tracking-tight text-[#1A1A2E]">
            {howItWorksContent.faqHeading}
          </h2>
          <FaqAccordion className="mx-auto mt-8 max-w-2xl" />
        </section>

        {/* CTA */}
        <section className="mt-20 flex flex-col items-center rounded-3xl border border-[#E5E5EE] bg-[#F3F0FE]/40 px-8 py-12 text-center">
          <ShieldCheck className="h-10 w-10 text-[#5B3FE0]" />
          <h2 className="mt-4 max-w-xl text-2xl font-extrabold tracking-tight text-[#1A1A2E]">
            {preLaunchEnabled
              ? howItWorksContent.preLaunchCta
              : "Ready to house your clients with confidence?"}
          </h2>
          <div className="mt-6 flex flex-wrap items-center justify-center gap-3">
            <Link
              to="/join"
              className="inline-flex items-center gap-2 rounded-xl bg-[#5B3FE0] px-6 py-3 text-[15px] font-semibold text-white transition-colors hover:bg-[#4A2FC7]"
            >
              Back to start
            </Link>
            {!preLaunchEnabled && (
              <Link
                to="/listings"
                className="inline-flex items-center gap-2 rounded-xl border border-[#E5E5EE] bg-white px-6 py-3 text-[15px] font-semibold text-[#1A1A2E] transition-colors hover:border-[#5B3FE0]/40"
              >
                Browse rentals
              </Link>
            )}
          </div>
        </section>
      </main>
    </div>
  );
};

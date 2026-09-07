import { Link } from "react-router-dom";
import { FaqAccordion } from "@/features/join/components/FaqAccordion";
import { howItWorksContent } from "@/features/join/joinContent";
import { StaticPageChrome } from "../StaticPageChrome";

export const FaqPage = () => (
  <StaticPageChrome
    title={howItWorksContent.faqHeading}
    metaTitle="FAQ — Lagedra"
    lede="Answers about joining Lagedra, the homes on the platform, and how placements work."
  >
    <FaqAccordion />
    <p className="mt-8 text-sm text-[#3D3D4E]">
      Want the full walkthrough? See{" "}
      <Link to="/how-it-works" className="font-medium text-[#5B3FE0] underline underline-offset-2">
        How it works
      </Link>
      . Still stuck?{" "}
      <Link to="/contact" className="font-medium text-[#5B3FE0] underline underline-offset-2">
        Contact us
      </Link>
      .
    </p>
  </StaticPageChrome>
);

import { Link } from "react-router-dom";
import { COMPANY } from "../companyInfo";
import { StaticPageChrome } from "../StaticPageChrome";

export const AboutPage = () => (
  <StaticPageChrome
    title="About Lagedra"
    metaTitle="About Lagedra"
    lede="Lagedra is a mid-term rental marketplace and trust protocol. We connect guests, hosts, and institutional partners for furnished stays of 30 days or longer."
  >
    <div className="space-y-8 text-[15px] leading-7 text-[#3D3D4E]">
      <section>
        <h2 className="text-xl font-bold tracking-tight text-[#1A1A2E]">What we do</h2>
        <p className="mt-3">
          At <a href="https://www.lagedra.com">www.lagedra.com</a>, hosts list
          homes, guests search and book, and relocation or insurance partners
          source housing for clients. The product includes listing review,
          identity verification, payments, lease documents, and in-platform
          dispute tools. Lagedra is not the landlord, insurer, or bank for a
          stay — hosts offer the home and guests occupy it.
        </p>
        <p className="mt-3">
          Learn more on{" "}
          <Link to="/how-it-works" className="font-medium text-[#5B3FE0] underline underline-offset-2">
            How it works
          </Link>{" "}
          and the{" "}
          <Link to="/faq" className="font-medium text-[#5B3FE0] underline underline-offset-2">
            FAQ
          </Link>
          .
        </p>
      </section>

      <section>
        <h2 className="text-xl font-bold tracking-tight text-[#1A1A2E]">Business information</h2>
        <dl className="mt-3 grid gap-3 sm:grid-cols-[160px_1fr]">
          <dt className="font-semibold text-[#1A1A2E]">Brand</dt>
          <dd>{COMPANY.brand}</dd>
          <dt className="font-semibold text-[#1A1A2E]">Legal name</dt>
          <dd>{COMPANY.legalName}</dd>
          <dt className="font-semibold text-[#1A1A2E]">Mailing address</dt>
          <dd>
            {COMPANY.street}
            <br />
            {COMPANY.city}, {COMPANY.region} {COMPANY.postalCode}
          </dd>
          <dt className="font-semibold text-[#1A1A2E]">Website</dt>
          <dd>
            <a href="https://www.lagedra.com" className="font-medium text-[#5B3FE0] underline underline-offset-2">
              www.lagedra.com
            </a>
          </dd>
          <dt className="font-semibold text-[#1A1A2E]">Email</dt>
          <dd>
            <a href="mailto:info@lagedra.com" className="font-medium text-[#5B3FE0] underline underline-offset-2">
              info@lagedra.com
            </a>
          </dd>
          <dt className="font-semibold text-[#1A1A2E]">Phone</dt>
          <dd>
            <a href="tel:+12137352362" className="font-medium text-[#5B3FE0] underline underline-offset-2">
              213-735-2362
            </a>
          </dd>
          <dt className="font-semibold text-[#1A1A2E]">Industry</dt>
          <dd>Online marketplace for mid-term residential rentals</dd>
          <dt className="font-semibold text-[#1A1A2E]">Location</dt>
          <dd>
            {COMPANY.country} (California law governs the platform terms)
          </dd>
        </dl>
      </section>

      <section>
        <h2 className="text-xl font-bold tracking-tight text-[#1A1A2E]">Policies and messages</h2>
        <ul className="mt-3 list-disc space-y-2 pl-5">
          <li>
            <Link to="/tc" className="font-medium text-[#5B3FE0] underline underline-offset-2">
              Terms and Conditions
            </Link>
          </li>
          <li>
            <Link to="/privacy" className="font-medium text-[#5B3FE0] underline underline-offset-2">
              Privacy Policy
            </Link>
          </li>
          <li>
            <Link to="/sms" className="font-medium text-[#5B3FE0] underline underline-offset-2">
              Text alerts (campaign opt-in)
            </Link>
          </li>
          <li>
            <Link to="/sms#otp" className="font-medium text-[#5B3FE0] underline underline-offset-2">
              Phone verification (one-time passcodes)
            </Link>
          </li>
          <li>
            <Link to="/contact" className="font-medium text-[#5B3FE0] underline underline-offset-2">
              Contact
            </Link>
          </li>
        </ul>
      </section>
    </div>
  </StaticPageChrome>
);

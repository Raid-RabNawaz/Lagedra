import { Link } from "react-router-dom";
import { COMPANY } from "../companyInfo";
import { StaticPageChrome } from "../StaticPageChrome";

export const ContactPage = () => (
  <StaticPageChrome
    title="Contact"
    metaTitle="Contact — Lagedra"
    lede="Reach the Lagedra team for account, booking, or partnership questions."
  >
    <div className="space-y-4 text-[15px] leading-7 text-[#3D3D4E]">
      <p>
        {COMPANY.legalName} operates the mid-term rental marketplace at{" "}
        <a href="https://www.lagedra.com" className="font-medium text-[#5B3FE0] underline underline-offset-2">
          www.lagedra.com
        </a>
        . See{" "}
        <Link to="/about" className="font-medium text-[#5B3FE0] underline underline-offset-2">
          About
        </Link>{" "}
        for business information.
      </p>
      <p>
        Email:{" "}
        <a
          href="mailto:info@lagedra.com"
          className="font-medium text-[#5B3FE0] underline underline-offset-2"
        >
          info@lagedra.com
        </a>
      </p>
      <p>
        Phone:{" "}
        <a href="tel:+12137352362" className="font-medium text-[#5B3FE0] underline underline-offset-2">
          213-735-2362
        </a>
      </p>
      <p>
        Mail: {COMPANY.legalName}, {COMPANY.street}, {COMPANY.city},{" "}
        {COMPANY.region} {COMPANY.postalCode}
      </p>
      <p>
        Text-message opt-in and opt-out:{" "}
        <Link to="/sms" className="font-medium text-[#5B3FE0] underline underline-offset-2">
          www.lagedra.com/sms
        </Link>
      </p>
      <p>
        Legal:{" "}
        <Link to="/tc" className="font-medium text-[#5B3FE0] underline underline-offset-2">
          Terms
        </Link>
        {" · "}
        <Link to="/privacy" className="font-medium text-[#5B3FE0] underline underline-offset-2">
          Privacy
        </Link>
      </p>
    </div>
  </StaticPageChrome>
);

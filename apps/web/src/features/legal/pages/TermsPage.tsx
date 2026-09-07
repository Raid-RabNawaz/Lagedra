import { LegalPageLayout } from "../LegalPageLayout";
import { termsDocument } from "../termsContent";

export const TermsPage = () => (
  <LegalPageLayout document={termsDocument} other={{ label: "Privacy Policy", to: "/privacy" }} />
);

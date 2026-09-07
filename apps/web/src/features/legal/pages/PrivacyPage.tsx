import { LegalPageLayout } from "../LegalPageLayout";
import { privacyDocument } from "../privacyContent";

export const PrivacyPage = () => (
  <LegalPageLayout document={privacyDocument} other={{ label: "Terms", to: "/tc" }} />
);

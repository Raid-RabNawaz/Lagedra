import type { ReactNode } from "react";

export type LegalDocument = {
  title: string;
  metaTitle: string;
  lede: string;
  effectiveDateLabel: string;
  sections: LegalSection[];
};

export type LegalSection = {
  id: string;
  title: string;
  content: ReactNode;
};

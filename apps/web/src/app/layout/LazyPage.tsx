import { Suspense, type ReactNode } from "react";
import { PageBoundary } from "@/app/layout/PageBoundary";
import { Loader } from "@/components/shared/Loader";

export function LazyPage({ children }: { children: ReactNode }) {
  return (
    <PageBoundary>
      <Suspense fallback={<Loader fullPage label="Loading..." />}>{children}</Suspense>
    </PageBoundary>
  );
}

import { useCallback, useEffect, useState } from "react";
import { ExternalLink, FileDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { applicationApi } from "@/features/applications/services/applicationApi";
import type { ScreeningReportView } from "@/api/types";

function productTone(status: string | null) {
  if (status?.toLowerCase() === "clear") return "text-emerald-800";
  if (status?.toLowerCase() === "consider") return "text-amber-900";
  return "text-muted-foreground";
}

function productLabel(status: string | null) {
  if (status?.toLowerCase() === "clear") return "Clear";
  if (status?.toLowerCase() === "consider") return "Review";
  return status ?? "Pending";
}

function resultLabel(result: ScreeningReportView["overallResult"]) {
  if (result === "Pass") return "Pass";
  if (result === "Review") return "Review";
  return result;
}

export function ScreeningReportViewer({
  applicationId,
  report,
}: {
  applicationId: string;
  report: ScreeningReportView;
}) {
  const [pdfUrl, setPdfUrl] = useState<string | null>(null);
  const [pdfError, setPdfError] = useState<string | null>(null);
  const [pdfBusy, setPdfBusy] = useState(false);
  const [loadKey, setLoadKey] = useState(0);

  const loadPdf = useCallback(async () => {
    setPdfError(null);
    const file = await applicationApi.downloadScreeningReportPdf(applicationId);
    if (!file) {
      setPdfError(
        "The provider report file is still rendering. Wait a moment and try again.",
      );
      return null;
    }
    return URL.createObjectURL(file.blob);
  }, [applicationId]);

  useEffect(() => {
    let revoked: string | null = null;
    let cancelled = false;
    void loadPdf().then((url) => {
      if (cancelled) {
        if (url) URL.revokeObjectURL(url);
        return;
      }
      if (url) {
        revoked = url;
        setPdfUrl(url);
      }
    });
    return () => {
      cancelled = true;
      if (revoked) URL.revokeObjectURL(revoked);
    };
  }, [loadPdf, loadKey]);

  const products = report.products ?? [];

  return (
    <div className="space-y-3 rounded-md border bg-background p-3">
      <div>
        <p className="font-semibold">{resultLabel(report.overallResult)}</p>
        {report.summary && (
          <p className="mt-1 text-sm text-muted-foreground">{report.summary}</p>
        )}
      </div>

      {products.length > 0 && (
        <ul className="space-y-2 text-sm">
          {products.map((product) => (
            <li key={product.key} className="flex flex-col gap-0.5 border-t pt-2 first:border-t-0 first:pt-0">
              <div className="flex items-center justify-between gap-2">
                <span className="font-medium">{product.label}</span>
                <span className={productTone(product.status)}>{productLabel(product.status)}</span>
              </div>
              {product.detail && (
                <p className="text-xs text-muted-foreground">{product.detail}</p>
              )}
            </li>
          ))}
        </ul>
      )}

      {pdfUrl ? (
        <iframe
          title="Screening report from Checkr"
          src={pdfUrl}
          className="h-[32rem] w-full rounded-md border bg-white"
        />
      ) : pdfError ? (
        <Alert>
          {pdfError} The section above is the same result Checkr returned for this order.
        </Alert>
      ) : (
        <p className="text-xs text-muted-foreground">
          Loading the provider PDF. Checkr can take up to a minute to render it.
        </p>
      )}

      <div className="flex flex-wrap gap-2">
        {pdfUrl && (
          <Button
            variant="outline"
            size="sm"
            className="gap-1"
            onClick={() => window.open(pdfUrl, "_blank", "noopener")}
          >
            <ExternalLink className="h-3.5 w-3.5" />
            Open report
          </Button>
        )}
        <Button
          variant="outline"
          size="sm"
          className="gap-1"
          disabled={pdfBusy}
          onClick={() => {
            setPdfBusy(true);
            void applicationApi.downloadScreeningReportPdf(applicationId).then((file) => {
              setPdfBusy(false);
              if (!file) {
                setPdfError(
                  "The provider report file is still rendering. Wait a moment and try again.",
                );
                return;
              }
              const url = URL.createObjectURL(file.blob);
              const a = document.createElement("a");
              a.href = url;
              a.download = file.filename;
              a.click();
              URL.revokeObjectURL(url);
            });
          }}
        >
          <FileDown className="h-3.5 w-3.5" />
          {pdfBusy ? "Downloading…" : "Download PDF"}
        </Button>
        {pdfError && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setPdfUrl(null);
              setPdfError(null);
              setLoadKey((n) => n + 1);
            }}
          >
            Retry PDF
          </Button>
        )}
      </div>
    </div>
  );
}

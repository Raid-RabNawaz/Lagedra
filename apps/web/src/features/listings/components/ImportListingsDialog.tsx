import { useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  CheckCircle2,
  Download,
  FileCode,
  FileSpreadsheet,
  Loader2,
  Upload,
  XCircle,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { AmenityDefinitionDto } from "@/api/types";
import { getApiErrorMessage } from "@/api/errors";
import { listingApi } from "@/features/listings/services/listingApi";
import { toCreateListingRequest } from "@/features/listings/lib/toListingRequests";
import {
  MAX_IMPORT_ROWS,
  type ParsedListingFile,
  type ParsedListingRow,
} from "@/features/listings/lib/listingImportShared";
import {
  buildListingImportTemplate,
  parseListingImportWorkbook,
} from "@/features/listings/lib/listingExcelImport";
import {
  buildListingXmlTemplate,
  parseListingImportXml,
} from "@/features/listings/lib/listingXmlImport";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

export type ListingImportFormat = "excel" | "xml";

type FormatConfig = {
  buttonLabel: string;
  icon: LucideIcon;
  title: string;
  description: string;
  templateHint: string;
  templateFileName: string;
  accept: string;
  parseFailureMessage: string;
  /** What one entry is called in review copy: "Row" for Excel, "Listing" for XML. */
  rowNoun: string;
  buildTemplate: (amenities: readonly AmenityDefinitionDto[]) => Promise<Blob>;
  parseFile: (
    file: File,
    amenities: readonly AmenityDefinitionDto[],
  ) => Promise<ParsedListingFile>;
};

const FORMATS: Record<ListingImportFormat, FormatConfig> = {
  excel: {
    buttonLabel: "Import from Excel",
    icon: FileSpreadsheet,
    title: "Import listings from Excel",
    description:
      "Download the template, fill in one listing per row, then upload the file. Each row becomes a draft you can review and submit for review.",
    templateHint:
      "The template includes an example row, the list of valid amenity names and instructions for every column.",
    templateFileName: "lagedra-listings-template.xlsx",
    accept: ".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    parseFailureMessage:
      "Something went wrong while reading the file. Make sure it is the .xlsx template and try again.",
    rowNoun: "Row",
    buildTemplate: (amenities) => buildListingImportTemplate(amenities),
    parseFile: async (file, amenities) =>
      parseListingImportWorkbook(await file.arrayBuffer(), amenities),
  },
  xml: {
    buttonLabel: "Import from XML",
    icon: FileCode,
    title: "Import listings from XML",
    description:
      "Download the template, add one <listing> element per listing, then upload the file. Each listing becomes a draft you can review and submit for review. Address and photo URLs in the file are applied automatically.",
    templateHint:
      "The template includes a commented example listing, the list of valid amenity names and notes for every field. "
      + "Zillow/HotPads-style property feeds (<properties> with <property> entries) are also detected and imported automatically.",
    templateFileName: "lagedra-listings-template.xml",
    accept: ".xml,text/xml,application/xml",
    parseFailureMessage:
      "Something went wrong while reading the file. Make sure it is the .xml template and try again.",
    rowNoun: "Listing",
    buildTemplate: (amenities) =>
      Promise.resolve(
        new Blob([buildListingXmlTemplate(amenities)], { type: "application/xml" }),
      ),
    parseFile: async (file, amenities) => parseListingImportXml(await file.text(), amenities),
  },
};

type ImportFailure = {
  rowNumber: number;
  title: string;
  message: string;
};

type ImportOutcome = {
  createdCount: number;
  failures: ImportFailure[];
  /** Created listings that still had address/photo problems. */
  partials: ImportFailure[];
};

type Phase = "pick" | "review" | "importing" | "done";

type ImportListingsDialogProps = {
  format: ListingImportFormat;
  amenities: AmenityDefinitionDto[];
};

/**
 * Bulk import: the host downloads a template (Excel or XML), fills in one
 * listing per row/element and uploads it back. Every valid entry is created
 * as a Draft through the regular create-listing endpoint, so all server-side
 * validation and ownership rules apply. Nothing is published — the host
 * reviews each draft and submits it for review individually. When the file
 * includes an address or photo URLs, those are applied on the draft as well.
 */
export function ImportListingsDialog({ format, amenities }: ImportListingsDialogProps) {
  const config = FORMATS[format];
  const ButtonIcon = config.icon;
  const rowNounPlural = `${config.rowNoun.toLowerCase()}s`;

  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [open, setOpen] = useState(false);
  const [phase, setPhase] = useState<Phase>("pick");
  const [downloading, setDownloading] = useState(false);
  const [parsing, setParsing] = useState(false);
  const [fileName, setFileName] = useState<string | null>(null);
  const [parsed, setParsed] = useState<ParsedListingFile | null>(null);
  const [parseFailure, setParseFailure] = useState<string | null>(null);
  const [progress, setProgress] = useState({ done: 0, total: 0 });
  const [outcome, setOutcome] = useState<ImportOutcome | null>(null);

  const validRows = parsed?.rows.filter((r) => r.values !== null) ?? [];
  const invalidRows = parsed?.rows.filter((r) => r.values === null) ?? [];

  const reset = () => {
    setPhase("pick");
    setFileName(null);
    setParsed(null);
    setParseFailure(null);
    setProgress({ done: 0, total: 0 });
    setOutcome(null);
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const handleOpenChange = (next: boolean) => {
    if (!next && phase === "importing") return; // don't abandon a running import
    setOpen(next);
    if (!next) reset();
  };

  const handleDownloadTemplate = async () => {
    setDownloading(true);
    try {
      const blob = await config.buildTemplate(amenities);
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = config.templateFileName;
      link.click();
      URL.revokeObjectURL(url);
    } finally {
      setDownloading(false);
    }
  };

  const handleFileSelected = async (file: File | undefined) => {
    if (!file) return;
    setParsing(true);
    setParseFailure(null);
    setFileName(file.name);
    try {
      const result = await config.parseFile(file, amenities);
      setParsed(result);
      setPhase("review");
    } catch {
      setParseFailure(config.parseFailureMessage);
    } finally {
      setParsing(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  const handleImport = async () => {
    const rows = validRows;
    if (rows.length === 0) return;
    setPhase("importing");
    setProgress({ done: 0, total: rows.length });

    let createdCount = 0;
    const failures: ImportFailure[] = [];
    const partials: ImportFailure[] = [];
    // Sequential on purpose: keeps server load predictable and error
    // attribution per-row simple.
    for (const row of rows) {
      try {
        const created = await listingApi.create(
          toCreateListingRequest(row.values!, {
            addedVia: format === "excel" ? "Excel" : "Xml",
          }),
        );
        const notes: string[] = [];

        if (row.address) {
          try {
            await listingApi.lockAddress(created.id, {
              street: row.address.street,
              city: row.address.city,
              state: row.address.state,
              zipCode: row.address.zipCode,
              country: row.address.country,
              jurisdictionCode: null,
            });
          } catch (error) {
            notes.push(`Address was not applied: ${getApiErrorMessage(error)}`);
          }
        }

        if (row.photoUrls && row.photoUrls.length > 0) {
          try {
            const photos = await listingApi.importPhotosFromUrls(created.id, row.photoUrls);
            if (photos.failed > 0) {
              notes.push(
                `${photos.uploaded} photo${photos.uploaded === 1 ? "" : "s"} imported, ${photos.failed} could not be fetched.`,
              );
            }
          } catch (error) {
            notes.push(`Photos were not imported: ${getApiErrorMessage(error)}`);
          }
        }

        createdCount += 1;
        if (notes.length > 0) {
          partials.push({
            rowNumber: row.rowNumber,
            title: row.title,
            message: notes.join(" "),
          });
        }
      } catch (error) {
        failures.push({
          rowNumber: row.rowNumber,
          title: row.title,
          message: getApiErrorMessage(error),
        });
      }
      setProgress((p) => ({ ...p, done: p.done + 1 }));
    }

    void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    setOutcome({ createdCount, failures, partials });
    setPhase("done");
  };

  const handleGoToListings = () => {
    setOpen(false);
    reset();
    void navigate("/app/listings");
  };

  const renderRowIssues = (rows: ParsedListingRow[]) => (
    <ul className="space-y-2">
      {rows.map((row) => (
        <li key={row.rowNumber} className="rounded-md border bg-muted/30 p-2 text-sm">
          <p className="font-medium">
            {config.rowNoun} {row.rowNumber} — {row.title}
          </p>
          <ul className="mt-1 list-disc pl-5 text-muted-foreground">
            {row.errors.map((error) => (
              <li key={error}>{error}</li>
            ))}
          </ul>
        </li>
      ))}
    </ul>
  );

  const warningsToShow = validRows.filter((r) => r.warnings.length > 0);

  return (
    <>
      <Button type="button" variant="outline" onClick={() => setOpen(true)}>
        <ButtonIcon className="mr-2 h-4 w-4" />
        {config.buttonLabel}
      </Button>

      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{config.title}</DialogTitle>
            <DialogDescription>{config.description}</DialogDescription>
          </DialogHeader>

          {phase === "pick" && (
            <div className="space-y-4">
              <div className="rounded-lg border bg-muted/30 p-4">
                <p className="text-sm font-medium">Step 1 — Get the template</p>
                <p className="mt-1 text-sm text-muted-foreground">{config.templateHint}</p>
                <Button
                  type="button"
                  variant="outline"
                  className="mt-3"
                  onClick={handleDownloadTemplate}
                  disabled={downloading}
                >
                  <Download className="mr-2 h-4 w-4" />
                  {downloading ? "Preparing..." : "Download template"}
                </Button>
              </div>

              <div className="rounded-lg border bg-muted/30 p-4">
                <p className="text-sm font-medium">Step 2 — Upload the filled-in file</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  Up to {MAX_IMPORT_ROWS} listings per file. We'll check every listing before
                  anything is created.
                </p>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept={config.accept}
                  className="hidden"
                  onChange={(e) => void handleFileSelected(e.target.files?.[0])}
                />
                <Button
                  type="button"
                  className="mt-3"
                  onClick={() => fileInputRef.current?.click()}
                  disabled={parsing}
                >
                  {parsing ? (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  ) : (
                    <Upload className="mr-2 h-4 w-4" />
                  )}
                  {parsing ? "Reading file..." : "Upload file"}
                </Button>
              </div>

              {parseFailure && (
                <Alert variant="destructive">
                  <AlertTriangle className="h-4 w-4" />
                  <AlertDescription>{parseFailure}</AlertDescription>
                </Alert>
              )}
            </div>
          )}

          {phase === "review" && parsed && (
            <div className="space-y-4">
              {fileName && (
                <p className="text-sm text-muted-foreground">
                  File: <span className="font-medium text-foreground">{fileName}</span>
                </p>
              )}

              {parsed.fileErrors.map((error) => (
                <Alert key={error} variant="destructive">
                  <AlertTriangle className="h-4 w-4" />
                  <AlertDescription>{error}</AlertDescription>
                </Alert>
              ))}

              {parsed.rows.length > 0 && (
                <div className="flex items-center gap-2 text-sm font-medium">
                  {validRows.length > 0 ? (
                    <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                  ) : (
                    <XCircle className="h-4 w-4 text-destructive" />
                  )}
                  <span>
                    {validRows.length} of {parsed.rows.length} listing
                    {parsed.rows.length === 1 ? "" : "s"} ready to import
                  </span>
                </div>
              )}

              {validRows.some((r) => r.address || (r.photoUrls && r.photoUrls.length > 0)) && (
                <p className="text-sm text-muted-foreground">
                  {validRows.filter((r) => r.address).length} with address
                  {validRows.filter((r) => r.photoUrls && r.photoUrls.length > 0).length > 0
                    ? `, ${validRows.reduce((n, r) => n + (r.photoUrls?.length ?? 0), 0)} photos`
                    : ""}{" "}
                  will be applied after each draft is created.
                </p>
              )}

              {(invalidRows.length > 0 || warningsToShow.length > 0) && (
                <div className="max-h-64 space-y-3 overflow-y-auto pr-1">
                  {invalidRows.length > 0 && (
                    <div className="space-y-2">
                      <p className="text-sm font-medium text-destructive">
                        These {rowNounPlural} have problems and will be skipped:
                      </p>
                      {renderRowIssues(invalidRows)}
                    </div>
                  )}

                  {warningsToShow.length > 0 && (
                    <div className="space-y-2">
                      <p className="text-sm font-medium text-amber-700">Heads up:</p>
                      <ul className="space-y-2">
                        {warningsToShow.map((row) => (
                          <li
                            key={row.rowNumber}
                            className="rounded-md border border-amber-200 bg-amber-50 p-2 text-sm text-amber-900"
                          >
                            <p className="font-medium">
                              {config.rowNoun} {row.rowNumber} — {row.title}
                            </p>
                            <ul className="mt-1 list-disc pl-5">
                              {row.warnings.map((warning) => (
                                <li key={warning}>{warning}</li>
                              ))}
                            </ul>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              )}

              <DialogFooter>
                <Button type="button" variant="outline" onClick={reset}>
                  Choose another file
                </Button>
                <Button type="button" onClick={handleImport} disabled={validRows.length === 0}>
                  Import {validRows.length} listing{validRows.length === 1 ? "" : "s"}
                </Button>
              </DialogFooter>
            </div>
          )}

          {phase === "importing" && (
            <div className="space-y-4 py-4">
              <div className="flex items-center gap-3 text-sm">
                <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
                <span>
                  Creating listing {Math.min(progress.done + 1, progress.total)} of {progress.total}
                  {validRows[progress.done]?.photoUrls && validRows[progress.done]!.photoUrls!.length > 0
                    ? " (including photos)"
                    : ""}
                  ...
                </span>
              </div>
              <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                <div
                  className="h-full rounded-full bg-primary transition-all"
                  style={{
                    width: `${progress.total === 0 ? 0 : Math.round((progress.done / progress.total) * 100)}%`,
                  }}
                />
              </div>
              <p className="text-sm text-muted-foreground">
                Keep this window open until the import finishes.
              </p>
            </div>
          )}

          {phase === "done" && outcome && (
            <div className="space-y-4">
              {outcome.createdCount > 0 && (
                <Alert variant="default" className="border-emerald-300 bg-emerald-50 text-emerald-900">
                  <CheckCircle2 className="h-4 w-4" />
                  <AlertDescription>
                    {outcome.createdCount} listing{outcome.createdCount === 1 ? "" : "s"} imported
                    as draft{outcome.createdCount === 1 ? "" : "s"}. Open each one to double-check
                    the details, then submit it for review.
                  </AlertDescription>
                </Alert>
              )}

              {outcome.partials.length > 0 && (
                <div className="space-y-2">
                  <p className="text-sm font-medium text-amber-700">Imported, with notes:</p>
                  <ul className="max-h-40 space-y-2 overflow-y-auto pr-1">
                    {outcome.partials.map((note) => (
                      <li
                        key={note.rowNumber}
                        className="rounded-md border border-amber-200 bg-amber-50 p-2 text-sm text-amber-900"
                      >
                        <p className="font-medium">
                          {config.rowNoun} {note.rowNumber} — {note.title}
                        </p>
                        <p>{note.message}</p>
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              {outcome.failures.length > 0 && (
                <div className="space-y-2">
                  <Alert variant="destructive">
                    <AlertTriangle className="h-4 w-4" />
                    <AlertDescription>
                      {outcome.failures.length} listing
                      {outcome.failures.length === 1 ? "" : "s"} could not be created.
                    </AlertDescription>
                  </Alert>
                  <ul className="max-h-40 space-y-2 overflow-y-auto pr-1">
                    {outcome.failures.map((failure) => (
                      <li
                        key={failure.rowNumber}
                        className="rounded-md border bg-muted/30 p-2 text-sm"
                      >
                        <p className="font-medium">
                          {config.rowNoun} {failure.rowNumber} — {failure.title}
                        </p>
                        <p className="text-muted-foreground">{failure.message}</p>
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => handleOpenChange(false)}>
                  Close
                </Button>
                <Button type="button" onClick={handleGoToListings}>
                  Review my listings
                </Button>
              </DialogFooter>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}

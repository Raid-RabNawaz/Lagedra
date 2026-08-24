import { useEffect, useMemo, useState } from "react";
import { adminApi } from "@/features/admin/services/adminApi";
import type {
  LeasePlaceholderCatalogDto,
  LeasePlaceholderDto,
  LeaseTemplateSummaryDto,
  LeaseTemplateVersionDetailsDto,
  LeaseTemplateVersionSummaryDto,
} from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { DatePicker } from "@/components/ui/date-picker";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Loader } from "@/components/shared/Loader";
import {
  LeaseRichTextEditor,
  insertTextAtCursor,
} from "@/features/admin/components/LeaseRichTextEditor";
import { BookOpen, Copy, Plus } from "lucide-react";

export const LeaseAgreementTemplatesPage = () => {
  const [templates, setTemplates] = useState<LeaseTemplateSummaryDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [versions, setVersions] = useState<LeaseTemplateVersionSummaryDto[]>([]);
  const [details, setDetails] = useState<LeaseTemplateVersionDetailsDto | null>(null);
  const [catalog, setCatalog] = useState<LeasePlaceholderCatalogDto | null>(null);
  const [bodyHtml, setBodyHtml] = useState("");
  const [title, setTitle] = useState("");
  const [effectiveDate, setEffectiveDate] = useState("");
  const [newCode, setNewCode] = useState("US-CA");
  const [newTitle, setNewTitle] = useState("California Residential Lease Agreement");
  const [groupFilter, setGroupFilter] = useState<string>("All");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const loadTemplates = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [list, placeholders] = await Promise.all([
        adminApi.listLeaseTemplates(),
        adminApi.getLeasePlaceholderCatalog(),
      ]);
      setTemplates(list);
      setCatalog(placeholders);
      if (!selectedId && list.length > 0) {
        setSelectedId(list[0].templateId);
      }
    } catch {
      setError("Failed to load lease templates.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadTemplates();
  }, []);

  useEffect(() => {
    if (!selectedId) return;
    void (async () => {
      try {
        const vs = await adminApi.listLeaseTemplateVersions(selectedId);
        setVersions(vs);
        const draftOrLatest =
          vs.find((v) => v.status === "Draft") ??
          vs.find((v) => v.status === "PendingApproval") ??
          vs[0];
        if (draftOrLatest) {
          const d = await adminApi.getLeaseTemplateVersionDetails(
            selectedId,
            draftOrLatest.versionId,
          );
          setDetails(d);
          setBodyHtml(d.bodyHtml);
          setTitle(d.title);
          setEffectiveDate(d.effectiveDate ? d.effectiveDate.slice(0, 10) : "");
        } else {
          setDetails(null);
          setBodyHtml("");
        }
      } catch {
        setError("Failed to load template versions.");
      }
    })();
  }, [selectedId]);

  const groups = useMemo(() => {
    const g = new Set(catalog?.placeholders.map((p) => p.group) ?? []);
    return ["All", ...Array.from(g)];
  }, [catalog]);

  const visiblePlaceholders = useMemo(() => {
    const all = catalog?.placeholders ?? [];
    return groupFilter === "All" ? all : all.filter((p) => p.group === groupFilter);
  }, [catalog, groupFilter]);

  const isDraft = details?.status === "Draft";

  const insertPlaceholder = (token: string) => {
    if (!isDraft) return;
    insertTextAtCursor(token);
    // sync after insert
    window.setTimeout(() => {
      const editable = document.querySelector<HTMLElement>(
        '[contenteditable="true"]',
      );
      if (editable) setBodyHtml(editable.innerHTML);
    }, 0);
  };

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const created = await adminApi.createLeaseTemplate(
        newCode.trim().toUpperCase(),
        newTitle.trim(),
      );
      await loadTemplates();
      setSelectedId(created.templateId);
    } catch {
      setError("Could not create template (code may already exist).");
    } finally {
      setBusy(false);
    }
  };

  const handleSave = async () => {
    if (!selectedId || !details) return;
    setBusy(true);
    setError(null);
    try {
      const updated = await adminApi.updateLeaseTemplateDraft(selectedId, details.versionId, {
        bodyHtml,
        title: title.trim() || null,
        effectiveDate: effectiveDate ? new Date(effectiveDate).toISOString() : null,
      });
      setDetails(updated);
      setBodyHtml(updated.bodyHtml);
    } catch (e: unknown) {
      const msg =
        typeof e === "object" && e && "response" in e
          ? String((e as { response?: { data?: { detail?: string } } }).response?.data?.detail)
          : null;
      setError(msg || "Save failed. Check placeholders are from the catalog.");
    } finally {
      setBusy(false);
    }
  };

  const runAction = async (action: () => Promise<void>) => {
    setBusy(true);
    setError(null);
    try {
      await action();
      if (selectedId) {
        const vs = await adminApi.listLeaseTemplateVersions(selectedId);
        setVersions(vs);
        if (details) {
          const d = await adminApi.getLeaseTemplateVersionDetails(
            selectedId,
            details.versionId,
          );
          setDetails(d);
        }
      }
      await loadTemplates();
    } catch {
      setError("Action failed.");
    } finally {
      setBusy(false);
    }
  };

  if (isLoading) return <Loader label="Loading lease templates..." />;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Lease Agreement Templates</h1>
        <p className="mt-1 text-muted-foreground">
          Versioned jurisdiction lease templates with merge variables. Published templates are
          filled per booking, sealed with the deal, and emailed as PDF on activation.
        </p>
      </div>

      {error && (
        <p className="rounded-md border border-destructive/40 bg-destructive/5 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}

      <div className="grid gap-6 xl:grid-cols-[280px_1fr_300px]">
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base">Templates</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-2 rounded-md border p-3">
              <Input
                value={newCode}
                onChange={(e) => setNewCode(e.target.value)}
                placeholder="US-CA"
              />
              <Input
                value={newTitle}
                onChange={(e) => setNewTitle(e.target.value)}
                placeholder="Template title"
              />
              <Button size="sm" className="w-full" disabled={busy} onClick={() => void handleCreate()}>
                <Plus className="mr-1 h-4 w-4" /> Create
              </Button>
            </div>
            <div className="space-y-1">
              {templates.map((t) => (
                <button
                  key={t.templateId}
                  type="button"
                  onClick={() => setSelectedId(t.templateId)}
                  className={`flex w-full items-start gap-2 rounded-md px-2 py-2 text-left text-sm hover:bg-muted ${
                    selectedId === t.templateId ? "bg-muted" : ""
                  }`}
                >
                  <BookOpen className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
                  <span>
                    <span className="font-medium">{t.jurisdictionCode}</span>
                    <span className="block text-xs text-muted-foreground">{t.title}</span>
                  </span>
                </button>
              ))}
              {templates.length === 0 && (
                <p className="text-sm text-muted-foreground">No templates yet.</p>
              )}
            </div>
          </CardContent>
        </Card>

        <div className="space-y-4">
          {details ? (
            <>
              <Card>
                <CardHeader className="flex flex-row flex-wrap items-center justify-between gap-2 pb-3">
                  <div>
                    <CardTitle className="text-base">
                      {details.jurisdictionCode} · v{details.versionNumber}
                    </CardTitle>
                    <div className="mt-1 flex flex-wrap gap-2">
                      <Badge variant="outline">{details.status}</Badge>
                      {versions.map((v) => (
                        <button
                          key={v.versionId}
                          type="button"
                          className="text-xs text-muted-foreground underline-offset-2 hover:underline"
                          onClick={() => {
                            void adminApi
                              .getLeaseTemplateVersionDetails(v.templateId, v.versionId)
                              .then((d) => {
                                setDetails(d);
                                setBodyHtml(d.bodyHtml);
                                setTitle(d.title);
                                setEffectiveDate(
                                  d.effectiveDate ? d.effectiveDate.slice(0, 10) : "",
                                );
                              });
                          }}
                        >
                          v{v.versionNumber} ({v.status})
                        </button>
                      ))}
                    </div>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {isDraft && (
                      <>
                        <Button size="sm" disabled={busy} onClick={() => void handleSave()}>
                          Save draft
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          disabled={busy}
                          onClick={() =>
                            void runAction(() =>
                              adminApi.requestLeaseApproval(
                                details.templateId,
                                details.versionId,
                              ),
                            )
                          }
                        >
                          Request approval
                        </Button>
                      </>
                    )}
                    {details.status === "PendingApproval" && (
                      <Button
                        size="sm"
                        disabled={busy}
                        onClick={() =>
                          void runAction(() =>
                            adminApi.approveLeaseVersion(details.templateId, details.versionId),
                          )
                        }
                      >
                        Approve
                      </Button>
                    )}
                    {details.status === "Active" && (
                      <Button
                        size="sm"
                        disabled={busy}
                        onClick={() =>
                          void runAction(() =>
                            adminApi.publishLeaseVersion(details.templateId, details.versionId),
                          )
                        }
                      >
                        Publish
                      </Button>
                    )}
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={busy}
                      onClick={() =>
                        void runAction(async () => {
                          await adminApi.addLeaseTemplateVersion(details.templateId);
                        })
                      }
                    >
                      New version
                    </Button>
                  </div>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div className="grid gap-3 sm:grid-cols-2">
                    <div>
                      <label className="mb-1 block text-xs text-muted-foreground">Title</label>
                      <Input
                        value={title}
                        disabled={!isDraft}
                        onChange={(e) => setTitle(e.target.value)}
                      />
                    </div>
                    <div>
                      <label className="mb-1 block text-xs text-muted-foreground">
                        Effective date
                      </label>
                      <DatePicker
                        value={effectiveDate}
                        disabled={!isDraft}
                        onChange={setEffectiveDate}
                        aria-label="Effective date"
                      />
                    </div>
                  </div>
                  <LeaseRichTextEditor
                    value={bodyHtml}
                    onChange={setBodyHtml}
                    disabled={!isDraft}
                  />
                </CardContent>
              </Card>
            </>
          ) : (
            <Card>
              <CardContent className="py-12 text-center text-muted-foreground">
                Select or create a template to edit.
              </CardContent>
            </Card>
          )}
        </div>

        <Card className="h-fit xl:sticky xl:top-4">
          <CardHeader className="pb-2">
            <CardTitle className="text-base">Available variables</CardTitle>
            <p className="text-xs text-muted-foreground">
              {catalog?.usageHint ??
                "Click a variable to insert it. Use double curly braces, e.g. {{tenant.fullName}}."}
            </p>
          </CardHeader>
          <CardContent className="space-y-3">
            {catalog && (
              <div className="rounded-md border bg-muted/40 p-3 text-xs">
                <div className="mb-1 font-medium">Example</div>
                <div
                  className="prose prose-xs max-w-none text-muted-foreground"
                  dangerouslySetInnerHTML={{ __html: catalog.usageExampleHtml }}
                />
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  className="mt-2"
                  disabled={!isDraft}
                  onClick={() => insertPlaceholder("{{tenant.fullName}}")}
                >
                  <Copy className="mr-1 h-3.5 w-3.5" /> Insert sample token
                </Button>
              </div>
            )}
            <div className="flex flex-wrap gap-1">
              {groups.map((g) => (
                <Button
                  key={g}
                  type="button"
                  size="sm"
                  variant={groupFilter === g ? "default" : "outline"}
                  className="h-7 text-xs"
                  onClick={() => setGroupFilter(g)}
                >
                  {g}
                </Button>
              ))}
            </div>
            <div className="max-h-[480px] space-y-2 overflow-y-auto pr-1">
              {visiblePlaceholders.map((p) => (
                <PlaceholderRow
                  key={p.key}
                  placeholder={p}
                  disabled={!isDraft}
                  onInsert={() => insertPlaceholder(p.token)}
                />
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

function PlaceholderRow({
  placeholder,
  onInsert,
  disabled,
}: {
  placeholder: LeasePlaceholderDto;
  onInsert: () => void;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onInsert}
      className="w-full rounded-md border px-2 py-2 text-left hover:bg-muted disabled:opacity-50"
    >
      <div className="flex items-center justify-between gap-2">
        <code className="text-xs font-semibold">{placeholder.token}</code>
        {placeholder.required && (
          <Badge variant="secondary" className="text-[10px]">
            required
          </Badge>
        )}
      </div>
      <div className="mt-0.5 text-xs font-medium">{placeholder.label}</div>
      <div className="text-[11px] text-muted-foreground">{placeholder.description}</div>
      <div className="mt-1 text-[11px] text-muted-foreground">
        e.g. {placeholder.example}
      </div>
    </button>
  );
}

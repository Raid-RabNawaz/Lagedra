import { useCallback, useEffect, useState } from "react";
import { adminApi } from "@/features/admin/services/adminApi";
import { useAuthStore } from "@/app/auth/authStore";
import { roles } from "@/app/auth/roles";
import type {
  JurisdictionPackSummaryDto,
  PackVersionSummaryDto,
  PackVersionDetailDto,
  DepositCapRuleDto,
  FieldGatingRuleDto,
  EvidenceScheduleDto,
  EffectiveDateRuleDto,
  UpdatePackDraftBody,
} from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";

const statusBadgeVariant = (s: PackVersionSummaryDto["status"]) => {
  switch (s) {
    case "Draft": return "secondary" as const;
    case "PendingApproval": return "accent" as const;
    case "Active": return "success" as const;
    case "Deprecated": return "destructive" as const;
  }
};

type DepositCapInput = Omit<DepositCapRuleDto, "id">;
type FieldGatingInput = Omit<FieldGatingRuleDto, "id">;
type EvidenceScheduleInput = Omit<EvidenceScheduleDto, "id">;
type EffectiveDateRuleInput = Omit<EffectiveDateRuleDto, "id">;

export const JurisdictionPackVersionsPage = () => {
  const user = useAuthStore((s) => s.user);
  const isAdmin = String(user?.role) === roles.platformAdmin;
  const [packs, setPacks] = useState<JurisdictionPackSummaryDto[]>([]);
  const [selectedPackId, setSelectedPackId] = useState("");
  const [versions, setVersions] = useState<PackVersionSummaryDto[]>([]);
  const [versionDetail, setVersionDetail] = useState<PackVersionDetailDto | null>(null);
  const [newCode, setNewCode] = useState("US-CA-LA");
  const [draftEffectiveDate, setDraftEffectiveDate] = useState("");
  const [editingVersionId, setEditingVersionId] = useState<string | null>(null);
  const [viewingVersionId, setViewingVersionId] = useState<string | null>(null);
  const [isLoadingPacks, setIsLoadingPacks] = useState(true);
  const [isLoadingVersions, setIsLoadingVersions] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [actionInFlight, setActionInFlight] = useState<string | null>(null);

  // Rule editing state
  const [depositCaps, setDepositCaps] = useState<DepositCapInput[]>([]);
  const [fieldGating, setFieldGating] = useState<FieldGatingInput[]>([]);
  const [evidenceSchedules, setEvidenceSchedules] = useState<EvidenceScheduleInput[]>([]);
  const [effectiveDateRules, setEffectiveDateRules] = useState<EffectiveDateRuleInput[]>([]);

  const loadPacks = useCallback(async () => {
    setIsLoadingPacks(true);
    try {
      const data = await adminApi.listJurisdictionPacks();
      setPacks(data);
      if (data.length > 0 && !selectedPackId) {
        setSelectedPackId(data[0].packId);
      }
    } catch {
      setError("Failed to load jurisdiction packs.");
    } finally {
      setIsLoadingPacks(false);
    }
  }, [selectedPackId]);

  const loadVersions = useCallback(async (packId: string) => {
    if (!packId) return;
    setIsLoadingVersions(true);
    setError(null);
    try {
      const data = await adminApi.listPackVersions(packId);
      setVersions(data);
    } catch {
      setError("Failed to load pack versions.");
    } finally {
      setIsLoadingVersions(false);
    }
  }, []);

  const loadVersionDetail = useCallback(async (packId: string, versionId: string) => {
    try {
      const detail = await adminApi.getPackVersionDetails(packId, versionId);
      setVersionDetail(detail);
      setDepositCaps(detail.depositCapRules.map(({ id, ...rest }) => {
        void id;
        return rest;
      }));
      setFieldGating(detail.fieldGatingRules.map(({ id, ...rest }) => {
        void id;
        return rest;
      }));
      setEvidenceSchedules(detail.evidenceSchedules.map(({ id, ...rest }) => {
        void id;
        return rest;
      }));
      setEffectiveDateRules(detail.effectiveDateRules.map(({ id, ...rest }) => {
        void id;
        return rest;
      }));
    } catch {
      setError("Failed to load version details.");
    }
  }, []);

  useEffect(() => {
    void loadPacks();
  }, [loadPacks]);

  useEffect(() => {
    if (selectedPackId) void loadVersions(selectedPackId);
  }, [selectedPackId, loadVersions]);

  const handleAction = async (
    versionId: string,
    action: (pId: string, vId: string) => Promise<void>,
  ) => {
    setActionInFlight(versionId);
    try {
      await action(selectedPackId, versionId);
      await loadVersions(selectedPackId);
    } catch {
      setError("Action failed. Please try again.");
    } finally {
      setActionInFlight(null);
    }
  };

  const handleCreatePack = async () => {
    if (!newCode.trim()) return;
    setActionInFlight("create");
    try {
      const created = await adminApi.createJurisdictionPack(newCode.trim().toUpperCase());
      await loadPacks();
      setSelectedPackId(created.packId);
    } catch {
      setError("Could not create pack. It may already exist.");
    } finally {
      setActionInFlight(null);
    }
  };

  const handleSaveDraft = async (versionId: string) => {
    setActionInFlight(versionId);
    setError(null);
    try {
      const body: UpdatePackDraftBody = {};

      if (draftEffectiveDate) {
        body.effectiveDate = new Date(draftEffectiveDate).toISOString();
      }
      if (depositCaps.length > 0) {
        body.depositCapRules = depositCaps;
      }
      if (fieldGating.length > 0) {
        body.fieldGatingRules = fieldGating;
      }
      if (evidenceSchedules.length > 0) {
        body.evidenceSchedules = evidenceSchedules;
      }
      if (effectiveDateRules.length > 0) {
        body.effectiveDateRules = effectiveDateRules;
      }

      await adminApi.updatePackDraft(selectedPackId, versionId, body);
      setEditingVersionId(null);
      await loadVersions(selectedPackId);
    } catch {
      setError("Failed to update draft.");
    } finally {
      setActionInFlight(null);
    }
  };

  const handleApprove = async (versionId: string) => {
    if (!user?.userId) return;
    setActionInFlight(versionId);
    try {
      await adminApi.approveVersion(selectedPackId, versionId, user.userId);
      await loadVersions(selectedPackId);
    } catch {
      setError("Approval failed.");
    } finally {
      setActionInFlight(null);
    }
  };

  const startEditing = (v: PackVersionSummaryDto) => {
    setEditingVersionId(v.versionId);
    setViewingVersionId(null);
    setDraftEffectiveDate(
      v.effectiveDate
        ? new Date(v.effectiveDate).toISOString().slice(0, 10)
        : "",
    );
    void loadVersionDetail(selectedPackId, v.versionId);
  };

  const viewVersion = (v: PackVersionSummaryDto) => {
    setViewingVersionId(v.versionId);
    setEditingVersionId(null);
    void loadVersionDetail(selectedPackId, v.versionId);
  };

  // Deposit cap helpers
  const addDepositCap = () => {
    setDepositCaps([...depositCaps, {
      jurisdictionCode: "",
      maxMultiplier: 1.0,
      exceptionCondition: null,
      exceptionMultiplier: null,
      legalReference: "",
    }]);
  };
  const updateDepositCap = (idx: number, field: keyof DepositCapInput, value: string | number | null) => {
    setDepositCaps(prev => prev.map((r, i) => i === idx ? { ...r, [field]: value } : r));
  };
  const removeDepositCap = (idx: number) => {
    setDepositCaps(prev => prev.filter((_, i) => i !== idx));
  };

  // Field gating helpers
  const addFieldGating = () => {
    setFieldGating([...fieldGating, {
      fieldName: "",
      gatingType: "Hard",
      value: "",
      condition: null,
    }]);
  };
  const updateFieldGating = (idx: number, field: keyof FieldGatingInput, value: string | null) => {
    setFieldGating(prev => prev.map((r, i) => i === idx ? { ...r, [field]: value } : r));
  };
  const removeFieldGating = (idx: number) => {
    setFieldGating(prev => prev.filter((_, i) => i !== idx));
  };

  // Evidence schedule helpers
  const addEvidenceSchedule = () => {
    setEvidenceSchedules([...evidenceSchedules, { category: "", minimumRequirements: "" }]);
  };
  const updateEvidenceSchedule = (idx: number, field: keyof EvidenceScheduleInput, value: string) => {
    setEvidenceSchedules(prev => prev.map((r, i) => i === idx ? { ...r, [field]: value } : r));
  };
  const removeEvidenceSchedule = (idx: number) => {
    setEvidenceSchedules(prev => prev.filter((_, i) => i !== idx));
  };

  // Effective date rule helpers
  const addEffectiveDateRule = () => {
    setEffectiveDateRules([...effectiveDateRules, { fieldName: "", effectiveDate: "" }]);
  };
  const updateEffectiveDateRule = (idx: number, field: keyof EffectiveDateRuleInput, value: string) => {
    setEffectiveDateRules(prev => prev.map((r, i) => i === idx ? { ...r, [field]: value } : r));
  };
  const removeEffectiveDateRule = (idx: number) => {
    setEffectiveDateRules(prev => prev.filter((_, i) => i !== idx));
  };

  const selectedPack = packs.find((p) => p.packId === selectedPackId);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Jurisdiction Packs</h1>
        <p className="mt-1 text-muted-foreground">
          Manage pack lifecycle: draft, dual-control approval, publish, deprecate.
        </p>
      </div>

      <div className={`grid gap-4 ${isAdmin ? "lg:grid-cols-2" : ""}`}>
        {isAdmin && (
          <Card>
            <CardHeader className="pb-4">
              <CardTitle className="text-lg">Create pack</CardTitle>
            </CardHeader>
            <CardContent className="flex gap-3">
              <Input
                placeholder="US-CA-LA"
                value={newCode}
                onChange={(e) => setNewCode(e.target.value)}
                className="max-w-xs"
              />
              <Button
                onClick={() => void handleCreatePack()}
                disabled={!newCode.trim() || actionInFlight === "create"}
              >
                Create draft
              </Button>
            </CardContent>
          </Card>
        )}

        <Card>
          <CardHeader className="pb-4">
            <CardTitle className="text-lg">Select pack</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoadingPacks ? (
              <Loader label="Loading packs..." />
            ) : packs.length === 0 ? (
              <p className="text-sm text-muted-foreground">No packs yet.</p>
            ) : (
              <Select
                value={selectedPackId}
                onChange={(e) => setSelectedPackId(e.target.value)}
              >
                {packs.map((p) => (
                  <option key={p.packId} value={p.packId}>
                    {p.jurisdictionCode} ({p.versionCount} version{p.versionCount !== 1 ? "s" : ""})
                  </option>
                ))}
              </Select>
            )}
          </CardContent>
        </Card>
      </div>

      {error && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
          {error}
          <Button variant="ghost" size="sm" className="ml-2" onClick={() => setError(null)}>
            Dismiss
          </Button>
        </div>
      )}

      {selectedPack && (
        <Card>
          <CardHeader className="pb-4">
            <CardTitle className="text-lg">
              Versions — {selectedPack.jurisdictionCode}
            </CardTitle>
          </CardHeader>
          <CardContent>
            {isLoadingVersions ? (
              <Loader label="Loading versions..." />
            ) : versions.length === 0 ? (
              <p className="py-8 text-center text-muted-foreground">No versions found.</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Version</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Effective</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {versions.map((v) => (
                    <TableRow key={v.versionId}>
                      <TableCell className="font-medium">v{v.versionNumber}</TableCell>
                      <TableCell>
                        <Badge variant={statusBadgeVariant(v.status)}>{v.status}</Badge>
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {v.effectiveDate ? new Date(v.effectiveDate).toLocaleDateString() : "—"}
                      </TableCell>
                      <TableCell className="text-right space-x-2">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => viewVersion(v)}
                        >
                          View
                        </Button>
                        {v.status === "Draft" && isAdmin && (
                          <>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => startEditing(v)}
                            >
                              Edit
                            </Button>
                            <Button
                              variant="outline"
                              size="sm"
                              disabled={actionInFlight === v.versionId}
                              onClick={() =>
                                handleAction(v.versionId, adminApi.requestApproval)
                              }
                            >
                              Request approval
                            </Button>
                          </>
                        )}
                        {v.status === "PendingApproval" && (
                          <Button
                            variant="outline"
                            size="sm"
                            disabled={actionInFlight === v.versionId || v.approvedBy === user?.userId}
                            onClick={() => void handleApprove(v.versionId)}
                          >
                            Approve
                          </Button>
                        )}
                        {v.status === "Active" && isAdmin && (
                          <>
                            <Button
                              variant="outline"
                              size="sm"
                              disabled={actionInFlight === v.versionId}
                              onClick={() =>
                                handleAction(v.versionId, adminApi.publishVersion)
                              }
                            >
                              Publish
                            </Button>
                            <Button
                              variant="outline"
                              size="sm"
                              disabled={actionInFlight === v.versionId}
                              onClick={() =>
                                handleAction(v.versionId, adminApi.deprecateVersion)
                              }
                            >
                              Deprecate
                            </Button>
                          </>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      )}

      {/* Draft Editor Panel (admin only) */}
      {isAdmin && editingVersionId && versionDetail && (
        <Card>
          <CardHeader className="pb-4">
            <CardTitle className="text-lg">
              Edit Draft — v{versionDetail.versionNumber}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-6">
            {/* Effective Date */}
            <div>
              <label className="text-sm font-medium">Effective Date</label>
              <Input
                type="date"
                value={draftEffectiveDate}
                onChange={(e) => setDraftEffectiveDate(e.target.value)}
                className="mt-1 max-w-xs"
              />
            </div>

            {/* Deposit Cap Rules */}
            <div>
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-medium">Deposit Cap Rules</h3>
                <Button variant="outline" size="sm" onClick={addDepositCap}>
                  Add rule
                </Button>
              </div>
              {depositCaps.length === 0 ? (
                <p className="mt-2 text-sm text-muted-foreground">No deposit cap rules.</p>
              ) : (
                <div className="mt-2 space-y-3">
                  {depositCaps.map((rule, idx) => (
                    <div key={idx} className="flex flex-wrap items-end gap-2 rounded border p-3">
                      <div className="flex-1 min-w-[120px]">
                        <label className="text-xs text-muted-foreground">Jurisdiction</label>
                        <Input
                          value={rule.jurisdictionCode}
                          onChange={(e) => updateDepositCap(idx, "jurisdictionCode", e.target.value)}
                          placeholder="US-CA"
                        />
                      </div>
                      <div className="w-24">
                        <label className="text-xs text-muted-foreground">Max ×</label>
                        <Input
                          type="number"
                          step="0.1"
                          value={rule.maxMultiplier}
                          onChange={(e) => updateDepositCap(idx, "maxMultiplier", parseFloat(e.target.value) || 0)}
                        />
                      </div>
                      <div className="flex-1 min-w-[150px]">
                        <label className="text-xs text-muted-foreground">Legal Reference</label>
                        <Input
                          value={rule.legalReference}
                          onChange={(e) => updateDepositCap(idx, "legalReference", e.target.value)}
                          placeholder="CA Civil Code §1950.5"
                        />
                      </div>
                      <div className="flex-1 min-w-[120px]">
                        <label className="text-xs text-muted-foreground">Exception Condition</label>
                        <Input
                          value={rule.exceptionCondition ?? ""}
                          onChange={(e) => updateDepositCap(idx, "exceptionCondition", e.target.value || null)}
                          placeholder="furnished"
                        />
                      </div>
                      <div className="w-24">
                        <label className="text-xs text-muted-foreground">Exc. ×</label>
                        <Input
                          type="number"
                          step="0.1"
                          value={rule.exceptionMultiplier ?? ""}
                          onChange={(e) => updateDepositCap(idx, "exceptionMultiplier", e.target.value ? parseFloat(e.target.value) : null)}
                        />
                      </div>
                      <Button variant="ghost" size="sm" onClick={() => removeDepositCap(idx)}>
                        Remove
                      </Button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Field Gating Rules */}
            <div>
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-medium">Field Gating Rules</h3>
                <Button variant="outline" size="sm" onClick={addFieldGating}>
                  Add rule
                </Button>
              </div>
              {fieldGating.length === 0 ? (
                <p className="mt-2 text-sm text-muted-foreground">No field gating rules.</p>
              ) : (
                <div className="mt-2 space-y-3">
                  {fieldGating.map((rule, idx) => (
                    <div key={idx} className="flex flex-wrap items-end gap-2 rounded border p-3">
                      <div className="flex-1 min-w-[120px]">
                        <label className="text-xs text-muted-foreground">Field Name</label>
                        <Input
                          value={rule.fieldName}
                          onChange={(e) => updateFieldGating(idx, "fieldName", e.target.value)}
                          placeholder="DepositAmount"
                        />
                      </div>
                      <div className="w-24">
                        <label className="text-xs text-muted-foreground">Type</label>
                        <Select
                          value={rule.gatingType}
                          onChange={(e) => updateFieldGating(idx, "gatingType", e.target.value)}
                        >
                          <option value="Hard">Hard</option>
                          <option value="Soft">Soft</option>
                        </Select>
                      </div>
                      <div className="flex-1 min-w-[150px]">
                        <label className="text-xs text-muted-foreground">Value</label>
                        <Input
                          value={rule.value}
                          onChange={(e) => updateFieldGating(idx, "value", e.target.value)}
                          placeholder="must-not-exceed-jurisdiction-cap"
                        />
                      </div>
                      <div className="flex-1 min-w-[120px]">
                        <label className="text-xs text-muted-foreground">Condition</label>
                        <Input
                          value={rule.condition ?? ""}
                          onChange={(e) => updateFieldGating(idx, "condition", e.target.value || null)}
                          placeholder="Optional"
                        />
                      </div>
                      <Button variant="ghost" size="sm" onClick={() => removeFieldGating(idx)}>
                        Remove
                      </Button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Evidence Schedules */}
            <div>
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-medium">Evidence Schedules</h3>
                <Button variant="outline" size="sm" onClick={addEvidenceSchedule}>
                  Add schedule
                </Button>
              </div>
              {evidenceSchedules.length === 0 ? (
                <p className="mt-2 text-sm text-muted-foreground">No evidence schedules.</p>
              ) : (
                <div className="mt-2 space-y-3">
                  {evidenceSchedules.map((schedule, idx) => (
                    <div key={idx} className="flex flex-wrap items-end gap-2 rounded border p-3">
                      <div className="flex-1 min-w-[150px]">
                        <label className="text-xs text-muted-foreground">Category</label>
                        <Input
                          value={schedule.category}
                          onChange={(e) => updateEvidenceSchedule(idx, "category", e.target.value)}
                          placeholder="MoveInConditionReport"
                        />
                      </div>
                      <div className="flex-[2] min-w-[250px]">
                        <label className="text-xs text-muted-foreground">Minimum Requirements</label>
                        <Input
                          value={schedule.minimumRequirements}
                          onChange={(e) => updateEvidenceSchedule(idx, "minimumRequirements", e.target.value)}
                          placeholder="Timestamped photo/video walkthrough..."
                        />
                      </div>
                      <Button variant="ghost" size="sm" onClick={() => removeEvidenceSchedule(idx)}>
                        Remove
                      </Button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Effective Date Rules */}
            <div>
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-medium">Effective Date Rules</h3>
                <Button variant="outline" size="sm" onClick={addEffectiveDateRule}>
                  Add rule
                </Button>
              </div>
              {effectiveDateRules.length === 0 ? (
                <p className="mt-2 text-sm text-muted-foreground">No effective date rules.</p>
              ) : (
                <div className="mt-2 space-y-3">
                  {effectiveDateRules.map((rule, idx) => (
                    <div key={idx} className="flex flex-wrap items-end gap-2 rounded border p-3">
                      <div className="flex-1 min-w-[150px]">
                        <label className="text-xs text-muted-foreground">Field Name</label>
                        <Input
                          value={rule.fieldName}
                          onChange={(e) => updateEffectiveDateRule(idx, "fieldName", e.target.value)}
                          placeholder="DepositCapRule"
                        />
                      </div>
                      <div className="w-40">
                        <label className="text-xs text-muted-foreground">Effective Date</label>
                        <Input
                          type="date"
                          value={rule.effectiveDate ? rule.effectiveDate.slice(0, 10) : ""}
                          onChange={(e) => updateEffectiveDateRule(idx, "effectiveDate", e.target.value ? new Date(e.target.value).toISOString() : "")}
                        />
                      </div>
                      <Button variant="ghost" size="sm" onClick={() => removeEffectiveDateRule(idx)}>
                        Remove
                      </Button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Save / Cancel */}
            <div className="flex gap-3 pt-4 border-t">
              <Button
                onClick={() => void handleSaveDraft(editingVersionId)}
                disabled={actionInFlight === editingVersionId}
              >
                Save draft
              </Button>
              <Button
                variant="ghost"
                onClick={() => {
                  setEditingVersionId(null);
                  setVersionDetail(null);
                }}
              >
                Cancel
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Read-only Details Panel */}
      {viewingVersionId && versionDetail && !editingVersionId && (
        <Card>
          <CardHeader className="pb-4">
            <CardTitle className="text-lg">
              Version Details — v{versionDetail.versionNumber}
              <Badge variant={statusBadgeVariant(versionDetail.status as PackVersionSummaryDto["status"])} className="ml-2">
                {versionDetail.status}
              </Badge>
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
              <div>
                <span className="text-muted-foreground">Effective Date</span>
                <p className="font-medium mt-0.5">
                  {versionDetail.effectiveDate
                    ? new Date(versionDetail.effectiveDate).toLocaleDateString()
                    : "—"}
                </p>
              </div>
              <div>
                <span className="text-muted-foreground">Approved At</span>
                <p className="font-medium mt-0.5">
                  {versionDetail.approvedAt
                    ? new Date(versionDetail.approvedAt).toLocaleString()
                    : "—"}
                </p>
              </div>
              <div>
                <span className="text-muted-foreground">Approved By</span>
                <p className="font-mono text-xs mt-0.5">{versionDetail.approvedBy ?? "—"}</p>
              </div>
              <div>
                <span className="text-muted-foreground">Second Approver</span>
                <p className="font-mono text-xs mt-0.5">{versionDetail.secondApproverId ?? "—"}</p>
              </div>
            </div>

            {/* Deposit Cap Rules */}
            {versionDetail.depositCapRules.length > 0 && (
              <div>
                <h3 className="text-sm font-medium mb-2">Deposit Cap Rules</h3>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Jurisdiction</TableHead>
                      <TableHead>Max Multiplier</TableHead>
                      <TableHead>Legal Reference</TableHead>
                      <TableHead>Exception</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {versionDetail.depositCapRules.map((r) => (
                      <TableRow key={r.id}>
                        <TableCell className="font-mono text-xs">{r.jurisdictionCode}</TableCell>
                        <TableCell>{r.maxMultiplier}×</TableCell>
                        <TableCell className="text-xs">{r.legalReference}</TableCell>
                        <TableCell className="text-xs">
                          {r.exceptionCondition
                            ? `${r.exceptionCondition} → ${r.exceptionMultiplier}×`
                            : "—"}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}

            {/* Field Gating Rules */}
            {versionDetail.fieldGatingRules.length > 0 && (
              <div>
                <h3 className="text-sm font-medium mb-2">Field Gating Rules</h3>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Field</TableHead>
                      <TableHead>Type</TableHead>
                      <TableHead>Value</TableHead>
                      <TableHead>Condition</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {versionDetail.fieldGatingRules.map((r) => (
                      <TableRow key={r.id}>
                        <TableCell className="font-mono text-xs">{r.fieldName}</TableCell>
                        <TableCell>
                          <Badge variant={r.gatingType === "Hard" ? "destructive" : "secondary"}>
                            {r.gatingType}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-xs">{r.value}</TableCell>
                        <TableCell className="text-xs">{r.condition ?? "—"}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}

            {/* Evidence Schedules */}
            {versionDetail.evidenceSchedules.length > 0 && (
              <div>
                <h3 className="text-sm font-medium mb-2">Evidence Schedules</h3>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Category</TableHead>
                      <TableHead>Minimum Requirements</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {versionDetail.evidenceSchedules.map((s) => (
                      <TableRow key={s.id}>
                        <TableCell className="font-mono text-xs">{s.category}</TableCell>
                        <TableCell className="text-sm">{s.minimumRequirements}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}

            {/* Effective Date Rules */}
            {versionDetail.effectiveDateRules.length > 0 && (
              <div>
                <h3 className="text-sm font-medium mb-2">Effective Date Rules</h3>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Field</TableHead>
                      <TableHead>Effective Date</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {versionDetail.effectiveDateRules.map((r) => (
                      <TableRow key={r.id}>
                        <TableCell className="font-mono text-xs">{r.fieldName}</TableCell>
                        <TableCell>{new Date(r.effectiveDate).toLocaleDateString()}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}

            {versionDetail.depositCapRules.length === 0 &&
              versionDetail.fieldGatingRules.length === 0 &&
              versionDetail.evidenceSchedules.length === 0 &&
              versionDetail.effectiveDateRules.length === 0 && (
                <p className="text-sm text-muted-foreground py-4 text-center">
                  No rules configured for this version.
                </p>
              )}

            <div className="pt-4 border-t">
              <Button
                variant="ghost"
                onClick={() => {
                  setViewingVersionId(null);
                  setVersionDetail(null);
                }}
              >
                Close
              </Button>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

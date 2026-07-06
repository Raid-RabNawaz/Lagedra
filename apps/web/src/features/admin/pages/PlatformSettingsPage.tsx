import { useCallback, useEffect, useMemo, useState } from "react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { PlatformSettingDto } from "@/api/types";
import { ProtocolFeeReconciliationBanner } from "@/features/admin/components/ProtocolFeeReconciliationBanner";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Loader } from "@/components/shared/Loader";

type SettingKind = "money" | "percent" | "days" | "boolean" | "text";

const settingKind = (key: string, value: string): SettingKind => {
  if (key.endsWith("_cents")) return "money";
  if (key.endsWith("_bps")) return "percent";
  if (key.endsWith("_days")) return "days";
  if (value === "true" || value === "false") return "boolean";
  return "text";
};

// Friendly group labels keyed by the first dotted segment of the setting key.
const groupMeta: Record<string, { label: string; description: string; order: number }> = {
  protocol_fee: {
    label: "Protocol fee (paid by host)",
    description:
      "The recurring platform fee charged to the host per active deal. Pilot discount applies only while the pilot toggle is on.",
    order: 0,
  },
  arbitration_fee: {
    label: "Arbitration filing fee (paid by filer)",
    description: "Filing fee charged to the party that opens an arbitration case, per tier.",
    order: 1,
  },
  service_fee: {
    label: "Platform service fee (paid by tenant)",
    description:
      "Service fee added to the tenant's payment total, as a percentage of the first month's rent. Set to 0% to disable it.",
    order: 1.5,
  },
  stripe: {
    label: "Stripe Connect",
    description: "Stripe identifiers used when collecting the recurring host fee.",
    order: 2,
  },
  payment: {
    label: "Tenant payment timing",
    description: "Grace, reminder, and auto-cancel windows for tenant payments.",
    order: 3,
  },
  host_platform_payment: {
    label: "Host platform payment enforcement",
    description: "Reminder and suspension windows for overdue host platform fees.",
    order: 4,
  },
  cancellation: { label: "Cancellation", description: "", order: 5 },
  damage_claim: { label: "Damage claims", description: "", order: 6 },
  other: { label: "Other settings", description: "", order: 99 },
};

const groupKeyFor = (key: string): string => {
  const prefix = key.split(".")[0];
  return prefix in groupMeta ? prefix : "other";
};

// Explicit labels for keys whose humanised form is ambiguous.
const labelOverrides: Record<string, string> = {
  "service_fee.tenant_use_flat": "Fee type",
  "service_fee.tenant_bps": "Percentage rate",
  "service_fee.tenant_flat_cents": "Flat amount",
};

// Custom on/off labels for boolean settings (default is Enabled/Disabled).
const booleanLabelOverrides: Record<string, { on: string; off: string }> = {
  "service_fee.tenant_use_flat": { on: "Flat amount", off: "Percentage" },
};

// Humanise the trailing segment of a key into a label, e.g.
// "protocol_fee.monthly_cents" -> "Monthly (cents)" style becomes "Monthly".
const humaniseKey = (key: string): string => {
  const tail = key.split(".").slice(1).join(".") || key;
  return tail
    .replace(/_cents$/, "")
    .replace(/_bps$/, "")
    .replace(/_days$/, "")
    .replace(/_/g, " ")
    .replace(/\b\w/g, (c) => c.toUpperCase())
    .trim();
};

const bpsToPercent = (bps: string): string => {
  const n = Number(bps);
  if (!Number.isFinite(n)) return "";
  return String(n / 100);
};

const percentToBps = (percent: string): string => {
  const n = Number(percent);
  if (!Number.isFinite(n)) return "0";
  return String(Math.round(n * 100));
};

const centsToDollars = (cents: string): string => {
  const n = Number(cents);
  if (!Number.isFinite(n)) return "";
  return (n / 100).toFixed(2);
};

const dollarsToCents = (dollars: string): string => {
  const n = Number(dollars);
  if (!Number.isFinite(n)) return "0";
  return String(Math.round(n * 100));
};

export const PlatformSettingsPage = () => {
  const [settings, setSettings] = useState<PlatformSettingDto[]>([]);
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [savingKey, setSavingKey] = useState<string | null>(null);
  const [savedKey, setSavedKey] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.listPlatformSettings();
      setSettings(data);
      setDrafts(Object.fromEntries(data.map((s) => [s.key, s.value])));
    } catch {
      setError("Failed to load platform settings.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const grouped = useMemo(() => {
    const map = new Map<string, PlatformSettingDto[]>();
    for (const s of [...settings].sort((a, b) => a.key.localeCompare(b.key))) {
      const g = groupKeyFor(s.key);
      if (!map.has(g)) map.set(g, []);
      map.get(g)!.push(s);
    }
    return [...map.entries()].sort(
      (a, b) => (groupMeta[a[0]]?.order ?? 50) - (groupMeta[b[0]]?.order ?? 50),
    );
  }, [settings]);

  const setDraft = (key: string, value: string) => {
    setDrafts((prev) => ({ ...prev, [key]: value }));
    if (savedKey === key) setSavedKey(null);
  };

  const handleSave = async (setting: PlatformSettingDto) => {
    const next = drafts[setting.key];
    if (next === undefined || next === setting.value) return;
    setSavingKey(setting.key);
    setError(null);
    try {
      await adminApi.updatePlatformSetting(setting.key, { value: next });
      setSavedKey(setting.key);
      await load();
    } catch {
      setError(`Failed to update "${setting.key}".`);
    } finally {
      setSavingKey(null);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Platform Fees & Settings</h1>
        <p className="mt-1 text-muted-foreground">
          Adjust protocol, arbitration, and other platform-wide settings. Changes apply to new
          activity within a few minutes and do not retroactively alter existing deals or cases.
        </p>
      </div>

      <ProtocolFeeReconciliationBanner />

      {error && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
          {error}
          <Button variant="ghost" size="sm" className="ml-2" onClick={() => setError(null)}>
            Dismiss
          </Button>
        </div>
      )}

      {isLoading ? (
        <Loader label="Loading settings..." />
      ) : settings.length === 0 ? (
        <p className="py-8 text-center text-muted-foreground">No platform settings found.</p>
      ) : (
        grouped.map(([groupKey, items]) => {
          const meta = groupMeta[groupKey] ?? groupMeta.other;
          return (
            <Card key={groupKey}>
              <CardHeader className="pb-4">
                <CardTitle className="text-lg">{meta.label}</CardTitle>
                {meta.description && (
                  <p className="text-sm text-muted-foreground">{meta.description}</p>
                )}
              </CardHeader>
              <CardContent className="space-y-4">
                {items.map((setting) => (
                  <SettingRow
                    key={setting.key}
                    setting={setting}
                    draft={drafts[setting.key] ?? setting.value}
                    onChange={(v) => setDraft(setting.key, v)}
                    onSave={() => void handleSave(setting)}
                    saving={savingKey === setting.key}
                    saved={savedKey === setting.key}
                  />
                ))}
              </CardContent>
            </Card>
          );
        })
      )}
    </div>
  );
};

function SettingRow({
  setting,
  draft,
  onChange,
  onSave,
  saving,
  saved,
}: {
  setting: PlatformSettingDto;
  draft: string;
  onChange: (value: string) => void;
  onSave: () => void;
  saving: boolean;
  saved: boolean;
}) {
  const kind = settingKind(setting.key, setting.value);
  const dirty = draft !== setting.value;
  const label = labelOverrides[setting.key] ?? humaniseKey(setting.key);
  const boolLabels = booleanLabelOverrides[setting.key];

  return (
    <div className="flex flex-wrap items-end gap-3 rounded-lg border p-3">
      <div className="flex-1 min-w-[200px]">
        <label className="text-sm font-medium">{label}</label>
        <p className="mt-0.5 font-mono text-[11px] text-muted-foreground">{setting.key}</p>
        {setting.description && (
          <p className="mt-1 text-xs text-muted-foreground">{setting.description}</p>
        )}
      </div>

      <div className="w-40">
        {kind === "boolean" ? (
          <Select value={draft} onChange={(e) => onChange(e.target.value)}>
            <option value="true">{boolLabels?.on ?? "Enabled"}</option>
            <option value="false">{boolLabels?.off ?? "Disabled"}</option>
          </Select>
        ) : kind === "money" ? (
          <div className="flex items-center gap-1">
            <span className="text-sm text-muted-foreground">$</span>
            <Input
              type="number"
              step="0.01"
              min="0"
              value={centsToDollars(draft)}
              onChange={(e) => onChange(dollarsToCents(e.target.value))}
            />
          </div>
        ) : kind === "percent" ? (
          <div className="flex items-center gap-1">
            <Input
              type="number"
              step="0.01"
              min="0"
              value={bpsToPercent(draft)}
              onChange={(e) => onChange(percentToBps(e.target.value))}
            />
            <span className="text-sm text-muted-foreground">%</span>
          </div>
        ) : kind === "days" ? (
          <div className="flex items-center gap-1">
            <Input
              type="number"
              min="0"
              value={draft}
              onChange={(e) => onChange(e.target.value)}
            />
            <span className="text-sm text-muted-foreground">days</span>
          </div>
        ) : (
          <Input value={draft} onChange={(e) => onChange(e.target.value)} />
        )}
      </div>

      <div className="flex items-center gap-2">
        <Button size="sm" onClick={onSave} disabled={!dirty || saving}>
          {saving ? "Saving..." : "Save"}
        </Button>
        {saved && !dirty && <span className="text-xs text-success">Saved</span>}
      </div>
    </div>
  );
}

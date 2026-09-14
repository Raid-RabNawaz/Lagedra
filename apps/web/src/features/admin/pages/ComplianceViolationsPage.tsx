import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { adminApi } from "@/features/admin/services/adminApi";
import type { ViolationDto, ViolationCategory, ViolationStatus } from "@/api/types";
import { getApiErrorMessage } from "@/api/errors";
import { violationActions } from "@/features/admin/lib/adminQueueActions";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Select } from "@/components/ui/select";
import { Alert } from "@/components/ui/alert";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Loader } from "@/components/shared/Loader";
import { AdminDetailList } from "@/features/admin/components/AdminDetailList";

const statusVariant = (status: ViolationStatus) => {
  switch (status) {
    case "Resolved":
      return "success" as const;
    case "Dismissed":
      return "secondary" as const;
    case "Escalated":
      return "destructive" as const;
    case "UnderReview":
      return "default" as const;
    default:
      return "outline" as const;
  }
};

const statusClassName = (status: ViolationStatus) =>
  status === "Open" ? "bg-yellow-100 text-yellow-800 border-yellow-300" : undefined;

const allStatuses: ViolationStatus[] = ["Open", "UnderReview", "Resolved", "Dismissed", "Escalated"];
const allCategories: ViolationCategory[] = [
  "NonPayment",
  "UnauthorizedOccupants",
  "PropertyDamage",
  "RuleViolation",
  "InsuranceLapse",
  "EarlyTermination",
  "Other",
];

export const ComplianceViolationsPage = () => {
  const [violations, setViolations] = useState<ViolationDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<"All" | ViolationStatus>("All");
  const [categoryFilter, setCategoryFilter] = useState<"All" | ViolationCategory>("All");
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [selected, setSelected] = useState<ViolationDto | null>(null);

  const loadViolations = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getAllViolations();
      setViolations(data);
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to load violations."));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadViolations();
  }, []);

  const filtered = violations.filter((v) => {
    if (statusFilter !== "All" && v.status !== statusFilter) return false;
    if (categoryFilter !== "All" && v.category !== categoryFilter) return false;
    return true;
  });

  const handleAction = async (id: string, action: "resolve" | "dismiss" | "escalate") => {
    setActionLoading(id);
    setError(null);
    try {
      if (action === "resolve") await adminApi.resolveViolation(id);
      else if (action === "dismiss") await adminApi.dismissViolation(id);
      else await adminApi.escalateViolation(id);
      setSelected(null);
      await loadViolations();
    } catch (err) {
      setError(getApiErrorMessage(err, `Failed to ${action} violation.`));
    } finally {
      setActionLoading(null);
    }
  };

  const selectedActions = selected ? violationActions(selected.status) : null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Compliance Violations</h1>
        <p className="mt-1 text-muted-foreground">
          All violations across deals. Open a row for the full record and only
          the actions that are valid for its current status.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <CardTitle className="text-lg">Violations</CardTitle>
            <Select
              className="w-full sm:w-52"
              value={categoryFilter}
              onChange={(e) => setCategoryFilter(e.target.value as "All" | ViolationCategory)}
            >
              <option value="All">All Categories</option>
              {allCategories.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </Select>
          </div>
        </CardHeader>
        <CardContent>
          <Tabs value={statusFilter} onValueChange={(v) => setStatusFilter(v as "All" | ViolationStatus)}>
            <TabsList className="mb-4 flex-wrap">
              <TabsTrigger value="All">All</TabsTrigger>
              {allStatuses.map((s) => (
                <TabsTrigger key={s} value={s}>
                  {s}
                </TabsTrigger>
              ))}
            </TabsList>

            {["All", ...allStatuses].map((tab) => (
              <TabsContent key={tab} value={tab}>
                {isLoading ? (
                  <Loader label="Loading violations..." />
                ) : error && !selected ? (
                  <p className="py-8 text-center text-destructive">{error}</p>
                ) : filtered.length === 0 ? (
                  <p className="py-8 text-center text-muted-foreground">No violations found.</p>
                ) : (
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Listing / deal</TableHead>
                        <TableHead>Target</TableHead>
                        <TableHead>Category</TableHead>
                        <TableHead>Status</TableHead>
                        <TableHead className="hidden lg:table-cell">Description</TableHead>
                        <TableHead>Detected</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {filtered.map((v) => (
                        <TableRow
                          key={v.id}
                          className="cursor-pointer"
                          onClick={() => setSelected(v)}
                        >
                          <TableCell>
                            <p className="font-medium">{v.listingTitle || "Deal"}</p>
                            <p className="font-mono text-xs text-muted-foreground">{v.dealId.slice(0, 12)}…</p>
                          </TableCell>
                          <TableCell>
                            <p>{v.targetName || v.targetEmail || v.targetUserId.slice(0, 8) + "…"}</p>
                            {v.targetEmail && v.targetName && (
                              <p className="text-xs text-muted-foreground">{v.targetEmail}</p>
                            )}
                          </TableCell>
                          <TableCell>
                            <Badge variant="outline">{v.category}</Badge>
                          </TableCell>
                          <TableCell>
                            <Badge variant={statusVariant(v.status)} className={statusClassName(v.status)}>
                              {v.status}
                            </Badge>
                          </TableCell>
                          <TableCell className="hidden max-w-[200px] truncate text-sm text-muted-foreground lg:table-cell">
                            {v.description}
                          </TableCell>
                          <TableCell className="text-sm text-muted-foreground">
                            {new Date(v.detectedAt).toLocaleDateString()}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </TabsContent>
            ))}
          </Tabs>
        </CardContent>
      </Card>

      <Dialog open={selected !== null} onOpenChange={(open) => !open && setSelected(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Violation details</DialogTitle>
            <DialogDescription>
              Resolve or dismiss after review. Escalate only while the item is
              still open.
            </DialogDescription>
          </DialogHeader>
          {selected && selectedActions && (
            <div className="space-y-4">
              {error && <Alert variant="destructive">{error}</Alert>}
              <AdminDetailList
                rows={[
                  { label: "Listing", value: selected.listingTitle },
                  {
                    label: "Deal",
                    value: (
                      <Link className="underline" to={`/app/deals/${selected.dealId}`}>
                        {selected.dealId}
                      </Link>
                    ),
                  },
                  { label: "Target", value: selected.targetName },
                  { label: "Target email", value: selected.targetEmail },
                  { label: "Reported by", value: selected.reportedByName || selected.reportedByEmail },
                  { label: "Category", value: selected.category },
                  { label: "Status", value: selected.status },
                  { label: "Description", value: selected.description },
                  { label: "Evidence", value: selected.evidenceReference },
                  { label: "Detected", value: new Date(selected.detectedAt).toLocaleString() },
                  {
                    label: "Resolved",
                    value: selected.resolvedAt
                      ? new Date(selected.resolvedAt).toLocaleString()
                      : "—",
                  },
                ]}
              />
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setSelected(null)}>
              Close
            </Button>
            {selected && selectedActions?.canDismiss && (
              <Button
                variant="ghost"
                disabled={actionLoading === selected.id}
                onClick={() => void handleAction(selected.id, "dismiss")}
              >
                Dismiss
              </Button>
            )}
            {selected && selectedActions?.canEscalate && (
              <Button
                variant="outline"
                disabled={actionLoading === selected.id}
                onClick={() => void handleAction(selected.id, "escalate")}
              >
                Escalate
              </Button>
            )}
            {selected && selectedActions?.canResolve && (
              <Button
                disabled={actionLoading === selected.id}
                onClick={() => void handleAction(selected.id, "resolve")}
              >
                {actionLoading === selected.id ? "Saving…" : "Resolve"}
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

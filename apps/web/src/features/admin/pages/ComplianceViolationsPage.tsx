import { useEffect, useState } from "react";
import { adminApi } from "@/features/admin/services/adminApi";
import { http } from "@/api/http";
import { endpoints } from "@/api/endpoints";
import type { ViolationDto, ViolationCategory, ViolationStatus } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Select } from "@/components/ui/select";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";

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

const truncate = (value: string, len = 12) =>
  value.length > len ? value.slice(0, len) + "…" : value;

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

  const loadViolations = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getAllViolations();
      setViolations(data);
    } catch {
      setError("Failed to load violations.");
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
    try {
      const url =
        action === "resolve"
          ? endpoints.compliance.resolveViolation(id)
          : action === "dismiss"
            ? endpoints.compliance.dismissViolation(id)
            : endpoints.compliance.escalateViolation(id);
      await http.post(url);
      await loadViolations();
    } catch {
      setError(`Failed to ${action} violation.`);
    } finally {
      setActionLoading(null);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Compliance Violations</h1>
        <p className="mt-1 text-muted-foreground">All violations across all deals.</p>
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
                ) : error ? (
                  <p className="py-8 text-center text-destructive">{error}</p>
                ) : filtered.length === 0 ? (
                  <p className="py-8 text-center text-muted-foreground">No violations found.</p>
                ) : (
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Deal ID</TableHead>
                        <TableHead>Reported By</TableHead>
                        <TableHead>Target User</TableHead>
                        <TableHead>Category</TableHead>
                        <TableHead>Status</TableHead>
                        <TableHead className="hidden lg:table-cell">Description</TableHead>
                        <TableHead>Detected At</TableHead>
                        <TableHead className="text-right">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {filtered.map((v) => (
                        <TableRow key={v.id}>
                          <TableCell className="font-mono text-xs">{truncate(v.dealId)}</TableCell>
                          <TableCell className="font-mono text-xs">{truncate(v.reportedByUserId)}</TableCell>
                          <TableCell className="font-mono text-xs">{truncate(v.targetUserId)}</TableCell>
                          <TableCell>
                            <Badge variant="outline">{v.category}</Badge>
                          </TableCell>
                          <TableCell>
                            <Badge variant={statusVariant(v.status)} className={statusClassName(v.status)}>
                              {v.status}
                            </Badge>
                          </TableCell>
                          <TableCell className="hidden lg:table-cell max-w-[200px] truncate text-sm text-muted-foreground">
                            {v.description}
                          </TableCell>
                          <TableCell className="text-sm text-muted-foreground">
                            {new Date(v.detectedAt).toLocaleDateString()}
                          </TableCell>
                          <TableCell className="text-right">
                            <div className="flex justify-end gap-1">
                              {v.status !== "Resolved" && (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  disabled={actionLoading === v.id}
                                  onClick={() => handleAction(v.id, "resolve")}
                                >
                                  Resolve
                                </Button>
                              )}
                              {v.status !== "Dismissed" && (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  disabled={actionLoading === v.id}
                                  onClick={() => handleAction(v.id, "dismiss")}
                                >
                                  Dismiss
                                </Button>
                              )}
                              {v.status !== "Escalated" && (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  disabled={actionLoading === v.id}
                                  onClick={() => handleAction(v.id, "escalate")}
                                >
                                  Escalate
                                </Button>
                              )}
                            </div>
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
    </div>
  );
};

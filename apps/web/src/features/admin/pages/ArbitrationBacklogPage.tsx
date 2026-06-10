import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { RefreshCw, AlertTriangle, Users, Clock, Zap } from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { ArbitrationBacklogItemDto, ArbitratorCaseloadDto } from "@/api/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";
import { getApiErrorMessage } from "@/api/errors";
import { Alert, AlertDescription } from "@/components/ui/alert";

const truncateId = (id: string) => id.slice(0, 8) + "…";

const formatDate = (iso: string) =>
  new Date(iso).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });

const statusVariant = (status: string) => {
  switch (status) {
    case "Filed":
    case "EvidencePending":
      return "accent" as const;
    case "UnderReview":
    case "EvidenceComplete":
      return "default" as const;
    case "Appealed":
      return "destructive" as const;
    default:
      return "secondary" as const;
  }
};

export const ArbitrationBacklogPage = () => {
  const [items, setItems] = useState<ArbitrationBacklogItemDto[]>([]);
  const [caseload, setCaseload] = useState<ArbitratorCaseloadDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [assigningCaseId, setAssigningCaseId] = useState<string | null>(null);
  const [assignError, setAssignError] = useState<string | null>(null);

  const loadAll = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [backlog, loads] = await Promise.all([
        adminApi.getArbitrationBacklog(),
        adminApi.getArbitratorCaseload(),
      ]);
      setItems(backlog);
      setCaseload(loads);
    } catch {
      setError("Failed to load arbitration backlog.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadAll();
  }, []);

  const handleAutoAssign = async (caseId: string) => {
    setAssigningCaseId(caseId);
    setAssignError(null);
    try {
      await adminApi.autoAssignArbitrator(caseId);
      await loadAll();
    } catch (err) {
      setAssignError(getApiErrorMessage(err, "Auto-assign failed."));
    } finally {
      setAssigningCaseId(null);
    }
  };

  const totalActive = items.length;
  const unassigned = items.filter((i) => !i.arbitratorUserId).length;
  const overdue = items.filter((i) => i.isOverdue).length;

  const triageOrder = (a: ArbitrationBacklogItemDto, b: ArbitrationBacklogItemDto) => {
    const categoryPriority = (c: string) => {
      if (c === "CategoryA" || c === "CategoryG") return 0;
      if (c === "CategoryF") return 1;
      return 2;
    };
    const pa = categoryPriority(a.category);
    const pb = categoryPriority(b.category);
    if (pa !== pb) return pa - pb;
    if (a.isOverdue !== b.isOverdue) return a.isOverdue ? -1 : 1;
    const dueA = a.decisionDueAt ? new Date(a.decisionDueAt).getTime() : Infinity;
    const dueB = b.decisionDueAt ? new Date(b.decisionDueAt).getTime() : Infinity;
    if (dueA !== dueB) return dueA - dueB;
    return new Date(a.filedAt).getTime() - new Date(b.filedAt).getTime();
  };

  const sortedItems = [...items].sort(triageOrder);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Arbitration Backlog</h1>
        <p className="mt-1 text-muted-foreground">
          Monitor case assignments, SLA status, arbitrator caseload, and triage.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Total Active Cases</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{totalActive}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Unassigned</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{unassigned}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Overdue</CardTitle>
            <AlertTriangle className="h-4 w-4 text-destructive" />
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold text-destructive">{overdue}</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">Arbitrator caseload</CardTitle>
          <CardDescription>Soft cap 15 · Hard cap 20 active cases</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading caseload..." />
          ) : caseload.length === 0 ? (
            <p className="text-sm text-muted-foreground">No arbitrators on the panel.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Arbitrator</TableHead>
                  <TableHead>Active cases</TableHead>
                  <TableHead>Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {caseload.map((a) => (
                  <TableRow key={a.arbitratorUserId}>
                    <TableCell>
                      <div className="font-medium">{a.displayName ?? a.email}</div>
                      <div className="text-xs text-muted-foreground">{a.email}</div>
                    </TableCell>
                    <TableCell>{a.activeCaseCount}</TableCell>
                    <TableCell>
                      {a.isAtHardCap ? (
                        <Badge variant="destructive">At hard cap</Badge>
                      ) : a.isOverSoftCap ? (
                        <Badge variant="accent">Over soft cap</Badge>
                      ) : (
                        <Badge variant="success">Available</Badge>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <CardTitle className="text-lg">Cases (triage order)</CardTitle>
              <CardDescription>
                Safety/habitability → move-out → FIFO by decision due date
              </CardDescription>
            </div>
            <Button variant="outline" size="sm" onClick={() => void loadAll()} disabled={isLoading}>
              <RefreshCw className={`mr-2 h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
              Refresh
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {assignError && (
            <Alert variant="destructive" className="mb-4">
              <AlertDescription>{assignError}</AlertDescription>
            </Alert>
          )}
          {isLoading ? (
            <Loader label="Loading backlog..." />
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : sortedItems.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">No active arbitration cases.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Case ID</TableHead>
                  <TableHead>Deal ID</TableHead>
                  <TableHead>Arbitrator</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Category</TableHead>
                  <TableHead>Filed At</TableHead>
                  <TableHead>Decision Due</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {sortedItems.map((item) => (
                  <TableRow key={item.caseId}>
                    <TableCell className="font-mono text-sm" title={item.caseId}>
                      {truncateId(item.caseId)}
                    </TableCell>
                    <TableCell className="font-mono text-sm" title={item.dealId}>
                      {truncateId(item.dealId)}
                    </TableCell>
                    <TableCell>
                      {item.arbitratorEmail ?? (
                        <span className="text-muted-foreground italic">Unassigned</span>
                      )}
                    </TableCell>
                    <TableCell>
                      <Badge variant={statusVariant(item.status)}>{item.status}</Badge>
                    </TableCell>
                    <TableCell>{item.category}</TableCell>
                    <TableCell>{formatDate(item.filedAt)}</TableCell>
                    <TableCell>
                      {item.decisionDueAt ? (
                        <span className={item.isOverdue ? "font-semibold text-destructive" : ""}>
                          {formatDate(item.decisionDueAt)}
                        </span>
                      ) : (
                        <span className="text-muted-foreground">—</span>
                      )}
                    </TableCell>
                    <TableCell className="text-right space-x-2">
                      {!item.arbitratorUserId && item.status === "EvidenceComplete" && (
                        <Button
                          variant="outline"
                          size="sm"
                          disabled={assigningCaseId === item.caseId || caseload.length === 0}
                          onClick={() => void handleAutoAssign(item.caseId)}
                        >
                          <Zap className="h-3.5 w-3.5 mr-1" />
                          {assigningCaseId === item.caseId ? "Assigning…" : "Auto-assign"}
                        </Button>
                      )}
                      <Link to={`/app/arbitration/${item.caseId}`}>
                        <Button variant="ghost" size="sm">Open</Button>
                      </Link>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
};

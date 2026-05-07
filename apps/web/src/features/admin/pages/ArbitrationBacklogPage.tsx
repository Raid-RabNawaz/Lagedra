import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { RefreshCw, AlertTriangle, Users, Clock } from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { ArbitrationBacklogItemDto } from "@/api/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";

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
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadBacklog = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getArbitrationBacklog();
      setItems(data);
    } catch {
      setError("Failed to load the arbitration backlog.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadBacklog();
  }, []);

  const totalActive = items.length;
  const unassigned = items.filter((i) => !i.arbitratorUserId).length;
  const overdue = items.filter((i) => i.isOverdue).length;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Arbitration Backlog</h1>
        <p className="mt-1 text-muted-foreground">
          Monitor case assignments, SLA status, and triage.
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
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <CardTitle className="text-lg">Cases</CardTitle>
              <CardDescription>{totalActive} active case{totalActive !== 1 && "s"}</CardDescription>
            </div>
            <Button variant="outline" size="sm" onClick={() => void loadBacklog()} disabled={isLoading}>
              <RefreshCw className={`mr-2 h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
              Refresh
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading backlog..." />
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : items.length === 0 ? (
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
                  <TableHead>Tier</TableHead>
                  <TableHead>Filed At</TableHead>
                  <TableHead>Decision Due</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((item) => (
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
                    <TableCell>{item.tier}</TableCell>
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
                    <TableCell className="text-right">
                      <Link to={`/app/arbitration/${item.caseId}`}>
                        <Button variant="ghost" size="sm">Assign</Button>
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

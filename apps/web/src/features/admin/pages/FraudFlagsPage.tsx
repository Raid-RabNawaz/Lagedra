import { useEffect, useMemo, useState } from "react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { FraudFlagDto, IdentityFraudFlagDto } from "@/api/types";
import { getApiErrorMessage } from "@/api/errors";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
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

type AdminFraudFlag = {
  id: string;
  source: "Integrity" | "Identity";
  userId: string;
  userLabel: string;
  userEmail: string | null;
  severity: string;
  category: string;
  reason: string | null;
  detectedAt: string;
  slaDeadline: string | null;
  isResolved: boolean;
  isEscalated: boolean;
};

const severityBadgeVariant = (severity: string) => {
  if (severity === "High") return "destructive" as const;
  if (severity === "Medium") return "accent" as const;
  return "secondary" as const;
};

const toIntegrityFlag = (flag: FraudFlagDto): AdminFraudFlag => ({
  id: flag.id,
  source: "Integrity",
  userId: flag.userId,
  userLabel: flag.userDisplayName || flag.userEmail || flag.userId.slice(0, 8) + "…",
  userEmail: flag.userEmail ?? null,
  severity: flag.severity,
  category: flag.category,
  reason: null,
  detectedAt: flag.detectedAt,
  slaDeadline: null,
  isResolved: flag.isResolved,
  isEscalated: false,
});

const toIdentityFlag = (flag: IdentityFraudFlagDto): AdminFraudFlag => ({
  id: flag.id,
  source: "Identity",
  userId: flag.userId,
  userLabel: flag.userDisplayName || flag.userEmail || flag.userId.slice(0, 8) + "…",
  userEmail: flag.userEmail ?? null,
  severity: flag.isEscalated ? "High" : "Medium",
  category: flag.source,
  reason: flag.reason,
  detectedAt: flag.detectedAt,
  slaDeadline: flag.slaDeadline,
  isResolved: flag.isResolved,
  isEscalated: flag.isEscalated,
});

export const FraudFlagsPage = () => {
  const [flags, setFlags] = useState<AdminFraudFlag[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [resolvingId, setResolvingId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState("all");
  const [selected, setSelected] = useState<AdminFraudFlag | null>(null);

  const loadFlags = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [integrity, identity] = await Promise.all([
        adminApi.getAllFraudFlags(),
        adminApi.getIdentityFraudFlags(),
      ]);
      const merged = [
        ...integrity.map(toIntegrityFlag),
        ...identity.map(toIdentityFlag),
      ].sort((a, b) => +new Date(b.detectedAt) - +new Date(a.detectedAt));
      setFlags(merged);
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to load fraud flags."));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadFlags();
  }, []);

  const handleResolve = async (flag: AdminFraudFlag) => {
    setResolvingId(flag.id);
    setError(null);
    try {
      if (flag.source === "Identity") {
        await adminApi.resolveIdentityFlag(flag.id);
      } else {
        await adminApi.resolveFlag(flag.id);
      }
      setSelected(null);
      await loadFlags();
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to resolve flag."));
    } finally {
      setResolvingId(null);
    }
  };

  const filtered = useMemo(() => {
    if (activeTab === "high") return flags.filter((f) => f.severity === "High");
    if (activeTab === "medium") return flags.filter((f) => f.severity === "Medium");
    if (activeTab === "low") return flags.filter((f) => f.severity === "Low");
    if (activeTab === "open") return flags.filter((f) => !f.isResolved);
    return flags;
  }, [activeTab, flags]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Fraud Flags</h1>
        <p className="mt-1 text-muted-foreground">
          Integrity and identity-verification alerts. Open a row for the full
          record, then resolve it when the review is done.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">All Flags</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading fraud flags..." />
          ) : error && !selected ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : (
            <Tabs value={activeTab} onValueChange={setActiveTab}>
              <TabsList>
                <TabsTrigger value="all">All</TabsTrigger>
                <TabsTrigger value="open">Open</TabsTrigger>
                <TabsTrigger value="high">High</TabsTrigger>
                <TabsTrigger value="medium">Medium</TabsTrigger>
                <TabsTrigger value="low">Low</TabsTrigger>
              </TabsList>
              <TabsContent value={activeTab}>
                {filtered.length === 0 ? (
                  <p className="py-8 text-center text-muted-foreground">No flags found.</p>
                ) : (
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>User</TableHead>
                        <TableHead>Source</TableHead>
                        <TableHead>Severity</TableHead>
                        <TableHead>Category</TableHead>
                        <TableHead>Detected</TableHead>
                        <TableHead>Status</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {filtered.map((flag) => (
                        <TableRow
                          key={`${flag.source}-${flag.id}`}
                          className="cursor-pointer"
                          onClick={() => setSelected(flag)}
                        >
                          <TableCell>
                            <p>{flag.userLabel}</p>
                            {flag.userEmail && flag.userLabel !== flag.userEmail && (
                              <p className="text-xs text-muted-foreground">{flag.userEmail}</p>
                            )}
                          </TableCell>
                          <TableCell>
                            <Badge variant="outline">{flag.source}</Badge>
                          </TableCell>
                          <TableCell>
                            <Badge variant={severityBadgeVariant(flag.severity)}>
                              {flag.severity}
                            </Badge>
                          </TableCell>
                          <TableCell>{flag.category}</TableCell>
                          <TableCell className="text-sm text-muted-foreground">
                            {new Date(flag.detectedAt).toLocaleString()}
                          </TableCell>
                          <TableCell>
                            <Badge variant={flag.isResolved ? "success" : "outline"}>
                              {flag.isResolved ? "Resolved" : flag.isEscalated ? "Escalated" : "Open"}
                            </Badge>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </TabsContent>
            </Tabs>
          )}
        </CardContent>
      </Card>

      <Dialog open={selected !== null} onOpenChange={(open) => !open && setSelected(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Fraud flag details</DialogTitle>
            <DialogDescription>
              Resolve the flag after you have reviewed the user. Restrictions
              stay on the User restrictions page.
            </DialogDescription>
          </DialogHeader>
          {selected && (
            <div className="space-y-4">
              {error && <Alert variant="destructive">{error}</Alert>}
              <AdminDetailList
                rows={[
                  { label: "User", value: selected.userLabel },
                  { label: "Email", value: selected.userEmail },
                  { label: "User ID", value: selected.userId },
                  { label: "Source", value: selected.source },
                  { label: "Category", value: selected.category },
                  { label: "Severity", value: selected.severity },
                  { label: "Reason", value: selected.reason },
                  { label: "Detected", value: new Date(selected.detectedAt).toLocaleString() },
                  {
                    label: "SLA deadline",
                    value: selected.slaDeadline
                      ? new Date(selected.slaDeadline).toLocaleString()
                      : "—",
                  },
                  {
                    label: "Status",
                    value: selected.isResolved
                      ? "Resolved"
                      : selected.isEscalated
                        ? "Escalated"
                        : "Open",
                  },
                ]}
              />
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setSelected(null)}>
              Close
            </Button>
            {selected && !selected.isResolved && (
              <Button
                disabled={resolvingId === selected.id}
                onClick={() => void handleResolve(selected)}
              >
                {resolvingId === selected.id ? "Resolving…" : "Resolve"}
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

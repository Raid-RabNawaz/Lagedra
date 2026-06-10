import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { adminApi } from "@/features/admin/services/adminApi";
import { useAuthStore } from "@/app/auth/authStore";
import type { PendingPackApprovalDto } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";

export const DualControlApprovalsPage = () => {
  const user = useAuthStore((s) => s.user);
  const [pending, setPending] = useState<PendingPackApprovalDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionInFlight, setActionInFlight] = useState<string | null>(null);

  const loadPending = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.listPendingPackApprovals();
      setPending(data);
    } catch {
      setError("Failed to load pending approvals.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadPending();
  }, []);

  const handleApprove = async (item: PendingPackApprovalDto) => {
    if (!user?.userId) return;
    setActionInFlight(item.versionId);
    try {
      await adminApi.approveVersion(item.packId, item.versionId, user.userId);
      await loadPending();
    } catch {
      setError("Approval failed. You may already be the first approver — a different admin must provide the second approval.");
    } finally {
      setActionInFlight(null);
    }
  };

  const approvalHint = (item: PendingPackApprovalDto) => {
    if (!item.approvedBy) return "Awaiting first approver";
    if (item.approvedBy === user?.userId) return "You approved — needs second approver";
    return "First approval recorded — you can provide second approval";
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dual Control Approvals</h1>
        <p className="mt-1 text-muted-foreground">
          Jurisdiction pack versions requiring a second distinct platform admin approval.
        </p>
      </div>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between pb-4">
          <CardTitle className="text-lg">Pending approvals</CardTitle>
          <Button variant="outline" size="sm" onClick={() => void loadPending()} disabled={isLoading}>
            Refresh
          </Button>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading pending approvals..." />
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : pending.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">No versions awaiting approval.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Jurisdiction</TableHead>
                  <TableHead>Version</TableHead>
                  <TableHead>Effective</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {pending.map((item) => (
                  <TableRow key={item.versionId}>
                    <TableCell className="font-medium">{item.jurisdictionCode}</TableCell>
                    <TableCell>v{item.versionNumber}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {item.effectiveDate
                        ? new Date(item.effectiveDate).toLocaleDateString()
                        : "—"}
                    </TableCell>
                    <TableCell>
                      <span className="text-xs text-muted-foreground">{approvalHint(item)}</span>
                    </TableCell>
                    <TableCell className="text-right space-x-2">
                      <Link to="/app/admin/jurisdiction-packs">
                        <Button variant="ghost" size="sm">Manage</Button>
                      </Link>
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={
                          actionInFlight === item.versionId
                          || item.approvedBy === user?.userId
                        }
                        onClick={() => void handleApprove(item)}
                      >
                        {actionInFlight === item.versionId ? "Approving…" : "Approve"}
                      </Button>
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

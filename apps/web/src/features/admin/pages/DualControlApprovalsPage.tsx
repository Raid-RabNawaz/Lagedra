import { useState } from "react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { PackVersionSummaryDto } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";

export const DualControlApprovalsPage = () => {
  const [packId, setPackId] = useState("");
  const [versions, setVersions] = useState<PackVersionSummaryDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [actionInFlight, setActionInFlight] = useState<string | null>(null);

  const loadVersions = async () => {
    if (!packId.trim()) return;
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.listPackVersions(packId.trim());
      setVersions(data.filter((v) => v.status === "PendingApproval"));
    } catch {
      setError("Failed to load pending approvals.");
    } finally {
      setIsLoading(false);
    }
  };

  const handleApprove = async (versionId: string) => {
    setActionInFlight(versionId);
    try {
      await adminApi.approveVersion(packId.trim(), versionId);
      await loadVersions();
    } catch {
      setError("Approval failed. Please try again.");
    } finally {
      setActionInFlight(null);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dual Control Approvals</h1>
        <p className="mt-1 text-muted-foreground">
          Pending jurisdiction pack versions requiring second approver.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">Load Pending Approvals</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex gap-3">
            <Input
              placeholder="Enter pack GUID..."
              className="max-w-md"
              value={packId}
              onChange={(e) => setPackId(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && loadVersions()}
            />
            <Button onClick={loadVersions} disabled={!packId.trim() || isLoading}>
              Load Versions
            </Button>
          </div>
        </CardContent>
      </Card>

      {(isLoading || error || versions.length > 0) && (
        <Card>
          <CardContent className="pt-6">
            {isLoading ? (
              <Loader label="Loading pending approvals..." />
            ) : error ? (
              <p className="py-8 text-center text-destructive">{error}</p>
            ) : versions.length === 0 ? (
              <p className="py-8 text-center text-muted-foreground">No pending approvals.</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Version Label</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Jurisdiction Code</TableHead>
                    <TableHead>Created At</TableHead>
                    <TableHead>Effective Date</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {versions.map((v) => (
                    <TableRow key={v.versionId}>
                      <TableCell className="font-medium">{v.versionLabel}</TableCell>
                      <TableCell>
                        <Badge variant="accent">PendingApproval</Badge>
                      </TableCell>
                      <TableCell>{v.jurisdictionCode}</TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {new Date(v.createdAt).toLocaleDateString()}
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {v.effectiveDate
                          ? new Date(v.effectiveDate).toLocaleDateString()
                          : "—"}
                      </TableCell>
                      <TableCell className="text-right">
                        <Button
                          variant="outline"
                          size="sm"
                          disabled={actionInFlight === v.versionId}
                          onClick={() => handleApprove(v.versionId)}
                        >
                          Approve
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
};

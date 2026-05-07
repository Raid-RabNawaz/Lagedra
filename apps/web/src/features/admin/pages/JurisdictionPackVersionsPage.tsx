import { useState } from "react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { PackVersionSummaryDto, PackVersionStatus } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";

const statusBadgeVariant = (s: PackVersionStatus) => {
  switch (s) {
    case "Draft":           return "secondary" as const;
    case "PendingApproval": return "accent" as const;
    case "Active":          return "success" as const;
    case "Deprecated":      return "destructive" as const;
  }
};

export const JurisdictionPackVersionsPage = () => {
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
      setVersions(data);
    } catch {
      setError("Failed to load pack versions.");
    } finally {
      setIsLoading(false);
    }
  };

  const handleAction = async (
    versionId: string,
    action: (pId: string, vId: string) => Promise<void>,
  ) => {
    setActionInFlight(versionId);
    try {
      await action(packId.trim(), versionId);
      await loadVersions();
    } catch {
      setError("Action failed. Please try again.");
    } finally {
      setActionInFlight(null);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Jurisdiction Pack Versions</h1>
        <p className="mt-1 text-muted-foreground">
          Manage jurisdiction pack lifecycle: draft, approve, publish, deprecate.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">Load Pack Versions</CardTitle>
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
              <Loader label="Loading versions..." />
            ) : error ? (
              <p className="py-8 text-center text-destructive">{error}</p>
            ) : versions.length === 0 ? (
              <p className="py-8 text-center text-muted-foreground">No versions found.</p>
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
                        <Badge variant={statusBadgeVariant(v.status)}>{v.status}</Badge>
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
                        {v.status === "Draft" && (
                          <Button
                            variant="outline"
                            size="sm"
                            disabled={actionInFlight === v.versionId}
                            onClick={() => handleAction(v.versionId, adminApi.requestApproval)}
                          >
                            Request Approval
                          </Button>
                        )}
                        {v.status === "PendingApproval" && (
                          <Button
                            variant="outline"
                            size="sm"
                            disabled={actionInFlight === v.versionId}
                            onClick={() => handleAction(v.versionId, adminApi.approveVersion)}
                          >
                            Approve
                          </Button>
                        )}
                        {v.status === "Active" && (
                          <Button
                            variant="outline"
                            size="sm"
                            disabled={actionInFlight === v.versionId}
                            onClick={() => handleAction(v.versionId, adminApi.deprecateVersion)}
                          >
                            Deprecate
                          </Button>
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
    </div>
  );
};

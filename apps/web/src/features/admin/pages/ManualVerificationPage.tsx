import { useEffect, useState } from "react";
import { CheckCircle, XCircle } from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { ManualVerificationItemDto } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";

function slaBadge(hours: number) {
  if (hours > 12) return <Badge variant="success">{hours.toFixed(1)} h</Badge>;
  if (hours > 6) return <Badge variant="accent">{hours.toFixed(1)} h</Badge>;
  return <Badge variant="destructive">{hours.toFixed(1)} h</Badge>;
}

export const ManualVerificationPage = () => {
  const [queue, setQueue] = useState<ManualVerificationItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [acting, setActing] = useState<string | null>(null);

  const loadQueue = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getManualVerificationQueue();
      setQueue(data);
    } catch {
      setError("Failed to load manual verification queue.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadQueue();
  }, []);

  const handleApprove = async (id: string) => {
    setActing(id);
    try {
      await adminApi.approveManualVerification(id);
      await loadQueue();
    } catch {
      setError("Failed to approve verification.");
    } finally {
      setActing(null);
    }
  };

  const handleReject = async (id: string) => {
    setActing(id);
    try {
      await adminApi.rejectManualVerification(id);
      await loadQueue();
    } catch {
      setError("Failed to reject verification.");
    } finally {
      setActing(null);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">
          Manual Verification
        </h1>
        <p className="mt-1 text-muted-foreground">
          KYC manual review fallback queue. SLA: ≤ 24 hours.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">Pending Reviews</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading queue..." />
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : queue.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">
              No pending verifications.
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>User ID</TableHead>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead className="hidden md:table-cell">
                    Submitted At
                  </TableHead>
                  <TableHead>SLA Remaining</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {queue.map((item) => (
                  <TableRow key={item.profileId}>
                    <TableCell className="font-mono text-xs">
                      {item.userId.slice(0, 8)}…
                    </TableCell>
                    <TableCell>
                      {[item.firstName, item.lastName]
                        .filter(Boolean)
                        .join(" ") || "—"}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {item.email || "—"}
                    </TableCell>
                    <TableCell className="hidden md:table-cell text-sm text-muted-foreground">
                      {new Date(item.submittedAt).toLocaleString()}
                    </TableCell>
                    <TableCell>{slaBadge(item.hoursRemaining)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button
                          variant="ghost"
                          size="sm"
                          disabled={acting === item.profileId}
                          onClick={() => void handleApprove(item.profileId)}
                        >
                          <CheckCircle className="mr-1 h-4 w-4 text-green-600" />
                          Approve
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          disabled={acting === item.profileId}
                          onClick={() => void handleReject(item.profileId)}
                        >
                          <XCircle className="mr-1 h-4 w-4 text-red-600" />
                          Reject
                        </Button>
                      </div>
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

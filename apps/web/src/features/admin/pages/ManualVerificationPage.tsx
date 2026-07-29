import { useEffect, useState } from "react";
import { CheckCircle, Eye, Loader2, XCircle } from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type {
  ManualVerificationDetailDto,
  ManualVerificationItemDto,
} from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
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

const documentLabels: Record<string, string> = {
  IdFront: "ID — front",
  IdBack: "ID — back",
  Selfie: "Live selfie",
};

export const ManualVerificationPage = () => {
  const [queue, setQueue] = useState<ManualVerificationItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [acting, setActing] = useState<string | null>(null);

  const [reviewing, setReviewing] = useState<ManualVerificationItemDto | null>(null);
  const [detail, setDetail] = useState<ManualVerificationDetailDto | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);

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

  const openReview = async (item: ManualVerificationItemDto) => {
    setReviewing(item);
    setDetail(null);
    setDetailError(null);
    setDetailLoading(true);
    try {
      const data = await adminApi.getManualVerificationDetail(item.profileId);
      setDetail(data);
    } catch {
      setDetailError("Failed to load the submitted documents.");
    } finally {
      setDetailLoading(false);
    }
  };

  const closeReview = () => {
    setReviewing(null);
    setDetail(null);
    setDetailError(null);
  };

  const handleApprove = async (id: string) => {
    setActing(id);
    try {
      await adminApi.approveManualVerification(id);
      closeReview();
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
      closeReview();
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
          Review submitted ID photos and live selfies to verify user identity. SLA: ≤ 24 hours.
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
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={acting === item.profileId}
                        onClick={() => void openReview(item)}
                      >
                        <Eye className="mr-1 h-4 w-4" />
                        Review
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={reviewing !== null} onOpenChange={(open) => !open && closeReview()}>
        <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Identity review</DialogTitle>
            <DialogDescription>
              Compare the live selfie against the ID photo, and check that the name and date of
              birth below match the document.
            </DialogDescription>
          </DialogHeader>

          {detailLoading ? (
            <Loader label="Loading documents..." />
          ) : detailError ? (
            <p className="py-6 text-center text-destructive">{detailError}</p>
          ) : detail ? (
            <div className="space-y-5">
              <div className="grid grid-cols-2 gap-x-6 gap-y-2 text-sm sm:grid-cols-3">
                <div>
                  <p className="text-muted-foreground">Name</p>
                  <p className="font-medium">
                    {[detail.firstName, detail.lastName].filter(Boolean).join(" ") || "—"}
                  </p>
                </div>
                <div>
                  <p className="text-muted-foreground">Date of birth</p>
                  <p className="font-medium">
                    {detail.dateOfBirth
                      ? new Date(detail.dateOfBirth).toLocaleDateString()
                      : "—"}
                  </p>
                </div>
                <div>
                  <p className="text-muted-foreground">Email</p>
                  <p className="font-medium break-all">{detail.email || "—"}</p>
                </div>
              </div>

              {detail.documents.length === 0 ? (
                <p className="py-4 text-center text-muted-foreground">
                  No documents were uploaded for this submission.
                </p>
              ) : (
                <div className="grid gap-4 sm:grid-cols-2">
                  {detail.documents.map((doc) => (
                    <figure key={doc.documentType} className="space-y-2">
                      <figcaption className="text-sm font-medium">
                        {documentLabels[doc.documentType] ?? doc.documentType}
                        <span className="ml-2 text-xs font-normal text-muted-foreground">
                          {new Date(doc.uploadedAt).toLocaleString()}
                        </span>
                      </figcaption>
                      <a href={doc.downloadUrl} target="_blank" rel="noreferrer">
                        <img
                          src={doc.downloadUrl}
                          alt={documentLabels[doc.documentType] ?? doc.documentType}
                          className="w-full rounded-lg border object-contain max-h-72 bg-muted"
                        />
                      </a>
                    </figure>
                  ))}
                </div>
              )}
            </div>
          ) : null}

          <DialogFooter>
            <Button
              variant="destructive"
              disabled={!reviewing || acting === reviewing.profileId || detailLoading}
              onClick={() => reviewing && void handleReject(reviewing.profileId)}
            >
              {acting === reviewing?.profileId ? (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              ) : (
                <XCircle className="mr-1 h-4 w-4" />
              )}
              Reject
            </Button>
            <Button
              disabled={!reviewing || acting === reviewing.profileId || detailLoading}
              onClick={() => reviewing && void handleApprove(reviewing.profileId)}
            >
              {acting === reviewing?.profileId ? (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              ) : (
                <CheckCircle className="mr-1 h-4 w-4" />
              )}
              Approve
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

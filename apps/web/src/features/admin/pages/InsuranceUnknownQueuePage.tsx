import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { RefreshCw } from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { InsuranceQueueItemDto } from "@/api/types";
import { getApiErrorMessage } from "@/api/errors";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert } from "@/components/ui/alert";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";
import { AdminDetailList } from "@/features/admin/components/AdminDetailList";

const truncateId = (id: string) => id.slice(0, 8) + "…";

const formatDate = (iso: string | null | undefined) =>
  iso
    ? new Date(iso).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" })
    : "—";

const slaBadge = (hours: number) => {
  if (hours > 12) return <Badge variant="success">{hours.toFixed(1)}h</Badge>;
  if (hours >= 6) return <Badge variant="accent">{hours.toFixed(1)}h</Badge>;
  return <Badge variant="destructive">{hours.toFixed(1)}h</Badge>;
};

export const InsuranceUnknownQueuePage = () => {
  const [items, setItems] = useState<InsuranceQueueItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<InsuranceQueueItemDto | null>(null);
  const [acting, setActing] = useState(false);
  const [provider, setProvider] = useState("");
  const [policyNumber, setPolicyNumber] = useState("");

  const loadQueue = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getUnknownQueue();
      setItems(data);
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to load the insurance unknown queue."));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadQueue();
  }, []);

  const openDetail = (item: InsuranceQueueItemDto) => {
    setSelected(item);
    setProvider(item.provider ?? "");
    setPolicyNumber(item.policyNumber ?? "");
  };

  const resolve = async (isActive: boolean) => {
    if (!selected) return;
    setActing(true);
    setError(null);
    try {
      await adminApi.resolveInsuranceUnknown(selected.dealId, {
        isActive,
        provider: provider.trim() || null,
        policyNumber: policyNumber.trim() || null,
      });
      setSelected(null);
      await loadQueue();
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to update insurance status."));
    } finally {
      setActing(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Insurance Unknown Queue</h1>
        <p className="mt-1 text-muted-foreground">
          Deals whose stay protection could not be verified. Review the record,
          then mark it active or not active before the 72-hour window lapses.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <CardTitle className="text-lg">Unknown Queue</CardTitle>
              <CardDescription>
                {items.length} item{items.length !== 1 && "s"}
              </CardDescription>
            </div>
            <Button variant="outline" size="sm" onClick={() => void loadQueue()} disabled={isLoading}>
              <RefreshCw className={`mr-2 h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
              Refresh
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading queue..." />
          ) : error && !selected ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : items.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">No items in the unknown queue.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Listing / deal</TableHead>
                  <TableHead>Tenant</TableHead>
                  <TableHead>Unknown since</TableHead>
                  <TableHead className="text-right">Hours remaining</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((item) => (
                  <TableRow
                    key={item.policyRecordId}
                    className="cursor-pointer"
                    onClick={() => openDetail(item)}
                  >
                    <TableCell>
                      <p className="font-medium">{item.listingTitle || "Untitled listing"}</p>
                      <p className="font-mono text-xs text-muted-foreground" title={item.dealId}>
                        {truncateId(item.dealId)}
                      </p>
                    </TableCell>
                    <TableCell>
                      <p>{item.tenantName || item.tenantEmail || truncateId(item.tenantUserId)}</p>
                      {item.tenantEmail && item.tenantName && (
                        <p className="text-xs text-muted-foreground">{item.tenantEmail}</p>
                      )}
                    </TableCell>
                    <TableCell>{formatDate(item.unknownSince)}</TableCell>
                    <TableCell className="text-right">{slaBadge(item.hoursRemaining)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={selected !== null} onOpenChange={(open) => !open && setSelected(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Insurance review</DialogTitle>
            <DialogDescription>
              Confirm coverage or mark the stay unprotected. This removes the
              deal from the unknown queue.
            </DialogDescription>
          </DialogHeader>
          {selected && (
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
                  { label: "Tenant", value: selected.tenantName },
                  { label: "Tenant email", value: selected.tenantEmail },
                  { label: "Stay", value: selected.checkIn && selected.checkOut
                    ? `${selected.checkIn} → ${selected.checkOut}`
                    : "—" },
                  { label: "Unknown since", value: formatDate(selected.unknownSince) },
                  { label: "Hours remaining", value: `${selected.hoursRemaining.toFixed(1)}h` },
                  { label: "Screening", value: selected.screeningStatus },
                  { label: "Flagged reason", value: selected.flaggedReason },
                  { label: "Coverage", value: selected.coverageScope },
                  { label: "Last verified", value: formatDate(selected.verifiedAt) },
                ]}
              />
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="space-y-1.5">
                  <Label htmlFor="insurance-provider">Provider</Label>
                  <Input
                    id="insurance-provider"
                    value={provider}
                    onChange={(e) => setProvider(e.target.value)}
                    placeholder="Truvi / other"
                  />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="insurance-policy">Policy / reservation</Label>
                  <Input
                    id="insurance-policy"
                    value={policyNumber}
                    onChange={(e) => setPolicyNumber(e.target.value)}
                  />
                </div>
              </div>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setSelected(null)} disabled={acting}>
              Close
            </Button>
            <Button variant="destructive" onClick={() => void resolve(false)} disabled={acting}>
              {acting ? "Saving…" : "Mark not active"}
            </Button>
            <Button onClick={() => void resolve(true)} disabled={acting}>
              {acting ? "Saving…" : "Mark active"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

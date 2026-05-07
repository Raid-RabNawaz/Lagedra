import { useEffect, useState } from "react";
import { RefreshCw, ShieldAlert } from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { EvidenceScanQueueItemDto, ScanStatus } from "@/api/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Loader } from "@/components/shared/Loader";

const truncateId = (id: string) => id.slice(0, 8) + "…";

const formatDate = (iso: string) =>
  new Date(iso).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });

const scanBadge = (status: ScanStatus) => {
  switch (status) {
    case "Pending":
      return <Badge variant="accent">Pending</Badge>;
    case "Infected":
      return <Badge variant="destructive">Infected</Badge>;
    case "Clean":
      return <Badge variant="success">Clean</Badge>;
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
};

type TabFilter = "all" | "Pending" | "Infected";

export const EvidenceReviewPage = () => {
  const [items, setItems] = useState<EvidenceScanQueueItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<TabFilter>("all");
  const [quarantining, setQuarantining] = useState<string | null>(null);

  const loadQueue = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getEvidenceScanQueue();
      setItems(data);
    } catch {
      setError("Failed to load the evidence scan queue.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadQueue();
  }, []);

  const filtered =
    tab === "all" ? items : items.filter((i) => i.scanStatus === tab);

  const handleQuarantine = async (uploadId: string) => {
    setQuarantining(uploadId);
    try {
      await adminApi.quarantineUpload(uploadId);
      await loadQueue();
    } catch {
      setError("Failed to quarantine upload.");
    } finally {
      setQuarantining(null);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Evidence Review</h1>
        <p className="mt-1 text-muted-foreground">
          Malware scan queue and manual evidence review.
        </p>
      </div>

      <Tabs value={tab} onValueChange={(v) => setTab(v as TabFilter)}>
        <TabsList>
          <TabsTrigger value="all">All</TabsTrigger>
          <TabsTrigger value="Pending">Pending</TabsTrigger>
          <TabsTrigger value="Infected">Infected</TabsTrigger>
        </TabsList>
      </Tabs>

      <Card>
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <CardTitle className="text-lg">Scan Queue</CardTitle>
              <CardDescription>
                {filtered.length} item{filtered.length !== 1 && "s"}
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
            <Loader label="Loading scan queue..." />
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : filtered.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">No items in the queue.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>File Name</TableHead>
                  <TableHead>MIME Type</TableHead>
                  <TableHead>Deal ID</TableHead>
                  <TableHead>Manifest ID</TableHead>
                  <TableHead>Upload Date</TableHead>
                  <TableHead>Scan Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filtered.map((item) => (
                  <TableRow key={item.uploadId}>
                    <TableCell className="font-medium max-w-[200px] truncate" title={item.originalFileName}>
                      {item.originalFileName}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">{item.mimeType}</TableCell>
                    <TableCell className="font-mono text-sm" title={item.dealId}>
                      {truncateId(item.dealId)}
                    </TableCell>
                    <TableCell className="font-mono text-sm" title={item.manifestId}>
                      {truncateId(item.manifestId)}
                    </TableCell>
                    <TableCell className="text-sm">{formatDate(item.uploadedAt)}</TableCell>
                    <TableCell>{scanBadge(item.scanStatus)}</TableCell>
                    <TableCell className="text-right">
                      {item.scanStatus === "Pending" && (
                        <Button
                          variant="destructive"
                          size="sm"
                          disabled={quarantining === item.uploadId}
                          onClick={() => void handleQuarantine(item.uploadId)}
                        >
                          <ShieldAlert className="mr-1 h-4 w-4" />
                          {quarantining === item.uploadId ? "Quarantining…" : "Quarantine"}
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
    </div>
  );
};

import { useEffect, useState } from "react";
import { RefreshCw } from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { InsuranceQueueItemDto } from "@/api/types";
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

const slaBadge = (hours: number) => {
  if (hours > 12) return <Badge variant="success">{hours.toFixed(1)}h</Badge>;
  if (hours >= 6) return <Badge variant="accent">{hours.toFixed(1)}h</Badge>;
  return <Badge variant="destructive">{hours.toFixed(1)}h</Badge>;
};

export const InsuranceUnknownQueuePage = () => {
  const [items, setItems] = useState<InsuranceQueueItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadQueue = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getUnknownQueue();
      setItems(data);
    } catch {
      setError("Failed to load the insurance unknown queue.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadQueue();
  }, []);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Insurance Unknown Queue</h1>
        <p className="mt-1 text-muted-foreground">
          Deals with insurance status &lsquo;Unknown&rsquo; requiring manual verification.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <CardTitle className="text-lg">Unknown Queue</CardTitle>
              <CardDescription>{items.length} item{items.length !== 1 && "s"}</CardDescription>
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
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : items.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">No items in the unknown queue.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Deal ID</TableHead>
                  <TableHead>Tenant User ID</TableHead>
                  <TableHead>Unknown Since</TableHead>
                  <TableHead className="text-right">Hours Remaining</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((item) => (
                  <TableRow key={item.policyRecordId}>
                    <TableCell className="font-mono text-sm" title={item.dealId}>
                      {truncateId(item.dealId)}
                    </TableCell>
                    <TableCell className="font-mono text-sm" title={item.tenantUserId}>
                      {truncateId(item.tenantUserId)}
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
    </div>
  );
};

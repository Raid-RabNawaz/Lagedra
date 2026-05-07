import { useEffect, useState } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { AuditSearchParams, AuditSearchResultDto } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";

export const AuditSearchPage = () => {
  const [result, setResult] = useState<AuditSearchResultDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const pageSize = 50;

  const [userId, setUserId] = useState("");
  const [eventType, setEventType] = useState("");
  const [entityType, setEntityType] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");

  const search = async (p: number) => {
    setIsLoading(true);
    setError(null);
    try {
      const params: AuditSearchParams = { page: p, pageSize };
      if (userId.trim()) params.userId = userId.trim();
      if (eventType.trim()) params.eventType = eventType.trim();
      if (entityType.trim()) params.entityType = entityType.trim();
      if (startDate) params.startDate = startDate;
      if (endDate) params.endDate = endDate;
      const data = await adminApi.searchAuditEvents(params);
      setResult(data);
    } catch {
      setError("Failed to search audit events.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void search(page);
  }, [page]);

  const handleSearch = () => {
    setPage(1);
    void search(1);
  };

  const truncate = (value: string | null | undefined, max: number) => {
    if (!value) return "—";
    return value.length > max ? value.slice(0, max) + "…" : value;
  };

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / pageSize)) : 1;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Audit Log</h1>
        <p className="mt-1 text-muted-foreground">
          Search and review platform audit events.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">Filters</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <div className="space-y-1.5">
              <Label htmlFor="userId">User ID</Label>
              <Input
                id="userId"
                placeholder="Filter by user ID..."
                value={userId}
                onChange={(e) => setUserId(e.target.value)}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="eventType">Event Type</Label>
              <Input
                id="eventType"
                placeholder="Filter by event type..."
                value={eventType}
                onChange={(e) => setEventType(e.target.value)}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="entityType">Entity Type</Label>
              <Input
                id="entityType"
                placeholder="Filter by entity type..."
                value={entityType}
                onChange={(e) => setEntityType(e.target.value)}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="startDate">Start Date</Label>
              <Input
                id="startDate"
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="endDate">End Date</Label>
              <Input
                id="endDate"
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
              />
            </div>
            <div className="flex items-end">
              <Button onClick={handleSearch} disabled={isLoading}>
                Search
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="pt-6">
          {isLoading ? (
            <Loader label="Searching audit events..." />
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : !result || result.items.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">No audit events found.</p>
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Timestamp</TableHead>
                    <TableHead>User ID</TableHead>
                    <TableHead>Event Type</TableHead>
                    <TableHead>Entity Type</TableHead>
                    <TableHead>Entity ID</TableHead>
                    <TableHead>IP Address</TableHead>
                    <TableHead>Details</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {result.items.map((event) => (
                    <TableRow key={event.id}>
                      <TableCell className="whitespace-nowrap text-sm">
                        {new Date(event.timestamp).toLocaleString()}
                      </TableCell>
                      <TableCell className="font-mono text-xs">
                        {truncate(event.userId, 12)}
                      </TableCell>
                      <TableCell>
                        <Badge variant="secondary">{event.eventType}</Badge>
                      </TableCell>
                      <TableCell className="text-sm">{event.entityType}</TableCell>
                      <TableCell className="font-mono text-xs">
                        {truncate(event.entityId, 12)}
                      </TableCell>
                      <TableCell className="text-sm">{event.ipAddress ?? "—"}</TableCell>
                      <TableCell className="max-w-[200px] truncate text-sm text-muted-foreground">
                        {truncate(event.details, 100)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>

              <div className="flex items-center justify-between pt-4">
                <p className="text-sm text-muted-foreground">
                  Page {page} of {totalPages} &middot; {result.totalCount} total events
                </p>
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={page <= 1}
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                  >
                    <ChevronLeft className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={page >= totalPages}
                    onClick={() => setPage((p) => p + 1)}
                  >
                    <ChevronRight className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
};

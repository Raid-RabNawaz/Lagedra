import { useEffect, useState } from "react";
import { ArrowUp, ArrowDown, Download, Search, X } from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import { downloadListingAnalyticsReport } from "@/features/admin/lib/analyticsReports";
import type { AdminListingStatus, ListingAnalyticsItemDto } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DatePicker } from "@/components/ui/date-picker";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";
import { formatMoney } from "@/utils/format";

type SortKey = keyof Pick<
  ListingAnalyticsItemDto,
  "title" | "landlordName" | "status" | "createdAt" | "monthlyRentCents" | "applicationCount" | "conversionPercent" | "qualityScore" | "addedVia"
>;
type SortDir = "asc" | "desc";

const LISTING_STATUSES: AdminListingStatus[] = [
  "Draft",
  "InReview",
  "Published",
  "Activated",
  "Closed",
  "Denied",
];

const statusVariant: Record<AdminListingStatus, "default" | "secondary" | "destructive" | "accent" | "success"> = {
  Draft: "secondary",
  InReview: "default",
  Published: "accent",
  Activated: "success",
  Closed: "secondary",
  Denied: "destructive",
};

export const ListingAnalyticsPage = () => {
  const [items, setItems] = useState<ListingAnalyticsItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [sortKey, setSortKey] = useState<SortKey>("createdAt");
  const [sortDir, setSortDir] = useState<SortDir>("desc");

  // Filters (applied on demand so typing doesn't spam the API)
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<AdminListingStatus | "">("");
  const [addedFrom, setAddedFrom] = useState("");
  const [addedTo, setAddedTo] = useState("");

  const load = async (filters?: {
    search?: string;
    status?: AdminListingStatus | "";
    addedFrom?: string;
    addedTo?: string;
  }) => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getListingAnalytics({
        search: filters?.search?.trim() || undefined,
        status: filters?.status || undefined,
        addedFrom: filters?.addedFrom || undefined,
        addedTo: filters?.addedTo || undefined,
      });
      setItems(data);
    } catch {
      setError("Failed to load listing analytics.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const applyFilters = () => {
    void load({ search, status, addedFrom, addedTo });
  };

  const hasActiveFilters = Boolean(search.trim() || status || addedFrom || addedTo);

  const resetFilters = () => {
    setSearch("");
    setStatus("");
    setAddedFrom("");
    setAddedTo("");
    void load();
  };

  const toggleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortKey(key);
      setSortDir("desc");
    }
  };

  const sorted = [...items].sort((a, b) => {
    const av = a[sortKey];
    const bv = b[sortKey];
    if (typeof av === "string" && typeof bv === "string") {
      return sortDir === "asc" ? av.localeCompare(bv) : bv.localeCompare(av);
    }
    const diff = (av as number) - (bv as number);
    return sortDir === "asc" ? diff : -diff;
  });

  const SortIcon = ({ column }: { column: SortKey }) => {
    if (sortKey !== column) return null;
    return sortDir === "asc" ? (
      <ArrowUp className="ml-1 inline h-3.5 w-3.5" />
    ) : (
      <ArrowDown className="ml-1 inline h-3.5 w-3.5" />
    );
  };

  const headerButton = (label: string, key: SortKey) => (
    <Button
      variant="ghost"
      size="sm"
      className="-ml-3 h-auto px-3 py-1.5 font-medium"
      onClick={() => toggleSort(key)}
    >
      {label}
      <SortIcon column={key} />
    </Button>
  );

  const truncate = (value: string, max: number) =>
    value.length > max ? value.slice(0, max) + "…" : value;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Listing Analytics</h1>
          <p className="mt-1 text-muted-foreground">
            Per-listing performance metrics.
          </p>
        </div>
        <Button
          variant="outline"
          className="shrink-0 gap-2"
          disabled={isLoading || sorted.length === 0}
          onClick={() => downloadListingAnalyticsReport(sorted)}
        >
          <Download className="h-4 w-4" />
          Download report
        </Button>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">Filters</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
            <div className="space-y-1.5 lg:col-span-2">
              <Label htmlFor="filter-user">User</Label>
              <Input
                id="filter-user"
                placeholder="Landlord name or email"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") applyFilters();
                }}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="filter-status">Status</Label>
              <Select
                id="filter-status"
                value={status}
                onChange={(e) => setStatus(e.target.value as AdminListingStatus | "")}
              >
                <option value="">All statuses</option>
                {LISTING_STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {s === "InReview" ? "In review" : s}
                  </option>
                ))}
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="filter-added-from">Added from</Label>
              <DatePicker
                id="filter-added-from"
                value={addedFrom}
                onChange={setAddedFrom}
                max={addedTo || undefined}
                placeholder="Any date"
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="filter-added-to">Added to</Label>
              <DatePicker
                id="filter-added-to"
                value={addedTo}
                onChange={setAddedTo}
                min={addedFrom || undefined}
                placeholder="Any date"
              />
            </div>
          </div>
          <div className="mt-4 flex gap-2">
            <Button onClick={applyFilters} disabled={isLoading} className="gap-2">
              <Search className="h-4 w-4" />
              Apply filters
            </Button>
            {hasActiveFilters && (
              <Button variant="outline" onClick={resetFilters} disabled={isLoading} className="gap-2">
                <X className="h-4 w-4" />
                Reset
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">
            Listings
            {!isLoading && !error && (
              <span className="ml-2 text-sm font-normal text-muted-foreground">
                {sorted.length} result{sorted.length === 1 ? "" : "s"}
              </span>
            )}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading listing analytics..." />
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : sorted.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">
              {hasActiveFilters
                ? "No listings match the current filters."
                : "No listing data available."}
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{headerButton("Title", "title")}</TableHead>
                  <TableHead>{headerButton("Landlord", "landlordName")}</TableHead>
                  <TableHead>{headerButton("Status", "status")}</TableHead>
                  <TableHead className="hidden md:table-cell">{headerButton("Added", "createdAt")}</TableHead>
                  <TableHead className="hidden lg:table-cell">{headerButton("Rent", "monthlyRentCents")}</TableHead>
                  <TableHead>{headerButton("Applications", "applicationCount")}</TableHead>
                  <TableHead className="hidden md:table-cell">{headerButton("Conversion %", "conversionPercent")}</TableHead>
                  <TableHead>{headerButton("Quality", "qualityScore")}</TableHead>
                  <TableHead>{headerButton("Added via", "addedVia")}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {sorted.map((item) => (
                  <TableRow key={item.listingId}>
                    <TableCell>
                      <div>
                        <p className="font-medium">{truncate(item.title, 60)}</p>
                        <p className="text-xs text-muted-foreground font-mono">
                          {truncate(item.listingId, 12)}
                        </p>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div>
                        <p className="text-sm">{item.landlordName}</p>
                        {item.landlordEmail && (
                          <p className="text-xs text-muted-foreground">{item.landlordEmail}</p>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant={statusVariant[item.status] ?? "secondary"}>
                        {item.status === "InReview" ? "In review" : item.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="hidden md:table-cell text-sm text-muted-foreground">
                      {new Date(item.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell className="hidden lg:table-cell text-sm">
                      {formatMoney(item.monthlyRentCents)}
                    </TableCell>
                    <TableCell className="text-sm">
                      {item.applicationCount.toLocaleString()}
                    </TableCell>
                    <TableCell className="hidden md:table-cell text-sm">
                      {item.conversionPercent.toFixed(1)}%
                    </TableCell>
                    <TableCell className="text-sm">
                      {item.qualityScore}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {item.addedVia}
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

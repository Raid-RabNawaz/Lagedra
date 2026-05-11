import { useEffect, useState } from "react";
import { ArrowUp, ArrowDown } from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { ListingAnalyticsItemDto } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";

type SortKey = keyof Pick<ListingAnalyticsItemDto, "title" | "views" | "applicationCount" | "conversionPercent" | "qualityScore">;
type SortDir = "asc" | "desc";

export const ListingAnalyticsPage = () => {
  const [items, setItems] = useState<ListingAnalyticsItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [sortKey, setSortKey] = useState<SortKey>("views");
  const [sortDir, setSortDir] = useState<SortDir>("desc");

  useEffect(() => {
    const load = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const data = await adminApi.getListingAnalytics();
        setItems(data);
      } catch {
        setError("Failed to load listing analytics.");
      } finally {
        setIsLoading(false);
      }
    };
    void load();
  }, []);

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
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Listing Analytics</h1>
        <p className="mt-1 text-muted-foreground">
          Per-listing performance metrics.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">All Listings</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading listing analytics..." />
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : sorted.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">No listing data available.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{headerButton("Title", "title")}</TableHead>
                  <TableHead>{headerButton("Views", "views")}</TableHead>
                  <TableHead>{headerButton("Applications", "applicationCount")}</TableHead>
                  <TableHead>{headerButton("Conversion %", "conversionPercent")}</TableHead>
                  <TableHead>{headerButton("Quality Score", "qualityScore")}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {sorted.map((item) => (
                  <TableRow key={item.listingId}>
                    <TableCell>
                      <div>
                        <p className="font-medium">{item.title}</p>
                        <p className="text-xs text-muted-foreground font-mono">
                          {truncate(item.listingId, 12)}
                        </p>
                      </div>
                    </TableCell>
                    <TableCell className="text-sm">
                      {item.views.toLocaleString()}
                    </TableCell>
                    <TableCell className="text-sm">
                      {item.applicationCount.toLocaleString()}
                    </TableCell>
                    <TableCell className="text-sm">
                      {item.conversionPercent.toFixed(1)}%
                    </TableCell>
                    <TableCell className="text-sm">
                      {item.qualityScore}
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

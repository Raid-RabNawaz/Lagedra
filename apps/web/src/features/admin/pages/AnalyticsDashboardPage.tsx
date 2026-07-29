import { useEffect, useState } from "react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { PlatformSummaryDto } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader } from "@/components/shared/Loader";
import { formatMoney } from "@/utils/format";

export const AnalyticsDashboardPage = () => {
  const [summary, setSummary] = useState<PlatformSummaryDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");

  const load = async (start?: string, end?: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getPlatformSummary(
        start || undefined,
        end || undefined,
      );
      setSummary(data);
    } catch {
      setError("Failed to load analytics data.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const handleApply = () => {
    void load(startDate, endDate);
  };

  // Reuse the global money formatter so the analytics tiles render the
  // same whole-dollar amounts as the rest of the product.
  const formatCurrency = (cents: number) => formatMoney(cents);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Analytics Dashboard</h1>
        <p className="mt-1 text-muted-foreground">
          Platform performance overview.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">Date Range</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
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
            <Button onClick={handleApply} disabled={isLoading}>
              Apply
            </Button>
          </div>
        </CardContent>
      </Card>

      {isLoading ? (
        <Loader label="Loading analytics..." />
      ) : error ? (
        <p className="py-8 text-center text-destructive">{error}</p>
      ) : summary ? (
        <>
          <p className="text-sm text-muted-foreground">
            Period: {new Date(summary.periodStart).toLocaleDateString()} —{" "}
            {new Date(summary.periodEnd).toLocaleDateString()}
          </p>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <StatTile label="Total Listings" value={summary.totalListings.toLocaleString()} hint="All time" />
            <StatTile label="Listings Added" value={summary.listingsAdded.toLocaleString()} hint="In period" />
            <StatTile label="Applications" value={summary.totalApplications.toLocaleString()} hint="In period" />
            <StatTile label="New Deals" value={summary.newDeals.toLocaleString()} hint="In period" />
            <StatTile label="Active Deals" value={summary.activeDeals.toLocaleString()} hint="As of period end" />
            <StatTile label="MRR" value={formatCurrency(summary.mrrCents)} hint="From active deals" />
            <StatTile
              label="Conversion Rate"
              value={`${summary.conversionRatePercent.toFixed(1)}%`}
              hint="New deals / applications in period"
            />
          </div>
        </>
      ) : null}
    </div>
  );
};

function StatTile({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{label}</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-4xl font-bold tracking-tight">{value}</p>
        {hint && <p className="mt-1 text-xs text-muted-foreground">{hint}</p>}
      </CardContent>
    </Card>
  );
}

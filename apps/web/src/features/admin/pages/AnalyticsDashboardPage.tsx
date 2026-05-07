import { useEffect, useState } from "react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { PlatformSummaryDto } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader } from "@/components/shared/Loader";

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

  const formatCurrency = (cents: number) =>
    `$${(cents / 100).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

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
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Total Listings
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-4xl font-bold tracking-tight">
                {summary.totalListings.toLocaleString()}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Active Deals
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-4xl font-bold tracking-tight">
                {summary.activeDeals.toLocaleString()}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                MRR
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-4xl font-bold tracking-tight">
                {formatCurrency(summary.mrrCents)}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Conversion Rate
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-4xl font-bold tracking-tight">
                {summary.conversionRatePercent.toFixed(1)}%
              </p>
            </CardContent>
          </Card>
        </div>
      ) : null}
    </div>
  );
};

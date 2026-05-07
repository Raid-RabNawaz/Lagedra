import { useEffect, useState } from "react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { FraudFlagDto } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { Loader } from "@/components/shared/Loader";

const severityBadgeVariant = (severity: string) => {
  if (severity === "High") return "destructive" as const;
  if (severity === "Medium") return "accent" as const;
  return "secondary" as const;
};

const truncateId = (id: string) => id.slice(0, 8) + "…";

export const FraudFlagsPage = () => {
  const [flags, setFlags] = useState<FraudFlagDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [resolvingId, setResolvingId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState("all");

  const loadFlags = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getAllFraudFlags();
      setFlags(data);
    } catch {
      setError("Failed to load fraud flags.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadFlags();
  }, []);

  const handleResolve = async (id: string) => {
    setResolvingId(id);
    try {
      await adminApi.resolveFlag(id);
      await loadFlags();
    } catch {
      setError("Failed to resolve flag.");
    } finally {
      setResolvingId(null);
    }
  };

  const renderTable = (filtered: FraudFlagDto[]) =>
    filtered.length === 0 ? (
      <p className="py-8 text-center text-muted-foreground">No flags found.</p>
    ) : (
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>User ID</TableHead>
            <TableHead>Severity</TableHead>
            <TableHead>Category</TableHead>
            <TableHead>Detected At</TableHead>
            <TableHead>Status</TableHead>
            <TableHead className="text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {filtered.map((flag) => (
            <TableRow key={flag.id}>
              <TableCell className="font-mono text-xs" title={flag.userId}>
                {truncateId(flag.userId)}
              </TableCell>
              <TableCell>
                <Badge variant={severityBadgeVariant(flag.severity)}>
                  {flag.severity}
                </Badge>
              </TableCell>
              <TableCell>{flag.category}</TableCell>
              <TableCell className="text-sm text-muted-foreground">
                {new Date(flag.detectedAt).toLocaleDateString()}
              </TableCell>
              <TableCell>
                <Badge variant={flag.isResolved ? "success" : "outline"}>
                  {flag.isResolved ? "Resolved" : "Open"}
                </Badge>
              </TableCell>
              <TableCell className="text-right">
                {!flag.isResolved && (
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled={resolvingId === flag.id}
                    onClick={() => void handleResolve(flag.id)}
                  >
                    {resolvingId === flag.id ? "Resolving…" : "Resolve"}
                  </Button>
                )}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    );

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Fraud Flags</h1>
        <p className="mt-1 text-muted-foreground">
          Review and resolve fraud detection alerts.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">All Flags</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading fraud flags..." />
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : (
            <Tabs value={activeTab} onValueChange={setActiveTab}>
              <TabsList>
                <TabsTrigger value="all">All</TabsTrigger>
                <TabsTrigger value="high">High</TabsTrigger>
                <TabsTrigger value="medium">Medium</TabsTrigger>
                <TabsTrigger value="low">Low</TabsTrigger>
              </TabsList>

              <TabsContent value="all">{renderTable(flags)}</TabsContent>
              <TabsContent value="high">
                {renderTable(flags.filter((f) => f.severity === "High"))}
              </TabsContent>
              <TabsContent value="medium">
                {renderTable(flags.filter((f) => f.severity === "Medium"))}
              </TabsContent>
              <TabsContent value="low">
                {renderTable(flags.filter((f) => f.severity === "Low"))}
              </TabsContent>
            </Tabs>
          )}
        </CardContent>
      </Card>
    </div>
  );
};

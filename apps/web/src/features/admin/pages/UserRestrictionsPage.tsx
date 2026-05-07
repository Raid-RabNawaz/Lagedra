import { useEffect, useState } from "react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { UserRestrictionDto, RestrictionType } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Loader } from "@/components/shared/Loader";

const typeBadgeVariant = (type: string) => {
  switch (type) {
    case "Suspension":
      return "accent" as const;
    case "Ban":
      return "destructive" as const;
    default:
      return "outline" as const;
  }
};

const typeBadgeClassName = (type: string) =>
  type === "Warning" ? "bg-yellow-100 text-yellow-800 border-yellow-300" : undefined;

const truncate = (value: string, len = 12) =>
  value.length > len ? value.slice(0, len) + "…" : value;

const allRestrictionTypes: RestrictionType[] = ["Warning", "Suspension", "Ban"];

export const UserRestrictionsPage = () => {
  const [restrictions, setRestrictions] = useState<UserRestrictionDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [removing, setRemoving] = useState<string | null>(null);

  const [formUserId, setFormUserId] = useState("");
  const [formType, setFormType] = useState<RestrictionType>("Warning");
  const [formReason, setFormReason] = useState("");
  const loadRestrictions = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getAllRestrictions();
      setRestrictions(data);
    } catch {
      setError("Failed to load restrictions.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadRestrictions();
  }, []);

  const resetForm = () => {
    setFormUserId("");
    setFormType("Warning");
    setFormReason("");
  };

  const handleApply = async () => {
    if (!formUserId.trim() || !formReason.trim()) return;
    setSubmitting(true);
    setError(null);
    try {
      await adminApi.applyRestriction({
        userId: formUserId.trim(),
        restrictionLevel: formType,
        reason: formReason.trim(),
      });
      setDialogOpen(false);
      resetForm();
      await loadRestrictions();
    } catch {
      setError("Failed to apply restriction.");
    } finally {
      setSubmitting(false);
    }
  };

  const handleRemove = async (id: string) => {
    setRemoving(id);
    setError(null);
    try {
      await adminApi.removeRestriction(id);
      await loadRestrictions();
    } catch {
      setError("Failed to remove restriction.");
    } finally {
      setRemoving(null);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">User Restrictions</h1>
        <p className="mt-1 text-muted-foreground">
          Manage account restrictions, suspensions, and bans.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <CardTitle className="text-lg">All Restrictions</CardTitle>
            <Button onClick={() => setDialogOpen(true)}>New Restriction</Button>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading restrictions..." />
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : restrictions.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">No restrictions found.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>User ID</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Reason</TableHead>
                  <TableHead>Applied At</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {restrictions.map((r) => (
                  <TableRow key={r.id}>
                    <TableCell className="font-mono text-xs">{truncate(r.userId)}</TableCell>
                    <TableCell>
                      <Badge variant={typeBadgeVariant(r.restrictionType)} className={typeBadgeClassName(r.restrictionType)}>
                        {r.restrictionType}
                      </Badge>
                    </TableCell>
                    <TableCell className="max-w-[250px] truncate text-sm">
                      {r.reason}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {new Date(r.appliedAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant="ghost"
                        size="sm"
                        disabled={removing === r.id}
                        onClick={() => handleRemove(r.id)}
                      >
                        Remove
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>New Restriction</DialogTitle>
            <DialogDescription>Apply a warning, suspension, or ban to a user account.</DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-1.5">
              <label className="text-sm font-medium">User ID</label>
              <Input
                placeholder="Enter user ID"
                value={formUserId}
                onChange={(e) => setFormUserId(e.target.value)}
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-sm font-medium">Restriction Type</label>
              <Select value={formType} onChange={(e) => setFormType(e.target.value as RestrictionType)}>
                {allRestrictionTypes.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </Select>
            </div>

            <div className="space-y-1.5">
              <label className="text-sm font-medium">Reason</label>
              <textarea
                className="flex min-h-[80px] w-full rounded-lg border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 transition-colors"
                placeholder="Reason for restriction"
                value={formReason}
                onChange={(e) => setFormReason(e.target.value)}
              />
            </div>

          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              disabled={!formUserId.trim() || !formReason.trim() || submitting}
              onClick={handleApply}
            >
              {submitting ? "Applying…" : "Apply Restriction"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

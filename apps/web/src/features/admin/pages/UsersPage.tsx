import { useEffect, useState } from "react";
import { Shield, Search, ChevronLeft, ChevronRight } from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import type { UserProfileDto } from "@/api/types";
import type { UserRole } from "@/app/auth/roles";
import { allRoles, roleLabel } from "@/app/auth/roles";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";
import { ChangeRoleDialog } from "@/features/admin/components/ChangeRoleDialog";

export const UsersPage = () => {
  const [users, setUsers] = useState<UserProfileDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [searchQuery, setSearchQuery] = useState("");
  const [roleTarget, setRoleTarget] = useState<{ user: UserProfileDto } | null>(null);
  const pageSize = 50;

  const loadUsers = async (p: number) => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await authApi.listUsers(p, pageSize);
      setUsers(data);
    } catch {
      setError("Failed to load users.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadUsers(page);
  }, [page]);

  const filtered = searchQuery.trim()
    ? users.filter(
        (u) =>
          u.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
          (u.displayName?.toLowerCase().includes(searchQuery.toLowerCase()) ?? false) ||
          (u.firstName?.toLowerCase().includes(searchQuery.toLowerCase()) ?? false) ||
          (u.lastName?.toLowerCase().includes(searchQuery.toLowerCase()) ?? false),
      )
    : users;

  const handleRoleChanged = (userId: string, newRole: UserRole) => {
    setUsers((prev) =>
      prev.map((u) => (u.userId === userId ? { ...u, role: newRole } : u)),
    );
    setRoleTarget(null);
  };

  const roleBadgeVariant = (role: string | number) => {
    if (role === "PlatformAdmin") return "default" as const;
    if (role === "Landlord") return "accent" as const;
    return "secondary" as const;
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">User Management</h1>
        <p className="mt-1 text-muted-foreground">
          View and manage all platform users.
        </p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <CardTitle className="text-lg">All Users</CardTitle>
            <div className="relative w-full sm:w-64">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search by name or email..."
                className="pl-9"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading users..." />
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : filtered.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">No users found.</p>
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>User</TableHead>
                    <TableHead>Role</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="hidden md:table-cell">Joined</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filtered.map((user) => (
                    <TableRow key={user.userId}>
                      <TableCell>
                        <div>
                          <p className="font-medium">
                            {user.displayName || [user.firstName, user.lastName].filter(Boolean).join(" ") || user.email}
                          </p>
                          <p className="text-xs text-muted-foreground">{user.email}</p>
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={roleBadgeVariant(user.role)}>
                          {roleLabel(String(user.role))}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant={user.isActive ? "success" : "secondary"}>
                          {user.isActive ? "Active" : "Inactive"}
                        </Badge>
                      </TableCell>
                      <TableCell className="hidden md:table-cell text-sm text-muted-foreground">
                        {new Date(user.memberSince).toLocaleDateString()}
                      </TableCell>
                      <TableCell className="text-right">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setRoleTarget({ user })}
                        >
                          <Shield className="h-4 w-4" />
                          Change role
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>

              <div className="flex items-center justify-between pt-4">
                <p className="text-sm text-muted-foreground">
                  Page {page} &middot; {filtered.length} users shown
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
                    disabled={users.length < pageSize}
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

      {roleTarget && (
        <ChangeRoleDialog
          user={roleTarget.user}
          allRoles={allRoles}
          onClose={() => setRoleTarget(null)}
          onChanged={handleRoleChanged}
        />
      )}
    </div>
  );
};

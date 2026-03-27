import { useState } from "react";
import { Shield } from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import type { UserProfileDto } from "@/api/types";
import type { UserRole } from "@/app/auth/roles";
import { roleLabel } from "@/app/auth/roles";
import { Button } from "@/components/ui/button";
import { Select } from "@/components/ui/select";
import { FormError } from "@/components/shared/FormError";
import {
  Dialog,
  DialogContent,
  DialogClose,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";

type ChangeRoleDialogProps = {
  user: UserProfileDto;
  allRoles: readonly UserRole[];
  onClose: () => void;
  onChanged: (userId: string, newRole: UserRole) => void;
};

export function ChangeRoleDialog({ user, allRoles, onClose, onChanged }: ChangeRoleDialogProps) {
  const [selectedRole, setSelectedRole] = useState<UserRole>(String(user.role) as UserRole);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async () => {
    if (selectedRole === String(user.role)) {
      onClose();
      return;
    }

    setIsSubmitting(true);
    setError(null);
    try {
      await authApi.updateUserRole(user.userId, { newRole: selectedRole });
      onChanged(user.userId, selectedRole);
    } catch {
      setError("Failed to update role. You may not have permission.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent>
        <DialogClose onClose={onClose} />
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5" />
            Change User Role
          </DialogTitle>
          <DialogDescription>
            Update the role for <strong>{user.displayName || user.email}</strong>.
            This will change their permissions immediately.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3 py-2">
          <label className="text-sm font-medium">New role</label>
          <Select
            value={selectedRole}
            onChange={(e) => setSelectedRole(e.target.value as UserRole)}
            disabled={isSubmitting}
          >
            {allRoles.map((role) => (
              <option key={role} value={role}>
                {roleLabel(role)}
              </option>
            ))}
          </Select>
          <FormError message={error} />
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={isSubmitting}>
            {isSubmitting ? "Updating..." : "Update role"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

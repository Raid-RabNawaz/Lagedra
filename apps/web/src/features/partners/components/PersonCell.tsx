import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { cn } from "@/lib/utils";

function initials(name: string): string {
  const parts = name.split(/\s+/).filter(Boolean).slice(0, 2);
  if (parts.length === 0) return "?";
  return parts.map((p) => p[0]?.toUpperCase() ?? "").join("");
}

const shortId = (id: string) => `${id.slice(0, 8)}…`;

type PersonCellProps = {
  /** Resolved display name; falls back to a truncated user id when absent. */
  displayName?: string | null;
  email?: string | null;
  /** Used for the tooltip and as the fallback label when no name is known. */
  userId?: string;
  className?: string;
};

/**
 * Compact identity block (initials avatar + name + muted email) used in the
 * partner portal tables and lists so people are shown as people, not GUIDs.
 */
export function PersonCell({ displayName, email, userId, className }: PersonCellProps) {
  const name = displayName?.trim() || null;
  const hasEmail = Boolean(email?.trim());

  return (
    <div className={cn("flex items-center gap-3 min-w-0", className)} title={userId}>
      <Avatar className="h-9 w-9">
        <AvatarFallback className="bg-primary/10 text-primary text-xs">
          {name ? initials(name) : "?"}
        </AvatarFallback>
      </Avatar>
      <div className="min-w-0">
        {name ? (
          <p className="truncate text-sm font-medium leading-tight">{name}</p>
        ) : (
          <p className="truncate font-mono text-xs font-medium leading-tight">
            {userId ? shortId(userId) : "Unknown"}
          </p>
        )}
        {hasEmail && (
          <p className="truncate text-xs text-muted-foreground leading-tight mt-0.5">{email}</p>
        )}
      </div>
    </div>
  );
}

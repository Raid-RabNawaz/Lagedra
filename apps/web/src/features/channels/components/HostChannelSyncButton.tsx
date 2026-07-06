import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  useChannelConnections,
  useSyncChannel,
} from "@/features/channels/hooks/useChannels";

type HostChannelSyncButtonProps = {
  /** Called with a human-readable summary after a successful sync. */
  onSynced?: (message: string) => void;
  /** Called with a human-readable error message when a sync fails. */
  onError?: (message: string) => void;
};

/**
 * One-click "re-pull my channel listings" affordance that hosts can trigger
 * from wherever they manage listings (e.g. the My Listings header) without
 * detouring to the dedicated Import page. Renders nothing for hosts who have no
 * channel connection, so it stays invisible to the majority who don't use a PMS.
 * Syncs every non-disabled connection and refreshes the host's listing list so
 * freshly-imported drafts appear in place.
 */
export function HostChannelSyncButton({
  onSynced,
  onError,
}: HostChannelSyncButtonProps) {
  const { data: connections } = useChannelConnections();
  const sync = useSyncChannel();
  const queryClient = useQueryClient();
  const [busy, setBusy] = useState(false);

  const active = (connections ?? []).filter((c) => c.status !== "Disabled");
  if (active.length === 0) {
    return null;
  }

  const handleSync = async () => {
    setBusy(true);
    try {
      let pulled = 0;
      let created = 0;
      let updated = 0;
      for (const connection of active) {
        const result = await sync.mutateAsync(connection.id);
        pulled += result.pulled;
        created += result.created;
        updated += result.updated;
      }

      await queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });

      onSynced?.(
        pulled === 0
          ? "No OwnerRez listings found to import yet."
          : `Synced from OwnerRez — ${created} new draft${created === 1 ? "" : "s"}, ${updated} updated.`,
      );
    } catch (e) {
      onError?.(
        (e as Error)?.message ?? "Could not sync from OwnerRez. Please try again.",
      );
    } finally {
      setBusy(false);
    }
  };

  return (
    <Button
      variant="outline"
      onClick={handleSync}
      disabled={busy}
      title="Re-pull your listings from OwnerRez"
    >
      <RefreshCw className={busy ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
      {busy ? "Syncing..." : "Sync from OwnerRez"}
    </Button>
  );
}

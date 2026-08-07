import { useState } from "react";
import { AlertTriangle, Unplug } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { getApiErrorMessage } from "@/api/errors";
import { useDisconnectChannel } from "@/features/channels/hooks/useChannels";

type DisconnectChannelButtonProps = {
  connectionId: string;
  /** Provider name shown in the button title and confirmation copy. */
  providerLabel: string;
  /** Imported listing count, when known, so the host sees what is at stake. */
  listingCount?: number;
  disabled?: boolean;
  size?: "sm";
};

/**
 * Disconnects a PMS connection after an explicit confirmation. A host may only
 * hold one connection per provider, so this is how they switch to a different
 * account or rotate a leaked API token: disconnect, then connect again.
 */
export function DisconnectChannelButton({
  connectionId,
  providerLabel,
  listingCount,
  disabled,
  size,
}: DisconnectChannelButtonProps) {
  const disconnect = useDisconnectChannel();
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleOpenChange = (next: boolean) => {
    if (!next && disconnect.isPending) {
      return;
    }
    setError(null);
    setOpen(next);
  };

  const handleConfirm = async () => {
    setError(null);
    try {
      await disconnect.mutateAsync(connectionId);
      setOpen(false);
    } catch (e) {
      setError(
        getApiErrorMessage(
          e,
          `Could not disconnect ${providerLabel}. Please try again.`,
        ),
      );
    }
  };

  return (
    <>
      <Button
        variant="ghost"
        size={size}
        onClick={() => setOpen(true)}
        disabled={disabled || disconnect.isPending}
        title={`Disconnect ${providerLabel}`}
        className="gap-2 text-destructive hover:text-destructive sm:ml-auto"
      >
        <Unplug className="h-4 w-4" />
        Disconnect
      </Button>

      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-amber-500" />
              Disconnect {providerLabel}?
            </DialogTitle>
            <DialogDescription>
              Lagedra will stop syncing from {providerLabel} and stop pushing new
              bookings back to it.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-3 text-sm text-muted-foreground">
            <p>
              {listingCount === undefined || listingCount === 0
                ? `Listings already imported from ${providerLabel} stay in Lagedra — published ones keep taking bookings.`
                : `The ${listingCount} listing${listingCount === 1 ? "" : "s"} already imported from ${providerLabel} stay in Lagedra — published ones keep taking bookings.`}
            </p>
            <p>
              Afterwards you can connect a different {providerLabel} account.
              Reconnecting the same one picks up where you left off instead of
              importing duplicates.
            </p>
            {error && (
              <Alert variant="destructive">
                <AlertTriangle className="h-4 w-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => handleOpenChange(false)}
              disabled={disconnect.isPending}
            >
              Keep connected
            </Button>
            <Button
              variant="destructive"
              onClick={handleConfirm}
              disabled={disconnect.isPending}
            >
              {disconnect.isPending ? "Disconnecting..." : "Disconnect"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

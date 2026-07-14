import { useState } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { buttonVariants } from "@/components/ui/button-variants";
import { cn } from "@/lib/utils";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, type BadgeProps } from "@/components/ui/badge";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
} from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import {
  useChannelConnections,
  useConnectChannel,
  useSyncChannel,
  useSetChannelEnabled,
  useChannelListings,
} from "@/features/channels/hooks/useChannels";
import type {
  ChannelConnectionDto,
  ChannelConnectionStatus,
} from "@/api/types";
import {
  Link2,
  RefreshCw,
  Plug,
  CheckCircle2,
  AlertTriangle,
  ChevronDown,
  ChevronRight,
  ExternalLink,
  Building2,
} from "lucide-react";

const OWNERREZ_PROVIDER_KEY = "ownerrez";
const HOSTAWAY_PROVIDER_KEY = "hostaway";

const PROVIDER_LABELS: Record<string, string> = {
  [OWNERREZ_PROVIDER_KEY]: "OwnerRez",
  [HOSTAWAY_PROVIDER_KEY]: "Hostaway",
};

function providerLabel(key: string): string {
  return PROVIDER_LABELS[key.toLowerCase()] ?? key;
}

export function ChannelsPage() {
  const { data: connections, isLoading } = useChannelConnections();

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Import from your PMS</h1>
        <p className="mt-1 text-muted-foreground">
          Connect OwnerRez or Hostaway and Lagedra will pull your properties in
          as draft listings. Review and publish each one — once live, paid
          bookings are pushed back to your PMS automatically.
        </p>
      </div>

      <div className="grid gap-4">
        <ConnectOwnerRezCard />
        <ConnectHostawayCard />
      </div>

      <Separator />

      <div className="space-y-3">
        <h2 className="text-lg font-semibold">Your connections</h2>
        {isLoading ? (
          <Loader label="Loading connections..." />
        ) : !connections || connections.length === 0 ? (
          <EmptyState
            title="No channels connected yet"
            description="Connect OwnerRez or Hostaway above to import your listings into Lagedra."
          />
        ) : (
          connections.map((c) => <ConnectionCard key={c.id} connection={c} />)
        )}
      </div>

      <HowItWorks />
    </div>
  );
}

function ConnectOwnerRezCard() {
  const connect = useConnectChannel();
  const sync = useSyncChannel();

  const [displayName, setDisplayName] = useState("");
  const [advertiserId, setAdvertiserId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const busy = connect.isPending || sync.isPending;

  const handleConnect = async () => {
    setError(null);
    setSuccess(null);

    if (!advertiserId.trim()) {
      setError("Enter your OwnerRez advertiser ID.");
      return;
    }

    try {
      const connection = await connect.mutateAsync({
        providerKey: OWNERREZ_PROVIDER_KEY,
        externalAccountId: advertiserId.trim(),
        displayName: displayName.trim() || "OwnerRez",
      });

      const result = await sync.mutateAsync(connection.id);
      setDisplayName("");
      setAdvertiserId("");
      setSuccess(
        result.pulled > 0
          ? `Connected. Imported ${result.created} new and updated ${result.updated} listing(s) as drafts.`
          : "Connected. No listings were found yet — try syncing again once your OwnerRez feed is ready.",
      );
    } catch (e) {
      setError(
        (e as Error)?.message ??
          "Could not connect to OwnerRez. Check your advertiser ID and try again.",
      );
    }
  };

  return (
    <Card className="border-primary/20 bg-primary/5">
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <Plug className="h-5 w-5" />
          Connect OwnerRez
        </CardTitle>
        <CardDescription>
          Enter your OwnerRez advertiser ID to link your account.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {error && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        {success && (
          <Alert variant="success">
            <CheckCircle2 className="h-4 w-4" />
            <AlertDescription>{success}</AlertDescription>
          </Alert>
        )}

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="or-advertiserId">OwnerRez advertiser ID</Label>
            <Input
              id="or-advertiserId"
              placeholder="ora-12345"
              value={advertiserId}
              onChange={(e) => setAdvertiserId(e.target.value)}
              autoComplete="off"
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="or-displayName">Label (optional)</Label>
            <Input
              id="or-displayName"
              placeholder="My OwnerRez account"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              autoComplete="off"
            />
          </div>
        </div>

        <Button onClick={handleConnect} disabled={busy} className="gap-2">
          <Link2 className="h-4 w-4" />
          {busy ? "Connecting & importing..." : "Connect & import listings"}
        </Button>

        <p className="text-xs text-muted-foreground">
          Find your advertiser ID in OwnerRez under Settings → API / Channel
          integrations. Lagedra connects as your distribution channel — you
          don&apos;t need to share your OwnerRez password.
        </p>
      </CardContent>
    </Card>
  );
}

function ConnectHostawayCard() {
  const connect = useConnectChannel();
  const sync = useSyncChannel();

  const [displayName, setDisplayName] = useState("");
  const [accountId, setAccountId] = useState("");
  const [clientSecret, setClientSecret] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const busy = connect.isPending || sync.isPending;

  const handleConnect = async () => {
    setError(null);
    setSuccess(null);

    if (!accountId.trim()) {
      setError("Enter your Hostaway account ID.");
      return;
    }
    if (!/^\d+$/.test(accountId.trim())) {
      setError("Hostaway account ID must be numeric.");
      return;
    }
    if (!clientSecret.trim()) {
      setError("Enter your Hostaway API client secret.");
      return;
    }

    try {
      const connection = await connect.mutateAsync({
        providerKey: HOSTAWAY_PROVIDER_KEY,
        externalAccountId: accountId.trim(),
        displayName: displayName.trim() || "Hostaway",
        secret: clientSecret.trim(),
      });

      const result = await sync.mutateAsync(connection.id);
      setDisplayName("");
      setAccountId("");
      setClientSecret("");

      const importPart =
        result.pulled > 0
          ? `Imported ${result.created} new and updated ${result.updated} listing(s) as drafts.`
          : "No listings were found yet — try syncing again once listings are active in Hostaway.";
      const webhookPart =
        result.webhookRegistered === true
          ? " Live booking updates are connected automatically."
          : result.webhookRegistered === false
            ? " Listing sync worked, but webhook registration failed — try Sync again shortly."
            : "";
      setSuccess(`Connected. ${importPart}${webhookPart}`);
    } catch (e) {
      setError(
        (e as Error)?.message ??
          "Could not connect to Hostaway. Check your account ID and API secret.",
      );
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <Plug className="h-5 w-5" />
          Connect Hostaway
        </CardTitle>
        <CardDescription>
          Enter your Hostaway account ID and API client secret.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {error && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        {success && (
          <Alert variant="success">
            <CheckCircle2 className="h-4 w-4" />
            <AlertDescription>{success}</AlertDescription>
          </Alert>
        )}

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="ha-accountId">Hostaway account ID</Label>
            <Input
              id="ha-accountId"
              placeholder="12345"
              value={accountId}
              onChange={(e) => setAccountId(e.target.value)}
              autoComplete="off"
              inputMode="numeric"
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="ha-displayName">Label (optional)</Label>
            <Input
              id="ha-displayName"
              placeholder="My Hostaway account"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              autoComplete="off"
            />
          </div>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="ha-secret">API client secret</Label>
          <Input
            id="ha-secret"
            type="password"
            placeholder="Paste your Hostaway API client secret"
            value={clientSecret}
            onChange={(e) => setClientSecret(e.target.value)}
            autoComplete="off"
          />
        </div>

        <Button onClick={handleConnect} disabled={busy} className="gap-2">
          <Link2 className="h-4 w-4" />
          {busy ? "Connecting & importing..." : "Connect & import listings"}
        </Button>

        <p className="text-xs text-muted-foreground">
          Create an API client in Hostaway under Settings → Hostaway API. Your
          account ID is the <code className="text-[11px]">client_id</code>; the
          secret is encrypted at rest and never shown again. When you connect,
          Lagedra imports your listings and registers live booking updates for
          you automatically (on staging/production).
        </p>
      </CardContent>
    </Card>
  );
}

function ConnectionCard({ connection }: { connection: ChannelConnectionDto }) {
  const sync = useSyncChannel();
  const setEnabled = useSetChannelEnabled();
  const [expanded, setExpanded] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const disabled = connection.status === "Disabled";
  const busy = sync.isPending || setEnabled.isPending;
  const label = providerLabel(connection.providerKey);

  const handleSync = async () => {
    setError(null);
    try {
      await sync.mutateAsync(connection.id);
      setExpanded(true);
    } catch (e) {
      setError((e as Error)?.message ?? "Sync failed. Please try again.");
    }
  };

  const handleToggle = async () => {
    setError(null);
    try {
      await setEnabled.mutateAsync({ id: connection.id, enabled: disabled });
    } catch (e) {
      setError((e as Error)?.message ?? "Could not update the connection.");
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle className="text-base flex items-center gap-2">
              <Building2 className="h-4 w-4" />
              {connection.displayName}
            </CardTitle>
            <CardDescription className="mt-0.5">
              {label} · {connection.externalAccountId}
            </CardDescription>
          </div>
          <StatusBadge status={connection.status} />
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        {error && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        {connection.lastError && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{connection.lastError}</AlertDescription>
          </Alert>
        )}

        <p className="text-xs text-muted-foreground">
          Last import: {formatDate(connection.lastContentSyncAt)}
        </p>

        <div className="flex flex-wrap gap-2">
          <Button
            size="sm"
            onClick={handleSync}
            disabled={busy || disabled}
            className="gap-2"
          >
            <RefreshCw className={sync.isPending ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
            {sync.isPending ? "Importing..." : "Sync now"}
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={handleToggle}
            disabled={busy}
          >
            {disabled ? "Enable" : "Disable"}
          </Button>
          <Button
            size="sm"
            variant="ghost"
            onClick={() => setExpanded((v) => !v)}
            className="gap-1"
          >
            {expanded ? (
              <ChevronDown className="h-4 w-4" />
            ) : (
              <ChevronRight className="h-4 w-4" />
            )}
            Imported listings
          </Button>
        </div>

        {expanded && <ImportedListings connectionId={connection.id} providerLabel={label} />}
      </CardContent>
    </Card>
  );
}

function ImportedListings({
  connectionId,
  providerLabel: label,
}: {
  connectionId: string;
  providerLabel: string;
}) {
  const { data, isLoading } = useChannelListings(connectionId);

  if (isLoading) {
    return <Loader label="Loading imported listings..." />;
  }

  if (!data || data.length === 0) {
    return (
      <p className="rounded-md border border-dashed p-3 text-sm text-muted-foreground">
        Nothing imported yet. Run a sync to pull listings from {label}.
      </p>
    );
  }

  return (
    <ul className="divide-y rounded-md border">
      {data.map((listing) => (
        <li
          key={listing.id}
          className="flex items-center justify-between gap-3 p-3"
        >
          <div className="min-w-0">
            <p className="truncate text-sm font-medium">
              {listing.title ?? listing.providerListingId}
            </p>
            <p className="truncate text-xs text-muted-foreground">
              {listing.providerListingId} · imported{" "}
              {formatDate(listing.lastImportedAt)}
            </p>
          </div>
          {listing.listingId ? (
            <Link
              to={`/app/listings/${listing.listingId}`}
              className={cn(
                buttonVariants({ variant: "outline", size: "sm" }),
                "gap-1 shrink-0",
              )}
            >
              Open draft
              <ExternalLink className="h-3.5 w-3.5" />
            </Link>
          ) : (
            <Badge variant="secondary">Pending</Badge>
          )}
        </li>
      ))}
    </ul>
  );
}

function StatusBadge({ status }: { status: ChannelConnectionStatus | string }) {
  const map: Record<string, { label: string; variant: BadgeProps["variant"] }> = {
    Active: { label: "Active", variant: "success" },
    PendingActivation: { label: "Pending", variant: "secondary" },
    Error: { label: "Error", variant: "destructive" },
    Disabled: { label: "Disabled", variant: "outline" },
  };
  const badge = map[status] ?? { label: status, variant: "outline" as const };
  return <Badge variant={badge.variant}>{badge.label}</Badge>;
}

function formatDate(value: string | null): string {
  if (!value) return "never";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "never";
  return date.toLocaleString();
}

function HowItWorks() {
  return (
    <div className="rounded-lg bg-muted/50 p-4 text-xs text-muted-foreground space-y-2">
      <p className="font-medium text-foreground text-sm">How it works</p>
      <ul className="list-disc pl-4 space-y-1">
        <li>
          Lagedra connects to your PMS and pulls property content, photos, and
          rates.
        </li>
        <li>
          Imported properties land as <strong>draft listings</strong> — review
          the details, add anything missing, and submit each for approval.
        </li>
        <li>
          Once a listing is live and a guest pays through Lagedra, the booking is
          pushed back to your PMS so your calendar stays in sync.
        </li>
        <li>
          Re-run a sync any time to pick up new properties or content changes.
        </li>
      </ul>
    </div>
  );
}

export default ChannelsPage;

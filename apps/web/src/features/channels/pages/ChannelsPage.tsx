import { useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
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
import { usePublicConfigStore } from "@/app/config/publicConfigStore";
import { getApiErrorMessage } from "@/api/errors";
import { DisconnectChannelButton } from "@/features/channels/components/DisconnectChannelButton";
import {
  useChannelConnections,
  useConnectChannel,
  useSyncChannel,
  useSetChannelEnabled,
  useChannelListings,
  useChannelProviders,
  useStartOwnerRezOAuth,
} from "@/features/channels/hooks/useChannels";
import type {
  ChannelConnectionDto,
  ChannelConnectionStatus,
  ChannelListingMapDto,
  ChannelSyncResultDto,
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
const GUESTY_PROVIDER_KEY = "guesty";
const SMOOBU_PROVIDER_KEY = "smoobu";

const PROVIDER_LABELS: Record<string, string> = {
  [OWNERREZ_PROVIDER_KEY]: "OwnerRez",
  [HOSTAWAY_PROVIDER_KEY]: "Hostaway",
  [GUESTY_PROVIDER_KEY]: "Guesty",
  [SMOOBU_PROVIDER_KEY]: "Smoobu",
};

function providerLabel(key: string): string {
  return PROVIDER_LABELS[key.toLowerCase()] ?? key;
}

export function ChannelsPage() {
  const { data: connections, isLoading } = useChannelConnections();
  const preLaunchEnabled = usePublicConfigStore((s) => s.preLaunchEnabled);

  const hostawayConnection = (connections ?? []).find(
    (c) => c.providerKey.toLowerCase() === HOSTAWAY_PROVIDER_KEY,
  );
  const guestyConnection = (connections ?? []).find(
    (c) => c.providerKey.toLowerCase() === GUESTY_PROVIDER_KEY,
  );
  const ownerRezConnection = (connections ?? []).find(
    (c) => c.providerKey.toLowerCase() === OWNERREZ_PROVIDER_KEY,
  );
  const smoobuConnection = (connections ?? []).find(
    (c) => c.providerKey.toLowerCase() === SMOOBU_PROVIDER_KEY,
  );
  const otherConnections = (connections ?? []).filter((c) => {
    const key = c.providerKey.toLowerCase();
    return (
      key !== HOSTAWAY_PROVIDER_KEY &&
      key !== GUESTY_PROVIDER_KEY &&
      key !== OWNERREZ_PROVIDER_KEY &&
      key !== SMOOBU_PROVIDER_KEY
    );
  });

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Import from your PMS</h1>
        <p className="mt-1 text-muted-foreground">
          {preLaunchEnabled
            ? "Connect Hostaway or OwnerRez once and Lagedra will pull your properties in as draft listings. Sync anytime to update existing listings and import new ones. More channels are on the way."
            : "Connect a PMS once, then sync anytime to update existing listings and import new ones. Once live, paid bookings are pushed back to your PMS automatically."}
        </p>
      </div>

      <OwnerRezReturnNotice />

      <div className="grid gap-4">
        {isLoading ? (
          <Loader label="Loading PMS connections..." />
        ) : (
          <>
            {hostawayConnection ? (
              <HostawayConnectedCard connection={hostawayConnection} />
            ) : (
              <ConnectHostawayCard />
            )}
            {ownerRezConnection ? (
              <OwnerRezConnectedCard connection={ownerRezConnection} />
            ) : (
              <ConnectOwnerRezCard />
            )}
            {guestyConnection ? (
              <GuestyConnectedCard connection={guestyConnection} />
            ) : preLaunchEnabled ? (
              <ComingSoonChannelCard providerName="Guesty" />
            ) : (
              <ConnectGuestyCard />
            )}
            {smoobuConnection ? (
              <SmoobuConnectedCard connection={smoobuConnection} />
            ) : preLaunchEnabled ? (
              <ComingSoonChannelCard providerName="Smoobu" />
            ) : (
              <ConnectSmoobuCard />
            )}
          </>
        )}
      </div>

      {otherConnections.length > 0 && (
        <>
          <Separator />
          <div className="space-y-3">
            <h2 className="text-lg font-semibold">Other connections</h2>
            {otherConnections.map((c) => (
              <ConnectionCard key={c.id} connection={c} />
            ))}
          </div>
        </>
      )}

      {!isLoading &&
        !hostawayConnection &&
        !guestyConnection &&
        !ownerRezConnection &&
        !smoobuConnection &&
        otherConnections.length === 0 && (
        <>
          <Separator />
          <EmptyState
            title="No channels connected yet"
            description={
              preLaunchEnabled
                ? "Connect Hostaway or OwnerRez above to import your listings into Lagedra."
                : "Connect Hostaway, Guesty, OwnerRez, or Smoobu above to import your listings into Lagedra."
            }
          />
        </>
      )}

      <HowItWorks />
    </div>
  );
}

/**
 * Reports how the OwnerRez authorization went once OwnerRez has redirected the
 * host back here, and imports their properties straight away so a successful
 * connection lands them on listings rather than on an empty card.
 *
 * The query parameters are cleared immediately so reloading the page does not
 * replay the message.
 */
function OwnerRezReturnNotice() {
  const [searchParams, setSearchParams] = useSearchParams();
  const sync = useSyncChannel();
  const [outcome, setOutcome] = useState<{
    variant: "success" | "destructive";
    message: string;
  } | null>(null);
  const handled = useRef(false);

  const result = searchParams.get("ownerrez");
  const connectionId = searchParams.get("connection");

  useEffect(() => {
    if (!result || handled.current) {
      return;
    }
    handled.current = true;

    const params = new URLSearchParams(searchParams);
    params.delete("ownerrez");
    params.delete("connection");
    params.delete("reason");
    setSearchParams(params, { replace: true });

    if (result === "denied") {
      setOutcome({
        variant: "destructive",
        message:
          "OwnerRez access was not approved, so nothing was connected. You can try again whenever you are ready.",
      });
      return;
    }

    if (result !== "connected" || !connectionId) {
      setOutcome({
        variant: "destructive",
        message:
          "We could not finish connecting OwnerRez. Please start the connection again.",
      });
      return;
    }

    setOutcome({
      variant: "success",
      message: "OwnerRez connected. Importing your properties...",
    });

    sync
      .mutateAsync(connectionId)
      .then((syncResult) => {
        setOutcome({
          variant: "success",
          message: formatOwnerRezSyncSuccess(syncResult, "OwnerRez connected. "),
        });
      })
      .catch((e: unknown) => {
        setOutcome({
          variant: "destructive",
          message: getApiErrorMessage(
            e,
            "OwnerRez is connected, but importing your listings failed. Use Sync from OwnerRez to try again.",
          ),
        });
      });
  }, [result, connectionId, searchParams, setSearchParams, sync]);

  if (!outcome) {
    return null;
  }

  return (
    <Alert variant={outcome.variant}>
      {outcome.variant === "success" ? (
        <CheckCircle2 className="h-4 w-4" />
      ) : (
        <AlertTriangle className="h-4 w-4" />
      )}
      <AlertDescription>{outcome.message}</AlertDescription>
    </Alert>
  );
}

function formatOwnerRezSyncSuccess(
  result: ChannelSyncResultDto,
  prefix: string,
): string {
  const importPart =
    result.pulled > 0
      ? `Imported ${result.created} new and updated ${result.updated} listing(s) as drafts.`
      : "No listings were found yet — try syncing again once properties are active in OwnerRez.";
  return `${prefix}${importPart}`;
}

function OwnerRezConnectedCard({
  connection,
}: {
  connection: ChannelConnectionDto;
}) {
  const sync = useSyncChannel();
  const setEnabled = useSetChannelEnabled();
  const [expanded, setExpanded] = useState(false);
  const listings = useChannelListings(connection.id);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const disabled = connection.status === "Disabled";
  const busy = sync.isPending || setEnabled.isPending;

  const handleSync = async () => {
    setError(null);
    setSuccess(null);
    try {
      const result = await sync.mutateAsync(connection.id);
      setSuccess(
        formatOwnerRezSyncSuccess(
          result,
          "Synced. Updates existing drafts and imports any new OwnerRez properties. ",
        ),
      );
    } catch (e) {
      setError(getApiErrorMessage(e, "Sync failed. Please try again."));
    }
  };

  const handleToggle = async () => {
    setError(null);
    try {
      await setEnabled.mutateAsync({ id: connection.id, enabled: disabled });
    } catch (e) {
      setError(getApiErrorMessage(e, "Could not update the connection."));
    }
  };

  return (
    <Card className="border-primary/20 bg-primary/5">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle className="text-lg flex items-center gap-2">
              <Building2 className="h-5 w-5" />
              OwnerRez connected
            </CardTitle>
            <CardDescription className="mt-0.5">
              {connection.displayName} · {connection.externalAccountId}
            </CardDescription>
          </div>
          <StatusBadge status={connection.status} />
        </div>
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
        {connection.lastError && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{connection.lastError}</AlertDescription>
          </Alert>
        )}

        <p className="text-sm text-muted-foreground">
          Sync updates listings already imported from OwnerRez and pulls in any
          new ones as drafts. Last sync: {formatDate(connection.lastContentSyncAt)}.
        </p>

        <div className="flex flex-wrap gap-2">
          <Button onClick={handleSync} disabled={busy || disabled} className="gap-2">
            <RefreshCw className={sync.isPending ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
            {sync.isPending ? "Syncing..." : "Sync from OwnerRez"}
          </Button>
          <Button variant="outline" onClick={handleToggle} disabled={busy}>
            {disabled ? "Enable" : "Disable"}
          </Button>
          <ImportedListingsToggle
            expanded={expanded}
            count={listings.data?.length}
            onToggle={() => setExpanded((v) => !v)}
          />
          <DisconnectChannelButton
            connectionId={connection.id}
            providerLabel="OwnerRez"
            listingCount={listings.data?.length}
            disabled={busy}
          />
        </div>

        {expanded && (
          <ImportedListingsList
            listings={listings.data}
            isLoading={listings.isLoading}
            providerLabel="OwnerRez"
          />
        )}
      </CardContent>
    </Card>
  );
}

/**
 * OwnerRez can be linked two ways, and which one is live is a server-side
 * decision: once an OwnerRez OAuth app is configured, `usesOAuth` flips and the
 * redirect flow replaces the token form. Until then hosts paste an account email
 * and personal access token.
 *
 * Worth knowing about the token path: OwnerRez rate limits personal access
 * tokens to two different accounts per IP address per 24 hours, so it does not
 * scale past a small pilot — see
 * https://www.ownerrez.com/support/articles/api-auth.
 */
function ConnectOwnerRezCard() {
  const providers = useChannelProviders();
  const usesOAuth = providers.data?.some(
    (p) => p.providerKey.toLowerCase() === OWNERREZ_PROVIDER_KEY && p.usesOAuth,
  );

  // Wait for the answer rather than flashing the wrong card at a host who is
  // part-way through reading the instructions.
  if (providers.isLoading) {
    return (
      <Card>
        <CardContent className="py-6">
          <Loader label="Loading OwnerRez connection options..." />
        </CardContent>
      </Card>
    );
  }

  return usesOAuth ? <ConnectOwnerRezOAuthCard /> : <ConnectOwnerRezTokenCard />;
}

function ConnectOwnerRezOAuthCard() {
  const start = useStartOwnerRezOAuth();
  const [error, setError] = useState<string | null>(null);

  const handleConnect = async () => {
    setError(null);
    try {
      const { authorizationUrl } = await start.mutateAsync();
      window.location.assign(authorizationUrl);
    } catch (e) {
      setError(
        getApiErrorMessage(
          e,
          "Could not start the OwnerRez connection. Please try again.",
        ),
      );
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <Plug className="h-5 w-5" />
          Connect OwnerRez
        </CardTitle>
        <CardDescription>
          Approve Lagedra in your OwnerRez account once. No API keys to copy, and
          you can withdraw access from either side at any time.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {error && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        <ol className="list-decimal space-y-2 pl-4 text-sm text-muted-foreground">
          <li>
            Click <span className="text-foreground">Connect with OwnerRez</span>.
            We&apos;ll send you to OwnerRez to sign in.
          </li>
          <li>
            Approve access for Lagedra. OwnerRez then returns you straight back
            here.
          </li>
          <li>Your active OwnerRez properties are imported as draft listings.</li>
          <li>
            After that, use{" "}
            <span className="text-foreground">Sync from OwnerRez</span> anytime
            to update existing listings and import new ones.
          </li>
        </ol>

        <Button onClick={handleConnect} disabled={start.isPending} className="gap-2">
          <Link2 className="h-4 w-4" />
          {start.isPending ? "Redirecting..." : "Connect with OwnerRez"}
        </Button>
      </CardContent>
    </Card>
  );
}

function ConnectOwnerRezTokenCard() {
  const connect = useConnectChannel();
  const sync = useSyncChannel();

  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [accessToken, setAccessToken] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const busy = connect.isPending || sync.isPending;

  const handleConnect = async () => {
    setError(null);
    setSuccess(null);

    const trimmedEmail = email.trim();
    if (!trimmedEmail) {
      setError("Enter the email address you use to sign in to OwnerRez.");
      return;
    }
    if (!accessToken.trim()) {
      setError("Enter your OwnerRez API key.");
      return;
    }

    try {
      const connection = await connect.mutateAsync({
        providerKey: OWNERREZ_PROVIDER_KEY,
        externalAccountId: trimmedEmail,
        displayName: displayName.trim() || "OwnerRez",
        username: trimmedEmail,
        secret: accessToken.trim(),
      });

      const result = await sync.mutateAsync(connection.id);
      setDisplayName("");
      setEmail("");
      setAccessToken("");
      setSuccess(formatOwnerRezSyncSuccess(result, "Connected. "));
    } catch (e) {
      setError(
        getApiErrorMessage(
          e,
          "Could not connect to OwnerRez. Check your sign-in email and API key.",
        ),
      );
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <Plug className="h-5 w-5" />
          Connect OwnerRez
        </CardTitle>
        <CardDescription>
          Connect once with an OwnerRez API key. After that you&apos;ll only need
          Sync to refresh listings.
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

        <ol className="list-decimal space-y-2 pl-4 text-sm text-muted-foreground">
          <li>
            In OwnerRez, go to{" "}
            <span className="text-foreground">
              Settings → Advanced Tools → Developer/API Settings
            </span>
            .
          </li>
          <li>
            Create an API key (OwnerRez calls it a personal access token) and copy
            it. It starts with <code className="text-[11px]">pt_</code> and is shown
            only once — Lagedra encrypts it and never shows it again.
          </li>
          <li>
            Paste the key below along with the email address you sign in to
            OwnerRez with. OwnerRez identifies the account by that email, not by
            your numeric user id.
          </li>
          <li>
            Click <span className="text-foreground">Connect &amp; import listings</span>.
            Your active OwnerRez properties are imported as draft listings.
          </li>
          <li>
            After connecting, use{" "}
            <span className="text-foreground">Sync from OwnerRez</span> anytime
            to update existing listings and import new ones.
          </li>
        </ol>

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="or-email">OwnerRez sign-in email</Label>
            <Input
              id="or-email"
              type="email"
              placeholder="you@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
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

        <div className="space-y-1.5">
          <Label htmlFor="or-token">API key</Label>
          <Input
            id="or-token"
            type="password"
            placeholder="pt_..."
            value={accessToken}
            onChange={(e) => setAccessToken(e.target.value)}
            autoComplete="off"
          />
        </div>

        <Button onClick={handleConnect} disabled={busy} className="gap-2">
          <Link2 className="h-4 w-4" />
          {busy ? "Connecting & importing..." : "Connect & import listings"}
        </Button>
      </CardContent>
    </Card>
  );
}

function formatHostawaySyncSuccess(
  result: ChannelSyncResultDto,
  prefix: string,
): string {
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
  return `${prefix}${importPart}${webhookPart}`;
}

function HostawayConnectedCard({
  connection,
}: {
  connection: ChannelConnectionDto;
}) {
  const sync = useSyncChannel();
  const setEnabled = useSetChannelEnabled();
  const [expanded, setExpanded] = useState(false);
  const listings = useChannelListings(connection.id);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const disabled = connection.status === "Disabled";
  const busy = sync.isPending || setEnabled.isPending;

  const handleSync = async () => {
    setError(null);
    setSuccess(null);
    try {
      const result = await sync.mutateAsync(connection.id);
      setSuccess(
        formatHostawaySyncSuccess(
          result,
          "Synced. Updates existing drafts and imports any new Hostaway listings. ",
        ),
      );
    } catch (e) {
      setError(getApiErrorMessage(e, "Sync failed. Please try again."));
    }
  };

  const handleToggle = async () => {
    setError(null);
    try {
      await setEnabled.mutateAsync({ id: connection.id, enabled: disabled });
    } catch (e) {
      setError(getApiErrorMessage(e, "Could not update the connection."));
    }
  };

  return (
    <Card className="border-primary/20 bg-primary/5">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle className="text-lg flex items-center gap-2">
              <Building2 className="h-5 w-5" />
              Hostaway connected
            </CardTitle>
            <CardDescription className="mt-0.5">
              {connection.displayName} · account {connection.externalAccountId}
            </CardDescription>
          </div>
          <StatusBadge status={connection.status} />
        </div>
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
        {connection.lastError && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{connection.lastError}</AlertDescription>
          </Alert>
        )}

        <p className="text-sm text-muted-foreground">
          Sync updates listings already imported from Hostaway and pulls in any
          new ones as drafts. Last sync: {formatDate(connection.lastContentSyncAt)}.
        </p>

        <div className="flex flex-wrap gap-2">
          <Button onClick={handleSync} disabled={busy || disabled} className="gap-2">
            <RefreshCw className={sync.isPending ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
            {sync.isPending ? "Syncing..." : "Sync from Hostaway"}
          </Button>
          <Button
            variant="outline"
            onClick={handleToggle}
            disabled={busy}
          >
            {disabled ? "Enable" : "Disable"}
          </Button>
          <ImportedListingsToggle
            expanded={expanded}
            count={listings.data?.length}
            onToggle={() => setExpanded((v) => !v)}
          />
          <DisconnectChannelButton
            connectionId={connection.id}
            providerLabel="Hostaway"
            listingCount={listings.data?.length}
            disabled={busy}
          />
        </div>

        {expanded && (
          <ImportedListingsList
            listings={listings.data}
            isLoading={listings.isLoading}
            providerLabel="Hostaway"
          />
        )}
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

    const trimmedAccountId = accountId.trim();
    if (!trimmedAccountId) {
      setError("Enter your Hostaway account ID.");
      return;
    }
    if (!/^\d+$/.test(trimmedAccountId)) {
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
        externalAccountId: trimmedAccountId,
        displayName: displayName.trim() || "Hostaway",
        secret: clientSecret.trim(),
      });

      const result = await sync.mutateAsync(connection.id);
      setDisplayName("");
      setAccountId("");
      setClientSecret("");
      setSuccess(formatHostawaySyncSuccess(result, "Connected. "));
    } catch (e) {
      setError(
        getApiErrorMessage(
          e,
          "Could not connect to Hostaway. Check your account ID and API secret.",
        ),
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
          Connect once. After that you&apos;ll only need Sync to refresh listings.
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

        <ol className="list-decimal space-y-2 pl-4 text-sm text-muted-foreground">
          <li>
            In Hostaway, go to{" "}
            <span className="text-foreground">Settings → Hostaway API</span> and
            create an API client.
          </li>
          <li>
            Select{" "}
            <span className="text-foreground">Hostaway Public API</span>, then
            click <span className="text-foreground">Create</span>. Your account
            ID is the <code className="text-[11px]">client_id</code>.
          </li>
          <li>
            Copy the generated API secret key and paste it below. Lagedra
            encrypts it and never shows it again.
          </li>
          <li>
            Click <span className="text-foreground">Connect &amp; import listings</span>.
            Your Hostaway properties are imported as draft listings.
          </li>
          <li>
            After connecting, use{" "}
            <span className="text-foreground">Sync from Hostaway</span> anytime
            to update existing listings and import new ones.
          </li>
        </ol>

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="ha-accountId">Hostaway account ID (client_id)</Label>
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
          <Label htmlFor="ha-secret">API secret key</Label>
          <Input
            id="ha-secret"
            type="password"
            placeholder="Paste your Hostaway API secret key"
            value={clientSecret}
            onChange={(e) => setClientSecret(e.target.value)}
            autoComplete="off"
          />
        </div>

        <Button onClick={handleConnect} disabled={busy} className="gap-2">
          <Link2 className="h-4 w-4" />
          {busy ? "Connecting & importing..." : "Connect & import listings"}
        </Button>
      </CardContent>
    </Card>
  );
}

function formatGuestySyncSuccess(
  result: ChannelSyncResultDto,
  prefix: string,
): string {
  const importPart =
    result.pulled > 0
      ? `Imported ${result.created} new and updated ${result.updated} listing(s) as drafts.`
      : "No listings were found yet — try syncing again once listings are active in Guesty.";
  return `${prefix}${importPart}`;
}

function GuestyConnectedCard({
  connection,
}: {
  connection: ChannelConnectionDto;
}) {
  const sync = useSyncChannel();
  const setEnabled = useSetChannelEnabled();
  const [expanded, setExpanded] = useState(false);
  const listings = useChannelListings(connection.id);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const disabled = connection.status === "Disabled";
  const busy = sync.isPending || setEnabled.isPending;

  const handleSync = async () => {
    setError(null);
    setSuccess(null);
    try {
      const result = await sync.mutateAsync(connection.id);
      setSuccess(
        formatGuestySyncSuccess(
          result,
          "Synced. Updates existing drafts and imports any new Guesty listings. ",
        ),
      );
    } catch (e) {
      setError(getApiErrorMessage(e, "Sync failed. Please try again."));
    }
  };

  const handleToggle = async () => {
    setError(null);
    try {
      await setEnabled.mutateAsync({ id: connection.id, enabled: disabled });
    } catch (e) {
      setError(getApiErrorMessage(e, "Could not update the connection."));
    }
  };

  return (
    <Card className="border-primary/20 bg-primary/5">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle className="text-lg flex items-center gap-2">
              <Building2 className="h-5 w-5" />
              Guesty connected
            </CardTitle>
            <CardDescription className="mt-0.5">
              {connection.displayName} · client {connection.externalAccountId}
            </CardDescription>
          </div>
          <StatusBadge status={connection.status} />
        </div>
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
        {connection.lastError && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{connection.lastError}</AlertDescription>
          </Alert>
        )}

        <p className="text-sm text-muted-foreground">
          Sync updates listings already imported from Guesty and pulls in any
          new ones as drafts. Last sync: {formatDate(connection.lastContentSyncAt)}.
        </p>

        <div className="flex flex-wrap gap-2">
          <Button onClick={handleSync} disabled={busy || disabled} className="gap-2">
            <RefreshCw className={sync.isPending ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
            {sync.isPending ? "Syncing..." : "Sync from Guesty"}
          </Button>
          <Button
            variant="outline"
            onClick={handleToggle}
            disabled={busy}
          >
            {disabled ? "Enable" : "Disable"}
          </Button>
          <ImportedListingsToggle
            expanded={expanded}
            count={listings.data?.length}
            onToggle={() => setExpanded((v) => !v)}
          />
          <DisconnectChannelButton
            connectionId={connection.id}
            providerLabel="Guesty"
            listingCount={listings.data?.length}
            disabled={busy}
          />
        </div>

        {expanded && (
          <ImportedListingsList
            listings={listings.data}
            isLoading={listings.isLoading}
            providerLabel="Guesty"
          />
        )}
      </CardContent>
    </Card>
  );
}

function ConnectGuestyCard() {
  const connect = useConnectChannel();
  const sync = useSyncChannel();

  const [displayName, setDisplayName] = useState("");
  const [clientId, setClientId] = useState("");
  const [clientSecret, setClientSecret] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const busy = connect.isPending || sync.isPending;

  const handleConnect = async () => {
    setError(null);
    setSuccess(null);

    const trimmedClientId = clientId.trim();
    if (!trimmedClientId) {
      setError("Enter your Guesty Client ID.");
      return;
    }
    if (!clientSecret.trim()) {
      setError("Enter your Guesty Client Secret.");
      return;
    }

    try {
      const connection = await connect.mutateAsync({
        providerKey: GUESTY_PROVIDER_KEY,
        externalAccountId: trimmedClientId,
        displayName: displayName.trim() || "Guesty",
        secret: clientSecret.trim(),
      });

      const result = await sync.mutateAsync(connection.id);
      setDisplayName("");
      setClientId("");
      setClientSecret("");
      setSuccess(formatGuestySyncSuccess(result, "Connected. "));
    } catch (e) {
      setError(
        getApiErrorMessage(
          e,
          "Could not connect to Guesty. Check your Client ID and Client Secret.",
        ),
      );
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <Plug className="h-5 w-5" />
          Connect Guesty
        </CardTitle>
        <CardDescription>
          Connect once with your Guesty Open API credentials. After that you&apos;ll
          only need Sync to refresh listings.
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

        <ol className="list-decimal space-y-2 pl-4 text-sm text-muted-foreground">
          <li>
            In Guesty, go to{" "}
            <span className="text-foreground">Integrations → API &amp; Webhooks</span>{" "}
            and create a new API application.
          </li>
          <li>
            Copy the <span className="text-foreground">Client ID</span> and{" "}
            <span className="text-foreground">Client Secret</span>. The secret is
            shown only once — paste it below. Lagedra encrypts it and never shows
            it again.
          </li>
          <li>
            Click <span className="text-foreground">Connect &amp; import listings</span>.
            Your Guesty properties are imported as draft listings.
          </li>
          <li>
            After connecting, use{" "}
            <span className="text-foreground">Sync from Guesty</span> anytime to
            update existing listings and import new ones.
          </li>
        </ol>

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="gy-clientId">Guesty Client ID</Label>
            <Input
              id="gy-clientId"
              placeholder="Your Open API Client ID"
              value={clientId}
              onChange={(e) => setClientId(e.target.value)}
              autoComplete="off"
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="gy-displayName">Label (optional)</Label>
            <Input
              id="gy-displayName"
              placeholder="My Guesty account"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              autoComplete="off"
            />
          </div>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="gy-secret">Client Secret</Label>
          <Input
            id="gy-secret"
            type="password"
            placeholder="Paste your Guesty Client Secret"
            value={clientSecret}
            onChange={(e) => setClientSecret(e.target.value)}
            autoComplete="off"
          />
        </div>

        <Button onClick={handleConnect} disabled={busy} className="gap-2">
          <Link2 className="h-4 w-4" />
          {busy ? "Connecting & importing..." : "Connect & import listings"}
        </Button>
      </CardContent>
    </Card>
  );
}

function formatSmoobuSyncSuccess(
  result: ChannelSyncResultDto,
  prefix: string,
): string {
  const importPart =
    result.pulled > 0
      ? `Imported ${result.created} new and updated ${result.updated} listing(s) as drafts.`
      : "No listings were found yet — try syncing again once properties are active in Smoobu.";
  return `${prefix}${importPart}`;
}

function SmoobuConnectedCard({
  connection,
}: {
  connection: ChannelConnectionDto;
}) {
  const sync = useSyncChannel();
  const setEnabled = useSetChannelEnabled();
  const [expanded, setExpanded] = useState(false);
  const listings = useChannelListings(connection.id);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const disabled = connection.status === "Disabled";
  const busy = sync.isPending || setEnabled.isPending;

  const handleSync = async () => {
    setError(null);
    setSuccess(null);
    try {
      const result = await sync.mutateAsync(connection.id);
      setSuccess(
        formatSmoobuSyncSuccess(
          result,
          "Synced. Updates existing drafts and imports any new Smoobu properties. ",
        ),
      );
    } catch (e) {
      setError(getApiErrorMessage(e, "Sync failed. Please try again."));
    }
  };

  const handleToggle = async () => {
    setError(null);
    try {
      await setEnabled.mutateAsync({ id: connection.id, enabled: disabled });
    } catch (e) {
      setError(getApiErrorMessage(e, "Could not update the connection."));
    }
  };

  return (
    <Card className="border-primary/20 bg-primary/5">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle className="text-lg flex items-center gap-2">
              <Building2 className="h-5 w-5" />
              Smoobu connected
            </CardTitle>
            <CardDescription className="mt-0.5">
              {connection.displayName} · {connection.externalAccountId}
            </CardDescription>
          </div>
          <StatusBadge status={connection.status} />
        </div>
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
        {connection.lastError && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{connection.lastError}</AlertDescription>
          </Alert>
        )}

        <p className="text-sm text-muted-foreground">
          Sync updates listings already imported from Smoobu and pulls in any
          new ones as drafts. Last sync: {formatDate(connection.lastContentSyncAt)}.
        </p>

        <div className="flex flex-wrap gap-2">
          <Button onClick={handleSync} disabled={busy || disabled} className="gap-2">
            <RefreshCw className={sync.isPending ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
            {sync.isPending ? "Syncing..." : "Sync from Smoobu"}
          </Button>
          <Button
            variant="outline"
            onClick={handleToggle}
            disabled={busy}
          >
            {disabled ? "Enable" : "Disable"}
          </Button>
          <ImportedListingsToggle
            expanded={expanded}
            count={listings.data?.length}
            onToggle={() => setExpanded((v) => !v)}
          />
          <DisconnectChannelButton
            connectionId={connection.id}
            providerLabel="Smoobu"
            listingCount={listings.data?.length}
            disabled={busy}
          />
        </div>

        {expanded && (
          <ImportedListingsList
            listings={listings.data}
            isLoading={listings.isLoading}
            providerLabel="Smoobu"
          />
        )}
      </CardContent>
    </Card>
  );
}

function ConnectSmoobuCard() {
  const connect = useConnectChannel();
  const sync = useSyncChannel();

  const [displayName, setDisplayName] = useState("");
  const [apiKey, setApiKey] = useState("");
  const [apiSecret, setApiSecret] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const busy = connect.isPending || sync.isPending;

  const handleConnect = async () => {
    setError(null);
    setSuccess(null);

    const trimmedApiKey = apiKey.trim();
    if (!trimmedApiKey) {
      setError("Enter your Smoobu API key.");
      return;
    }
    if (!apiSecret.trim()) {
      setError("Enter your Smoobu API secret.");
      return;
    }

    try {
      const connection = await connect.mutateAsync({
        providerKey: SMOOBU_PROVIDER_KEY,
        externalAccountId: trimmedApiKey,
        displayName: displayName.trim() || "Smoobu",
        secret: apiSecret.trim(),
      });

      const result = await sync.mutateAsync(connection.id);
      setDisplayName("");
      setApiKey("");
      setApiSecret("");
      setSuccess(formatSmoobuSyncSuccess(result, "Connected. "));
    } catch (e) {
      setError(
        getApiErrorMessage(
          e,
          "Could not connect to Smoobu. Check your API key and API secret.",
        ),
      );
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <Plug className="h-5 w-5" />
          Connect Smoobu
        </CardTitle>
        <CardDescription>
          Connect once with a Smoobu API key and secret. After that you&apos;ll
          only need Sync to refresh listings.
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

        <ol className="list-decimal space-y-2 pl-4 text-sm text-muted-foreground">
          <li>
            In Smoobu, go to{" "}
            <span className="text-foreground">
              Settings → Advanced → API Keys
            </span>{" "}
            and click <span className="text-foreground">Create API Key</span>.
          </li>
          <li>
            Use <span className="text-foreground">Generate Secret</span> to
            create the API secret. It is shown only once — paste both below.
            Lagedra encrypts them and never shows them again.
          </li>
          <li>
            Click <span className="text-foreground">Connect &amp; import listings</span>.
            Your Smoobu properties are imported as draft listings.
          </li>
          <li>
            After connecting, use{" "}
            <span className="text-foreground">Sync from Smoobu</span> anytime to
            update existing listings and import new ones.
          </li>
        </ol>

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="sb-apiKey">Smoobu API key</Label>
            <Input
              id="sb-apiKey"
              placeholder="usr_live_..."
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
              autoComplete="off"
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="sb-displayName">Label (optional)</Label>
            <Input
              id="sb-displayName"
              placeholder="My Smoobu account"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              autoComplete="off"
            />
          </div>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="sb-secret">API secret</Label>
          <Input
            id="sb-secret"
            type="password"
            placeholder="Paste your Smoobu API secret"
            value={apiSecret}
            onChange={(e) => setApiSecret(e.target.value)}
            autoComplete="off"
          />
        </div>

        <Button onClick={handleConnect} disabled={busy} className="gap-2">
          <Link2 className="h-4 w-4" />
          {busy ? "Connecting & importing..." : "Connect & import listings"}
        </Button>
      </CardContent>
    </Card>
  );
}

/**
 * Placeholder for channels that exist in the product but are still being
 * tested during pre-launch. Shown instead of the connect form so hosts know
 * the integration is on the way without being able to connect yet.
 */
function ComingSoonChannelCard({ providerName }: { providerName: string }) {
  return (
    <Card className="border-dashed">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle className="text-lg flex items-center gap-2 text-muted-foreground">
              <Plug className="h-5 w-5" />
              Connect {providerName}
            </CardTitle>
            <CardDescription className="mt-0.5">
              We&apos;re finishing testing the {providerName} integration — it
              will open up at launch.
            </CardDescription>
          </div>
          <Badge variant="secondary">Coming soon</Badge>
        </div>
      </CardHeader>
    </Card>
  );
}

function ConnectionCard({ connection }: { connection: ChannelConnectionDto }) {
  const sync = useSyncChannel();
  const setEnabled = useSetChannelEnabled();
  const [expanded, setExpanded] = useState(false);
  const listings = useChannelListings(connection.id);
  const [error, setError] = useState<string | null>(null);

  const disabled = connection.status === "Disabled";
  const busy = sync.isPending || setEnabled.isPending;
  const label = providerLabel(connection.providerKey);

  const handleSync = async () => {
    setError(null);
    try {
      await sync.mutateAsync(connection.id);
    } catch (e) {
      setError(getApiErrorMessage(e, "Sync failed. Please try again."));
    }
  };

  const handleToggle = async () => {
    setError(null);
    try {
      await setEnabled.mutateAsync({ id: connection.id, enabled: disabled });
    } catch (e) {
      setError(getApiErrorMessage(e, "Could not update the connection."));
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
          <ImportedListingsToggle
            expanded={expanded}
            count={listings.data?.length}
            onToggle={() => setExpanded((v) => !v)}
            size="sm"
          />
          <DisconnectChannelButton
            connectionId={connection.id}
            providerLabel={label}
            listingCount={listings.data?.length}
            disabled={busy}
            size="sm"
          />
        </div>

        {expanded && (
          <ImportedListingsList
            listings={listings.data}
            isLoading={listings.isLoading}
            providerLabel={label}
          />
        )}
      </CardContent>
    </Card>
  );
}

/**
 * Collapsible trigger for a connection's imported listings. Collapsed by
 * default so a large import does not push the other channel cards off screen.
 */
function ImportedListingsToggle({
  expanded,
  count,
  onToggle,
  size,
}: {
  expanded: boolean;
  count?: number;
  onToggle: () => void;
  size?: "sm";
}) {
  return (
    <Button
      variant="ghost"
      size={size}
      onClick={onToggle}
      aria-expanded={expanded}
      className="gap-1"
    >
      {expanded ? (
        <ChevronDown className="h-4 w-4" />
      ) : (
        <ChevronRight className="h-4 w-4" />
      )}
      {expanded ? "Hide" : "Show"} imported listings
      {count === undefined ? "" : ` (${count})`}
    </Button>
  );
}

function ImportedListingsList({
  listings,
  isLoading,
  providerLabel: label,
}: {
  listings: ChannelListingMapDto[] | undefined;
  isLoading: boolean;
  providerLabel: string;
}) {
  if (isLoading) {
    return <Loader label="Loading imported listings..." />;
  }

  if (!listings || listings.length === 0) {
    return (
      <p className="rounded-md border border-dashed p-3 text-sm text-muted-foreground">
        Nothing imported yet. Run a sync to pull listings from {label}.
      </p>
    );
  }

  return (
    <ul className="max-h-80 divide-y overflow-y-auto rounded-md border">
      {listings.map((listing) => (
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
          Sync anytime to update listings you already imported and pull in new
          ones from your PMS.
        </li>
        <li>
          One connection per PMS. To switch accounts or replace an API key,
          disconnect the existing connection and connect again — your imported
          listings stay put.
        </li>
      </ul>
    </div>
  );
}

export default ChannelsPage;

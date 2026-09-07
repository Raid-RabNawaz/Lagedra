import { useMemo } from "react";
import {
  Home,
  Users,
  CalendarRange,
  Receipt,
  ScrollText,
  ShieldCheck,
  BadgeCheck,
  MessageSquareQuote,
  Scale,
  HandCoins,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { formatDate, formatMoney } from "@/utils/format";

/**
 * Renders the sealed Truth Surface agreement as a clean, human-readable
 * document for the two signing parties.
 *
 * The canonical content is a deterministic JSON payload built for the
 * cryptographic hash — it carries internal plumbing (schema/protocol/pack
 * versions, raw UUIDs, ISO timestamps, consent fingerprints) that must never
 * be shown to end users. Rather than blindly flattening every key (which
 * leaked that plumbing and one party's IP/User-Agent to the other), we map a
 * curated allow-list of fields into titled sections.
 */

type Json = Record<string, unknown>;

function asObject(value: unknown): Json | undefined {
  return value !== null && typeof value === "object" && !Array.isArray(value)
    ? (value as Json)
    : undefined;
}

function asString(value: unknown): string | undefined {
  return typeof value === "string" && value.trim().length > 0 ? value : undefined;
}

function asNumber(value: unknown): number | undefined {
  return typeof value === "number" && Number.isFinite(value) ? value : undefined;
}

function asBool(value: unknown): boolean | undefined {
  return typeof value === "boolean" ? value : undefined;
}

function asArray(value: unknown): unknown[] {
  return Array.isArray(value) ? value : [];
}

function titleCase(token: string): string {
  return token
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/[_-]/g, " ")
    .replace(/\b\w/g, (c) => c.toUpperCase())
    .trim();
}

function formatAddress(addr: Json | undefined): string | undefined {
  if (!addr) return undefined;
  const parts = [
    asString(addr.street),
    asString(addr.city),
    [asString(addr.state), asString(addr.zipCode)].filter(Boolean).join(" ").trim() || undefined,
    asString(addr.country),
  ].filter((p): p is string => Boolean(p && p.length > 0));
  return parts.length > 0 ? parts.join(", ") : undefined;
}

function Row({
  label,
  value,
}: {
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="flex items-start justify-between gap-4 py-2 text-sm">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium text-right">{value}</span>
    </div>
  );
}

function Section({
  icon,
  title,
  children,
}: {
  icon: React.ReactNode;
  title: string;
  children: React.ReactNode;
}) {
  return (
    <section className="rounded-lg border bg-card">
      <div className="flex items-center gap-2 border-b px-4 py-3">
        <span className="text-muted-foreground">{icon}</span>
        <h3 className="text-sm font-semibold">{title}</h3>
      </div>
      <div className="divide-y px-4 py-1">{children}</div>
    </section>
  );
}

function VerificationBadges({ party }: { party: Json }) {
  const flags: { ok: boolean | undefined; label: string }[] = [
    { ok: asBool(party.isIdentityVerified), label: "Identity verified" },
    { ok: asBool(party.isGovernmentIdVerified), label: "Government ID" },
    { ok: asBool(party.isPhoneVerified), label: "Phone verified" },
    { ok: asBool(party.isBackgroundCheckPassed), label: "Background check" },
  ];
  const active = flags.filter((f) => f.ok);
  if (active.length === 0) {
    return <span className="text-xs text-muted-foreground">No verifications on record</span>;
  }
  return (
    <div className="flex flex-wrap justify-end gap-1.5">
      {active.map((f) => (
        <Badge key={f.label} variant="secondary" className="gap-1 text-[10px]">
          <BadgeCheck className="h-3 w-3" />
          {f.label}
        </Badge>
      ))}
    </div>
  );
}

function PartyRow({ role, party }: { role: string; party: Json | undefined }) {
  if (!party) return null;
  const name = asString(party.displayName) ?? "—";
  const memberSince = asString(party.memberSince);
  const tier = asString(party.protectionTier);
  const endorsements = asArray(party.partnerEndorsements)
    .map((e) => asString(asObject(e)?.organizationName))
    .filter((n): n is string => Boolean(n));

  return (
    <div className="py-3">
      <div className="flex items-center justify-between gap-4">
        <div>
          <p className="text-xs uppercase tracking-wide text-muted-foreground">{role}</p>
          <p className="text-sm font-medium">{name}</p>
          {memberSince && (
            <p className="text-xs text-muted-foreground">
              Member since {formatDate(memberSince)}
            </p>
          )}
        </div>
        <VerificationBadges party={party} />
      </div>
      {(tier || endorsements.length > 0) && (
        <div className="mt-2 flex flex-wrap gap-1.5">
          {tier && (
            <Badge variant="outline" className="gap-1 text-[10px]">
              <ShieldCheck className="h-3 w-3" />
              {titleCase(tier)}
            </Badge>
          )}
          {endorsements.map((org) => (
            <Badge key={org} variant="accent" className="text-[10px]">
              Endorsed by {org}
            </Badge>
          ))}
        </div>
      )}
    </div>
  );
}

type Props = {
  canonicalContent: string | null | undefined;
  /** When set, only the headline terms render (used in the compact checkout panel). */
  compact?: boolean;
};

export const AgreementDocument = ({ canonicalContent, compact = false }: Props) => {
  const data = useMemo<Json | null>(() => {
    if (!canonicalContent) return null;
    try {
      return JSON.parse(canonicalContent) as Json;
    } catch {
      return null;
    }
  }, [canonicalContent]);

  if (!data) {
    return (
      <p className="text-sm text-muted-foreground">
        The agreement terms are not available to display.
      </p>
    );
  }

  const listing = asObject(data.listing);
  const parties = asObject(data.parties);
  const dates = asObject(data.dates);
  const financials = asObject(data.financials);
  const depositReturn = asObject(data.depositReturnTerms);
  const cancellation = asObject(data.cancellationPolicy);
  const houseRules = asObject(data.houseRules);
  const jurisdiction = asObject(data.jurisdiction);
  const consent = asObject(data.consent);
  const tenantMessage = asString(data.tenantMessage);

  const address = formatAddress(asObject(listing?.address));

  const money = (key: string): string | undefined => {
    const cents = asNumber(financials?.[key]);
    return cents === undefined ? undefined : formatMoney(cents);
  };

  const financialRows: { label: string; value: string | undefined }[] = [
    { label: "First month's rent", value: money("firstMonthRentCents") },
    { label: "Security deposit", value: money("depositAmountCents") },
    { label: "Stay protection", value: money("insuranceFeeCents") },
    { label: "Service fee", value: money("serviceFeeCents") },
    { label: "Protocol fee (monthly)", value: money("monthlyProtocolFeeCents") },
  ].filter((r) => r.value !== undefined);

  const totalDue = money("totalDueAtCheckoutCents");

  return (
    <div className="space-y-4">
      {/* Property */}
      {listing && (
        <Section icon={<Home className="h-4 w-4" />} title="Property">
          {asString(listing.title) && <Row label="Listing" value={asString(listing.title)} />}
          {asString(listing.propertyType) && (
            <Row label="Type" value={titleCase(asString(listing.propertyType)!)} />
          )}
          {(asNumber(listing.bedrooms) !== undefined ||
            asNumber(listing.bathrooms) !== undefined) && (
            <Row
              label="Layout"
              value={[
                asNumber(listing.bedrooms) !== undefined
                  ? `${listing.bedrooms} bd`
                  : null,
                asNumber(listing.bathrooms) !== undefined
                  ? `${listing.bathrooms} ba`
                  : null,
                asNumber(listing.squareFootage) !== undefined
                  ? `${listing.squareFootage} sq ft`
                  : null,
              ]
                .filter(Boolean)
                .join(" · ")}
            />
          )}
          <Row label="Address" value={address ?? "—"} />
        </Section>
      )}

      {/* Stay */}
      {dates && (
        <Section icon={<CalendarRange className="h-4 w-4" />} title="Stay">
          {asString(dates.checkIn) && (
            <Row label="Check-in" value={formatDate(asString(dates.checkIn)!)} />
          )}
          {asString(dates.checkOut) && (
            <Row label="Check-out" value={formatDate(asString(dates.checkOut)!)} />
          )}
          {asNumber(dates.stayDurationDays) !== undefined && (
            <Row label="Duration" value={`${dates.stayDurationDays} days`} />
          )}
          {asNumber(dates.guestCount) !== undefined && (
            <Row label="Guests" value={String(dates.guestCount)} />
          )}
        </Section>
      )}

      {/* Financials */}
      {(financialRows.length > 0 || totalDue) && (
        <Section icon={<Receipt className="h-4 w-4" />} title="Financial terms">
          {financialRows.map((r) => (
            <Row key={r.label} label={r.label} value={r.value} />
          ))}
          {totalDue && (
            <Row
              label="Total due now"
              value={<span className="text-base font-semibold">{totalDue}</span>}
            />
          )}
        </Section>
      )}

      {/* Deposit return — non-custodial contract, sealed at booking. */}
      {depositReturn && (
        <Section icon={<HandCoins className="h-4 w-4" />} title="Deposit return">
          <Row label="Deposit held by" value="Host" />
          <Row label="Returned by" value="Host, directly" />
          {asNumber(depositReturn.returnWindowDays) !== undefined && (
            <Row
              label="Return window"
              value={`Within ${depositReturn.returnWindowDays} days of move-out`}
            />
          )}
          <div className="py-2 text-sm text-muted-foreground">
            Lagedra never holds the deposit. The tenant pays it directly to the
            host, who holds it for the stay and returns it directly after
            move-out, less any agreed or arbitrated deductions. By law the host
            must return the deposit — or provide an itemized statement of
            deductions — within the return window above. If the host returns
            less than the full deposit, they must provide a valid reason and a
            photo of the damage. The booking is only marked complete once both
            parties confirm the deposit was returned by the host and received by
            the tenant. Any shortfall or dispute is resolved through arbitration.
          </div>
        </Section>
      )}

      {!compact && (
        <>
          {/* Parties */}
          {parties && (asObject(parties.landlord) || asObject(parties.tenant)) && (
            <Section icon={<Users className="h-4 w-4" />} title="Parties">
              <PartyRow role="Host" party={asObject(parties.landlord)} />
              <PartyRow role="Tenant" party={asObject(parties.tenant)} />
            </Section>
          )}

          {/* Tenant message */}
          {tenantMessage && (
            <Section
              icon={<MessageSquareQuote className="h-4 w-4" />}
              title="Message from tenant"
            >
              <p className="py-2 text-sm">{tenantMessage}</p>
            </Section>
          )}

          {/* Policies */}
          {(cancellation || houseRules) && (
            <Section icon={<ScrollText className="h-4 w-4" />} title="Policies & house rules">
              {cancellation && asString(cancellation.type) && (
                <Row label="Cancellation policy" value={titleCase(asString(cancellation.type)!)} />
              )}
              {cancellation && asNumber(cancellation.freeCancellationDays) !== undefined && (
                <Row
                  label="Free cancellation window"
                  value={`${cancellation.freeCancellationDays} days`}
                />
              )}
              {houseRules && asString(houseRules.checkInTime) && (
                <Row label="Check-in time" value={asString(houseRules.checkInTime)} />
              )}
              {houseRules && asString(houseRules.checkOutTime) && (
                <Row label="Check-out time" value={asString(houseRules.checkOutTime)} />
              )}
              {houseRules && asNumber(houseRules.maxGuests) !== undefined && (
                <Row label="Maximum guests" value={String(houseRules.maxGuests)} />
              )}
              {houseRules && asBool(houseRules.petsAllowed) !== undefined && (
                <Row label="Pets" value={houseRules.petsAllowed ? "Allowed" : "Not allowed"} />
              )}
              {houseRules && asBool(houseRules.smokingAllowed) !== undefined && (
                <Row
                  label="Smoking"
                  value={houseRules.smokingAllowed ? "Allowed" : "Not allowed"}
                />
              )}
              {houseRules && asBool(houseRules.partiesAllowed) !== undefined && (
                <Row
                  label="Parties / events"
                  value={houseRules.partiesAllowed ? "Allowed" : "Not allowed"}
                />
              )}
              {houseRules && asString(houseRules.additionalRules) && (
                <div className="py-2 text-sm">
                  <p className="text-muted-foreground">Additional rules</p>
                  <p className="mt-1">{asString(houseRules.additionalRules)}</p>
                </div>
              )}
            </Section>
          )}

          {/* Jurisdiction */}
          {jurisdiction &&
            (asString(jurisdiction.code) || asString(jurisdiction.warning)) && (
              <Section icon={<Scale className="h-4 w-4" />} title="Governing terms">
                {asString(jurisdiction.code) && (
                  <Row label="Jurisdiction" value={asString(jurisdiction.code)} />
                )}
                {asString(jurisdiction.warning) && (
                  <div className="py-2 text-sm text-amber-700">
                    {asString(jurisdiction.warning)}
                  </div>
                )}
              </Section>
            )}

          {/* Consent — who agreed and when. No IP/User-Agent/IDs. */}
          {consent && (asObject(consent.tenant) || asObject(consent.host)) && (
            <Section icon={<ShieldCheck className="h-4 w-4" />} title="Consent record">
              {asString(asObject(consent.tenant)?.consentedAt) && (
                <Row
                  label="Tenant agreed"
                  value={formatDate(asString(asObject(consent.tenant)?.consentedAt)!)}
                />
              )}
              {asString(asObject(consent.host)?.consentedAt) && (
                <Row
                  label="Host agreed"
                  value={formatDate(asString(asObject(consent.host)?.consentedAt)!)}
                />
              )}
            </Section>
          )}
        </>
      )}

      {compact && (
        <>
          <Separator />
          <p className="text-xs text-muted-foreground">
            This is a summary of the headline terms. Open the full agreement to
            review every clause before you confirm.
          </p>
        </>
      )}
    </div>
  );
};

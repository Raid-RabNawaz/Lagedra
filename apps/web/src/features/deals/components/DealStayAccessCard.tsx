import { useQuery } from "@tanstack/react-query";
import { Mail, MapPin, Phone, UserRound } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Loader } from "@/components/shared/Loader";
import { dealApi } from "@/features/deals/services/dealApi";
import type { DealPhase } from "@/api/types";

const UNLOCK_PHASES: DealPhase[] = ["Active", "AwaitingDepositReturn", "Closed"];

type Props = {
  dealId: string;
  dealPhase: DealPhase;
  /** "Host" for the tenant view, "Guest" for the host view. */
  counterpartLabel: string;
};

/**
 * Shows the property's full address and the other party's contact details
 * once the booking is confirmed. Before that, a short locked message explains
 * when these unlock.
 */
export function DealStayAccessCard({ dealId, dealPhase, counterpartLabel }: Props) {
  const eligible = UNLOCK_PHASES.includes(dealPhase);
  const query = useQuery({
    queryKey: ["deal-stay-access", dealId],
    queryFn: () => dealApi.getStayAccess(dealId),
    enabled: eligible,
    staleTime: 60_000,
  });

  if (!eligible) {
    return (
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Stay details</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            The full property address and {counterpartLabel.toLowerCase()} contact
            details unlock after the booking is confirmed (deposit payment clears).
          </p>
        </CardContent>
      </Card>
    );
  }

  if (query.isLoading) {
    return (
      <Card>
        <CardContent className="py-6">
          <Loader label="Loading stay details…" />
        </CardContent>
      </Card>
    );
  }

  if (query.isError || !query.data) {
    return (
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Stay details</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-destructive">
            Could not load address and contact details. Try refreshing the page.
          </p>
        </CardContent>
      </Card>
    );
  }

  const { isUnlocked, lockedReason, propertyAddress, counterpart } = query.data;

  if (!isUnlocked) {
    return (
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Stay details</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            {lockedReason ??
              "Full address and contact details unlock after the booking is confirmed."}
          </p>
        </CardContent>
      </Card>
    );
  }

  const fullAddress = propertyAddress
    ? [
        propertyAddress.street,
        propertyAddress.city,
        [propertyAddress.state, propertyAddress.zipCode].filter(Boolean).join(" "),
        propertyAddress.country,
      ]
        .filter(Boolean)
        .join(", ")
    : null;

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base">Stay details</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-1.5">
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Property address
          </p>
          {fullAddress ? (
            <p className="flex items-start gap-2 text-sm">
              <MapPin className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
              <span>{fullAddress}</span>
            </p>
          ) : (
            <p className="text-sm text-muted-foreground">
              Address not available yet. Ask the host if it&apos;s missing from their listing.
            </p>
          )}
        </div>

        <div className="space-y-1.5 border-t pt-4">
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            {counterpartLabel} contact
          </p>
          {counterpart ? (
            <div className="space-y-1.5 text-sm">
              <p className="flex items-center gap-2">
                <UserRound className="h-4 w-4 shrink-0 text-muted-foreground" />
                <span className="font-medium">{counterpart.fullName}</span>
              </p>
              {counterpart.email && (
                <p className="flex items-center gap-2">
                  <Mail className="h-4 w-4 shrink-0 text-muted-foreground" />
                  <a
                    href={`mailto:${counterpart.email}`}
                    className="text-accent hover:underline"
                  >
                    {counterpart.email}
                  </a>
                </p>
              )}
              {counterpart.phone && (
                <p className="flex items-center gap-2">
                  <Phone className="h-4 w-4 shrink-0 text-muted-foreground" />
                  <a
                    href={`tel:${counterpart.phone}`}
                    className="text-accent hover:underline"
                  >
                    {counterpart.phone}
                  </a>
                </p>
              )}
              {!counterpart.email && !counterpart.phone && (
                <p className="text-sm text-muted-foreground">
                  No email or phone on file for this {counterpartLabel.toLowerCase()} yet.
                </p>
              )}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">
              Contact details are not available.
            </p>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

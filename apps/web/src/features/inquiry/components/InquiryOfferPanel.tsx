import { type FormEvent, useState } from "react";
import { CheckCircle2, Handshake, Send } from "lucide-react";
import { Alert } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { getApiErrorMessage } from "@/api/errors";
import type { InquiryOfferDto, ListingDetailsDto } from "@/api/types";
import { formatDate, formatMoney } from "@/utils/format";
import {
  useAcceptInquiryOffer,
  useCounterInquiryOffer,
  useProposeInquiryOffer,
  useWithdrawAcceptedInquiryOffer,
} from "@/features/inquiry/hooks/useInquiry";

type Props = {
  sessionId: string;
  offers: InquiryOfferDto[];
  acceptedOffer: InquiryOfferDto | null;
  listing: ListingDetailsDto | undefined;
  isTenant: boolean;
  isLandlord: boolean;
  isOpen: boolean;
  canNegotiate: boolean;
};

function centsFromDollarsInput(value: string): number | null {
  const trimmed = value.trim();
  if (!trimmed) return null;
  const dollars = Number(trimmed);
  if (!Number.isFinite(dollars) || dollars < 0) return null;
  return Math.round(dollars * 100);
}

function dollarsFromCents(cents: number): string {
  return (cents / 100).toFixed(cents % 100 === 0 ? 0 : 2);
}

const statusLabel: Record<InquiryOfferDto["status"], string> = {
  Pending: "Pending",
  Accepted: "Accepted",
  Superseded: "Superseded",
  Withdrawn: "Withdrawn",
};

/**
 * Rent/deposit offer negotiation on an inquiry thread. Either party can
 * propose; the other accepts or counters. An accepted offer feeds Apply pricing.
 */
export const InquiryOfferPanel = ({
  sessionId,
  offers,
  acceptedOffer,
  listing,
  isTenant,
  isLandlord,
  isOpen,
  canNegotiate,
}: Props) => {
  const propose = useProposeInquiryOffer();
  const accept = useAcceptInquiryOffer();
  const counter = useCounterInquiryOffer();
  const withdraw = useWithdrawAcceptedInquiryOffer();

  const pending = offers.find((o) => o.status === "Pending") ?? null;
  const canAct = isOpen && canNegotiate && (isTenant || isLandlord);

  const defaultRent = listing ? dollarsFromCents(listing.monthlyRentCents) : "";
  const defaultDeposit = listing ? dollarsFromCents(listing.maxDepositCents) : "";

  const [rentInput, setRentInput] = useState(defaultRent);
  const [depositInput, setDepositInput] = useState(defaultDeposit);
  const [note, setNote] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [mode, setMode] = useState<"propose" | "counter" | null>(null);

  const mutationError =
    propose.error ?? accept.error ?? counter.error ?? withdraw.error;

  const submitAmounts = (onValid: (rentCents: number, depositCents: number) => void) => {
    const rentCents = centsFromDollarsInput(rentInput);
    const depositCents = centsFromDollarsInput(depositInput);
    if (rentCents == null || rentCents <= 0) {
      setFormError("Enter a rent greater than zero.");
      return;
    }
    if (depositCents == null || depositCents < 0) {
      setFormError("Enter a valid deposit (0 or more).");
      return;
    }
    if (listing && depositCents > listing.maxDepositCents) {
      setFormError(
        `Deposit cannot exceed the listing maximum of ${formatMoney(listing.maxDepositCents)}.`,
      );
      return;
    }
    setFormError(null);
    onValid(rentCents, depositCents);
  };

  const onPropose = (e: FormEvent) => {
    e.preventDefault();
    submitAmounts((rentCents, depositCents) => {
      propose.mutate(
        {
          sessionId,
          payload: { rentCents, depositCents, note: note.trim() || null },
        },
        {
          onSuccess: () => {
            setMode(null);
            setNote("");
          },
        },
      );
    });
  };

  const onCounter = (e: FormEvent) => {
    e.preventDefault();
    if (!pending) return;
    submitAmounts((rentCents, depositCents) => {
      counter.mutate(
        {
          sessionId,
          offerId: pending.offerId,
          payload: { rentCents, depositCents, note: note.trim() || null },
        },
        {
          onSuccess: () => {
            setMode(null);
            setNote("");
          },
        },
      );
    });
  };

  const pendingIsMine =
    pending &&
    ((isTenant && pending.proposedByRole === "Tenant") ||
      (isLandlord && pending.proposedByRole === "Host"));

  const pendingIsTheirs = pending && !pendingIsMine;

  return (
    <Card className="mb-6">
      <CardHeader className="pb-2">
        <div className="flex items-center gap-2">
          <Handshake className="h-4 w-4 text-muted-foreground" />
          <CardTitle className="text-base">Rent &amp; deposit offers</CardTitle>
        </div>
        <p className="text-xs text-muted-foreground">
          Negotiate terms before applying. An accepted offer becomes the rent and
          deposit charged for this booking.
        </p>
      </CardHeader>
      <CardContent className="space-y-4">
        {acceptedOffer && (
          <Alert className="border-emerald-300 bg-emerald-50 text-emerald-900">
            <CheckCircle2 className="h-4 w-4" />
            <span className="ml-2 text-sm">
              Agreed: rent {formatMoney(acceptedOffer.rentCents)}, deposit{" "}
              {formatMoney(acceptedOffer.depositCents)}. These amounts will be used
              when you apply.
            </span>
          </Alert>
        )}

        {offers.length === 0 && (
          <p className="text-sm text-muted-foreground">
            No offers yet. {canAct ? "Propose rent and deposit below." : null}
          </p>
        )}

        {offers.length > 0 && (
          <ul className="space-y-3">
            {[...offers].reverse().map((offer) => (
              <li
                key={offer.offerId}
                className="rounded-md border p-3 text-sm space-y-1"
              >
                <div className="flex flex-wrap items-center gap-2">
                  <Badge
                    variant={
                      offer.status === "Accepted"
                        ? "default"
                        : offer.status === "Pending"
                          ? "secondary"
                          : "outline"
                    }
                  >
                    {statusLabel[offer.status] ?? offer.status}
                  </Badge>
                  <span className="text-xs text-muted-foreground">
                    {offer.proposedByRole} · {formatDate(offer.proposedAt)}
                  </span>
                </div>
                <p className="font-medium">
                  Rent {formatMoney(offer.rentCents)} · Deposit{" "}
                  {formatMoney(offer.depositCents)}
                </p>
                {offer.note && (
                  <p className="text-xs text-muted-foreground whitespace-pre-wrap">
                    {offer.note}
                  </p>
                )}
              </li>
            ))}
          </ul>
        )}

        {(mutationError || formError) && (
          <Alert variant="destructive" className="text-sm">
            {formError ?? getApiErrorMessage(mutationError, "Something went wrong.")}
          </Alert>
        )}

        {canAct && acceptedOffer && (
          <Button
            variant="outline"
            size="sm"
            disabled={withdraw.isPending}
            onClick={() => withdraw.mutate(sessionId)}
          >
            Withdraw accepted offer
          </Button>
        )}

        {canAct && !acceptedOffer && pendingIsTheirs && pending && (
          <div className="flex flex-wrap gap-2">
            <Button
              size="sm"
              disabled={accept.isPending}
              onClick={() =>
                accept.mutate({ sessionId, offerId: pending.offerId })
              }
            >
              Accept offer
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => {
                setMode("counter");
                setRentInput(dollarsFromCents(pending.rentCents));
                setDepositInput(dollarsFromCents(pending.depositCents));
              }}
            >
              Counter
            </Button>
          </div>
        )}

        {canAct && !acceptedOffer && !pending && mode !== "propose" && (
          <Button
            size="sm"
            variant="outline"
            className="gap-1.5"
            onClick={() => {
              setMode("propose");
              setRentInput(defaultRent);
              setDepositInput(defaultDeposit);
            }}
          >
            <Send className="h-3.5 w-3.5" />
            {isLandlord ? "Send offer" : "Propose terms"}
          </Button>
        )}

        {canAct && !acceptedOffer && pendingIsMine && (
          <p className="text-xs text-muted-foreground">
            Waiting for the other party to accept or counter your offer.
          </p>
        )}

        {canAct && !acceptedOffer && (mode === "propose" || mode === "counter") && (
          <form
            className="space-y-3 rounded-md border p-3"
            onSubmit={mode === "counter" ? onCounter : onPropose}
          >
            <p className="text-sm font-medium">
              {mode === "counter" ? "Counter offer" : "Propose offer"}
            </p>
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label htmlFor="offer-rent">Monthly rent (USD)</Label>
                <Input
                  id="offer-rent"
                  inputMode="decimal"
                  value={rentInput}
                  onChange={(e) => setRentInput(e.target.value)}
                  placeholder="2000"
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="offer-deposit">Deposit (USD)</Label>
                <Input
                  id="offer-deposit"
                  inputMode="decimal"
                  value={depositInput}
                  onChange={(e) => setDepositInput(e.target.value)}
                  placeholder="4000"
                />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="offer-note">Note (optional)</Label>
              <Textarea
                id="offer-note"
                value={note}
                onChange={(e) => setNote(e.target.value)}
                maxLength={500}
                rows={2}
                placeholder="Short note about this offer"
              />
            </div>
            <div className="flex gap-2">
              <Button
                type="submit"
                size="sm"
                disabled={propose.isPending || counter.isPending}
              >
                {mode === "counter" ? "Send counter" : "Send offer"}
              </Button>
              <Button
                type="button"
                size="sm"
                variant="ghost"
                onClick={() => {
                  setMode(null);
                  setFormError(null);
                }}
              >
                Cancel
              </Button>
            </div>
          </form>
        )}
      </CardContent>
    </Card>
  );
};

import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import {
  ArrowLeft,
  Receipt,
  CalendarCheck,
  DollarSign,
  Wallet,
  AlertTriangle,
  Info,
} from "lucide-react";
import { useHostBillingStatement } from "@/features/activation-billing/hooks/useBilling";
import { InvoiceStatusBadge } from "@/features/activation-billing/components/InvoiceStatusBadge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Alert } from "@/components/ui/alert";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";
import { getApiErrorMessage } from "@/api/errors";
import { formatDate, formatMoney } from "@/utils/format";

export const HostBillingStatementPage = () => {
  const { data, isLoading, error } = useHostBillingStatement();

  if (isLoading) {
    return <Loader fullPage label="Loading your statement..." />;
  }

  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      <Link
        to="/app"
        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-6"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to dashboard
      </Link>

      <div className="mb-6">
        <h1 className="text-2xl font-bold tracking-tight">Platform fees</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Lagedra charges a monthly platform fee for each active booking,
          collected automatically from your card on file. Here's what you're
          billed and every deduction to date.
        </p>
      </div>

      {error && (
        <Alert variant="destructive" className="mb-6">
          <AlertTriangle className="h-4 w-4" />
          <span className="ml-2 text-sm">
            {getApiErrorMessage(error, "Failed to load your billing statement.")}
          </span>
        </Alert>
      )}

      {data && (
        <>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4 mb-6">
            <SummaryStat
              icon={<CalendarCheck className="h-4 w-4" />}
              label="Active bookings"
              value={String(data.activeBookingCount)}
            />
            <SummaryStat
              icon={<DollarSign className="h-4 w-4" />}
              label="Fee per booking"
              value={`${formatMoney(data.currentMonthlyFeeCents)}/mo`}
            />
            <SummaryStat
              icon={<Wallet className="h-4 w-4" />}
              label="Projected monthly total"
              value={`${formatMoney(data.projectedMonthlyTotalCents)}/mo`}
              emphasis
            />
            <SummaryStat
              icon={<Receipt className="h-4 w-4" />}
              label="Paid to date"
              value={formatMoney(data.totalPaidToDateCents)}
            />
          </div>

          {data.totalOutstandingCents > 0 && (
            <Alert className="border-amber-200 bg-amber-50 text-amber-800 mb-6">
              <AlertTriangle className="h-4 w-4" />
              <span className="ml-2 text-sm">
                You have {formatMoney(data.totalOutstandingCents)} in pending or
                failed platform-fee charges. Please make sure your card on file
                is up to date to avoid your bookings being suspended.
              </span>
            </Alert>
          )}

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Receipt className="h-4 w-4" />
                Deduction history
              </CardTitle>
            </CardHeader>
            <CardContent>
              {data.invoices.length === 0 ? (
                <div className="flex items-start gap-2 rounded-md border bg-muted/30 p-4 text-sm text-muted-foreground">
                  <Info className="mt-0.5 h-4 w-4 shrink-0" />
                  <span>
                    No platform-fee charges yet. Once a booking is active, your
                    first monthly fee appears here.
                  </span>
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Period</TableHead>
                      <TableHead>Booking</TableHead>
                      <TableHead className="text-right">Amount</TableHead>
                      <TableHead className="text-right">Status</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {data.invoices.map((invoice) => (
                      <TableRow key={invoice.invoiceId}>
                        <TableCell className="whitespace-nowrap">
                          {formatDate(invoice.periodStart)}
                          {" – "}
                          {formatDate(invoice.periodEnd)}
                        </TableCell>
                        <TableCell>
                          <Link
                            to={`/app/deals/${invoice.dealId}/billing`}
                            className="text-primary hover:underline"
                          >
                            {invoice.listingTitle ?? "Booking"}
                          </Link>
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {formatMoney(invoice.amountCents)}
                        </TableCell>
                        <TableCell className="text-right">
                          <InvoiceStatusBadge status={invoice.status} />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
};

function SummaryStat({
  icon,
  label,
  value,
  emphasis,
}: {
  icon: ReactNode;
  label: string;
  value: string;
  emphasis?: boolean;
}) {
  return (
    <Card className={emphasis ? "border-primary/30 bg-primary/5" : undefined}>
      <CardContent className="p-4">
        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
          {icon}
          {label}
        </div>
        <p className="mt-1 text-xl font-bold tracking-tight">{value}</p>
      </CardContent>
    </Card>
  );
}

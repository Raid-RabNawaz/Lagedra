import { Link } from "react-router-dom";
import { Calendar, Clock, User } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { ApplicationStatusBadge } from "./ApplicationStatusBadge";
import { formatDate, formatMoney } from "@/utils/format";
import type { DealApplicationDto } from "@/api/types";

type Props = {
  application: DealApplicationDto;
  showListingLink?: boolean;
};

export const ApplicationCard = ({ application, showListingLink = false }: Props) => {
  const stayLabel = `${application.requestedCheckIn} → ${application.requestedCheckOut}`;

  return (
    <Link to={`/app/applications/${application.applicationId}`}>
      <Card className="transition-shadow hover:shadow-md">
        <CardContent className="p-5">
          <div className="flex items-start justify-between gap-3">
            <div className="space-y-2 min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
                <ApplicationStatusBadge status={application.status} />
                {application.jurisdictionWarning && (
                  <span className="text-xs text-amber-600 font-medium">
                    {application.jurisdictionWarning}
                  </span>
                )}
              </div>

              <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
                <span className="flex items-center gap-1.5">
                  <Calendar className="h-3.5 w-3.5" />
                  {stayLabel}
                </span>
                <span className="flex items-center gap-1.5">
                  <Clock className="h-3.5 w-3.5" />
                  {application.stayDurationDays} days
                </span>
              </div>

              {showListingLink && (
                <p className="text-xs text-muted-foreground truncate">
                  Listing: {application.listingId}
                </p>
              )}
            </div>

            <div className="text-right shrink-0 space-y-1">
              {application.firstMonthRentCents != null && (
                <p className="text-sm font-semibold">
                  {formatMoney(application.firstMonthRentCents)}/mo
                </p>
              )}
              {application.depositAmountCents != null && (
                <p className="text-xs text-muted-foreground">
                  Deposit: {formatMoney(application.depositAmountCents)}
                </p>
              )}
            </div>
          </div>

          <div className="mt-3 flex items-center gap-2 text-xs text-muted-foreground">
            <User className="h-3 w-3" />
            Submitted {formatDate(application.submittedAt)}
            {application.decidedAt && (
              <span className="ml-2">
                · Decided {formatDate(application.decidedAt)}
              </span>
            )}
          </div>
        </CardContent>
      </Card>
    </Link>
  );
};

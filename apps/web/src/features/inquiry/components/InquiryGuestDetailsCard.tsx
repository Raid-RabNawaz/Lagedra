import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ApplicationProfilePanel } from "@/features/applications/components/ApplicationProfilePanel";

type Props = {
  tenantUserId: string;
};

/**
 * Guest identity + reputation for the host (and attached partner) on an
 * inquiry thread, shown before they confirm rent/deposit terms.
 */
export function InquiryGuestDetailsCard({ tenantUserId }: Props) {
  return (
    <Card className="mb-6">
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Guest</CardTitle>
        <p className="text-sm text-muted-foreground">
          Review this guest before confirming terms.
        </p>
      </CardHeader>
      <CardContent>
        <ApplicationProfilePanel
          userId={tenantUserId}
          roleLabel="Guest"
          profileLink={`/app/users/${tenantUserId}`}
          showReputation
        />
      </CardContent>
    </Card>
  );
}

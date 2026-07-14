import { useState } from "react";
import { Building2, User, Users, X } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { Alert } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { getApiErrorMessage } from "@/api/errors";
import type { InquiryDto } from "@/api/types";
import { partnerApi } from "@/features/partners/services/partnerApi";
import {
  useAddInquiryPartner,
  useRemoveInquiryPartner,
} from "@/features/inquiry/hooks/useInquiry";

type Props = {
  inquiry: InquiryDto;
  isTenant: boolean;
  isLandlord: boolean;
  isPartner: boolean;
  canManagePartner: boolean;
};

/**
 * Shows who is on the inquiry thread and lets the tenant invite an
 * endorsed partner (or remove them while pre-deal).
 */
export const InquiryParticipantsPanel = ({
  inquiry,
  isTenant,
  isLandlord,
  isPartner,
  canManagePartner,
}: Props) => {
  const addPartner = useAddInquiryPartner();
  const removePartner = useRemoveInquiryPartner();
  const [orgId, setOrgId] = useState("");

  const endorsements = useQuery({
    queryKey: ["me", "partner-endorsements"],
    queryFn: () => partnerApi.listMyEndorsements(),
    enabled: isTenant && canManagePartner && !inquiry.partnerOrganizationId,
    staleTime: 60_000,
  });

  const approved = (endorsements.data ?? []).filter(
    (e) => e.status === "Approved",
  );

  const error = addPartner.error ?? removePartner.error;

  return (
    <Card className="mb-6">
      <CardHeader className="pb-2">
        <div className="flex items-center gap-2">
          <Users className="h-4 w-4 text-muted-foreground" />
          <CardTitle className="text-base">Participants</CardTitle>
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex flex-wrap gap-2">
          <Badge variant="secondary" className="gap-1">
            <User className="h-3 w-3" />
            Tenant{isTenant ? " (you)" : ""}
          </Badge>
          <Badge variant="secondary" className="gap-1">
            <User className="h-3 w-3" />
            Host{isLandlord ? " (you)" : ""}
          </Badge>
          {inquiry.partnerOrganizationId ? (
            <Badge variant="default" className="gap-1">
              <Building2 className="h-3 w-3" />
              {inquiry.partnerOrganizationName ?? "Partner"}
              {isPartner ? " (you)" : ""}
            </Badge>
          ) : (
            <Badge variant="outline" className="text-muted-foreground">
              No partner yet
            </Badge>
          )}
        </div>

        {error && (
          <Alert variant="destructive" className="text-sm">
            {getApiErrorMessage(error, "Could not update partner.")}
          </Alert>
        )}

        {canManagePartner && inquiry.partnerOrganizationId && (isTenant || isPartner) && (
          <Button
            size="sm"
            variant="outline"
            className="gap-1.5"
            disabled={removePartner.isPending}
            onClick={() => removePartner.mutate(inquiry.sessionId)}
          >
            <X className="h-3.5 w-3.5" />
            Remove partner
          </Button>
        )}

        {canManagePartner && isTenant && !inquiry.partnerOrganizationId && (
          <div className="space-y-2 rounded-md border p-3">
            <Label htmlFor="add-partner-org">Invite endorsed partner</Label>
            {endorsements.isLoading ? (
              <p className="text-xs text-muted-foreground">Loading endorsements…</p>
            ) : approved.length === 0 ? (
              <p className="text-xs text-muted-foreground">
                You need an approved partner endorsement before you can invite them.
              </p>
            ) : (
              <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
                <Select
                  id="add-partner-org"
                  value={orgId}
                  onChange={(e) => setOrgId(e.target.value)}
                  className="sm:flex-1"
                >
                  <option value="">Select partner…</option>
                  {approved.map((e) => (
                    <option key={e.id} value={e.organizationId}>
                      {e.organizationName}
                    </option>
                  ))}
                </Select>
                <Button
                  size="sm"
                  disabled={!orgId || addPartner.isPending}
                  onClick={() =>
                    addPartner.mutate({
                      sessionId: inquiry.sessionId,
                      organizationId: orgId,
                    })
                  }
                >
                  Add partner
                </Button>
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
};

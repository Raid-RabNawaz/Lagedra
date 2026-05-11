import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Building2, ShieldCheck, Loader2 } from "lucide-react";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { usePartnerMembership } from "@/features/partners/hooks/usePartnerMembership";
import { extractErrorMessage } from "@/lib/errors";
import type { PartnerOrganizationType } from "@/api/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Checkbox } from "@/components/ui/checkbox";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormError } from "@/components/shared/FormError";

export const PartnerOnboardingPage = () => {
  const navigate = useNavigate();
  const { refresh } = usePartnerMembership();

  const [name, setName] = useState("");
  const [orgType, setOrgType] = useState<PartnerOrganizationType>("Relocation");
  const [contactEmail, setContactEmail] = useState("");
  const [taxId, setTaxId] = useState("");
  const [acceptedTerms, setAcceptedTerms] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const isValid =
    name.trim().length >= 2 &&
    contactEmail.trim().length > 3 &&
    contactEmail.includes("@") &&
    acceptedTerms;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isValid || isSubmitting) return;

    setIsSubmitting(true);
    setSubmitError(null);
    try {
      await partnerApi.register({
        name: name.trim(),
        organizationType: orgType,
        contactEmail: contactEmail.trim(),
        taxId: taxId.trim() || null,
        endorsementTermsAccepted: acceptedTerms,
      });
      await refresh();
      navigate("/app/partner", { replace: true });
    } catch (err) {
      setSubmitError(extractErrorMessage(err));
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="flex items-center gap-2 text-3xl font-bold tracking-tight">
          <Building2 className="h-7 w-7 text-muted-foreground" />
          Register your organization
        </h1>
        <p className="mt-2 text-muted-foreground">
          Tell us about your organization so we can verify it. Once verified, you'll be able to
          generate referral links, book on behalf of guests, and endorse tenants.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Organization details</CardTitle>
          <CardDescription>
            All fields except Tax ID are required. We'll review your submission within 1–2 business
            days.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={(e) => void handleSubmit(e)} className="space-y-5">
            <div className="space-y-2">
              <Label htmlFor="org-name">Organization name</Label>
              <Input
                id="org-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Acme University"
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="org-type">Organization type</Label>
              <Select
                id="org-type"
                value={orgType}
                onChange={(e) => setOrgType(e.target.value as PartnerOrganizationType)}
              >
                <option value="Relocation">Relocation</option>
                <option value="Tech">Tech / Corporate</option>
                <option value="Other">Other</option>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="contact-email">Contact email</Label>
              <Input
                id="contact-email"
                type="email"
                value={contactEmail}
                onChange={(e) => setContactEmail(e.target.value)}
                placeholder="ops@example.com"
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="tax-id">
                Tax ID <span className="text-muted-foreground">(optional)</span>
              </Label>
              <Input
                id="tax-id"
                value={taxId}
                onChange={(e) => setTaxId(e.target.value)}
                placeholder="EIN, VAT number, etc."
              />
            </div>

            <div className="space-y-3 rounded-md border p-4">
              <div className="flex items-start gap-3">
                <Checkbox
                  id="endorsement-terms"
                  checked={acceptedTerms}
                  onCheckedChange={(value) => setAcceptedTerms(value === true)}
                />
                <Label htmlFor="endorsement-terms" className="text-sm font-normal leading-relaxed">
                  <span className="flex items-center gap-1 font-medium text-foreground">
                    <ShieldCheck className="h-4 w-4" />
                    I accept the Lagedra Partner Endorsement Terms
                  </span>
                  <span className="mt-1 block text-muted-foreground">
                    Partner-Backed Protection is a verification status, not an insurance policy.
                    Lagedra does not pay claims under this tier; eviction-related disputes follow
                    the standard Lagedra arbitration process. By endorsing a tenant, your
                    organization vouches for them and their endorsement may reduce their security
                    deposit.
                  </span>
                </Label>
              </div>
            </div>

            {submitError && <FormError message={submitError} />}

            <Alert>
              <AlertDescription className="text-xs">
                After submission, your organization status will be{" "}
                <strong>Pending verification</strong> until a Lagedra admin reviews it. You can
                still invite members and view your dashboard while you wait.
              </AlertDescription>
            </Alert>

            <Button type="submit" disabled={!isValid || isSubmitting} className="w-full">
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
              {isSubmitting ? "Submitting..." : "Register organization"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
};

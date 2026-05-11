import { ShieldCheck } from "lucide-react";
import { Alert } from "@/components/ui/alert";

export const PaymentSecurityNotice = () => (
  <Alert className="border-blue-200 bg-blue-50 text-blue-800">
    <ShieldCheck className="h-4 w-4" />
    <span className="ml-2 text-sm">
      All payments are securely processed through Stripe. Lagedra collects the
      activation payment, deducts the platform fee and insurance premium, and
      transfers the remainder to the host.
    </span>
  </Alert>
);

/**
 * @deprecated Use PaymentSecurityNotice instead.
 */
export const NonCustodialDisclaimer = PaymentSecurityNotice;

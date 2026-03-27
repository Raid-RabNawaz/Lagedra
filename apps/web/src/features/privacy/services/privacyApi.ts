import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type { ConsentRecordDto, ConsentTypeDto, RecordConsentRequest } from "@/api/types";

function clientMetadata(): { ipAddress: string; userAgent: string } {
  return {
    // Real IP is set by the gateway from X-Forwarded-For; browser cannot supply it reliably.
    ipAddress: "0.0.0.0",
    userAgent: typeof navigator !== "undefined" && navigator.userAgent ? navigator.userAgent : "LagedraWeb",
  };
}

export const privacyApi = {
  async getUserConsents(userId: string): Promise<ConsentRecordDto[]> {
    const response = await http.get<ConsentRecordDto[]>(endpoints.privacy.userConsents(userId));
    return response.data;
  },

  async recordConsent(payload: Omit<RecordConsentRequest, "ipAddress" | "userAgent">): Promise<void> {
    const meta = clientMetadata();
    const body: RecordConsentRequest = {
      ...payload,
      ...meta,
    };
    await http.post(endpoints.privacy.recordConsent, body);
  },

  /** Required for mutating API calls (see ConsentMiddleware). */
  async ensureRequiredConsents(userId: string): Promise<void> {
    const required: ConsentTypeDto[] = ["KYCConsent", "DataProcessing"];
    const records = await this.getUserConsents(userId);
    const active = new Set(
      records.filter((r) => r.withdrawnAt == null).map((r) => r.consentType),
    );
    for (const type of required) {
      if (!active.has(type)) {
        await this.recordConsent({ userId, consentType: type });
      }
    }
  },
};

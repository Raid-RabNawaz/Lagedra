import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  ApproveApplicationRequest,
  AttachApplicationPaymentRequest,
  BookingSetupIntentResult,
  DealApplicationDto,
  InsuranceStatusDto,
  OwnerTenancyConsentRequest,
  ReservationPreviewDto,
  ScreeningCheckoutDto,
  ScreeningReportView,
  ScreeningStatusDto,
  SubmitApplicationRequest,
  SubmitApplicationResult,
} from "@/api/types";

export const applicationApi = {
  async listMine(): Promise<DealApplicationDto[]> {
    const response = await http.get<DealApplicationDto[]>(endpoints.applications.mine);
    return response.data;
  },

  async listOwnerPending(): Promise<DealApplicationDto[]> {
    const response = await http.get<DealApplicationDto[]>(endpoints.applications.ownerPending);
    return response.data;
  },

  async submit(payload: SubmitApplicationRequest): Promise<SubmitApplicationResult> {
    const response = await http.post<SubmitApplicationResult>(
      endpoints.applications.submit,
      payload,
    );
    return response.data;
  },

  async createSetupIntent(listingId: string): Promise<BookingSetupIntentResult> {
    const response = await http.post<BookingSetupIntentResult>(
      endpoints.applications.setupIntent,
      { listingId },
    );
    return response.data;
  },

  async preview(
    listingId: string,
    checkIn: string,
    checkOut: string,
  ): Promise<ReservationPreviewDto> {
    const response = await http.get<ReservationPreviewDto>(
      endpoints.applications.preview,
      { params: { listingId, checkIn, checkOut } },
    );
    return response.data;
  },

  async getById(id: string): Promise<DealApplicationDto> {
    const response = await http.get<DealApplicationDto>(
      endpoints.applications.detail(id),
    );
    return response.data;
  },

  async listForListing(listingId: string): Promise<DealApplicationDto[]> {
    const response = await http.get<DealApplicationDto[]>(
      endpoints.applications.forListing(listingId),
    );
    return response.data;
  },

  async approve(id: string, payload: ApproveApplicationRequest): Promise<DealApplicationDto> {
    const response = await http.post<DealApplicationDto>(
      endpoints.applications.approve(id),
      payload,
    );
    return response.data;
  },

  async ownerConsent(
    id: string,
    payload: OwnerTenancyConsentRequest,
  ): Promise<DealApplicationDto> {
    const response = await http.post<DealApplicationDto>(
      endpoints.applications.ownerConsent(id),
      payload,
    );
    return response.data;
  },

  async ownerDecline(id: string): Promise<DealApplicationDto> {
    const response = await http.post<DealApplicationDto>(
      endpoints.applications.ownerDecline(id),
    );
    return response.data;
  },

  async consentOwnerTenancyByToken(token: string): Promise<DealApplicationDto> {
    const response = await http.post<DealApplicationDto>(
      endpoints.actions.consentOwnerTenancy,
      { token },
    );
    return response.data;
  },

  async declineOwnerTenancyByToken(token: string): Promise<DealApplicationDto> {
    const response = await http.post<DealApplicationDto>(
      endpoints.actions.declineOwnerTenancy,
      { token },
    );
    return response.data;
  },

  async reject(id: string): Promise<DealApplicationDto> {
    const response = await http.post<DealApplicationDto>(
      endpoints.applications.reject(id),
    );
    return response.data;
  },

  async getInsurance(dealId: string): Promise<InsuranceStatusDto> {
    const response = await http.get<InsuranceStatusDto>(endpoints.deals.insurance(dealId));
    return response.data;
  },

  async rescreenInsurance(dealId: string): Promise<void> {
    await http.post(endpoints.deals.insuranceRescreen(dealId));
  },

  async getScreening(id: string): Promise<ScreeningStatusDto> {
    const response = await http.get<ScreeningStatusDto>(endpoints.applications.screening(id));
    return response.data;
  },

  async createScreeningCheckout(id: string): Promise<ScreeningCheckoutDto> {
    const response = await http.post<ScreeningCheckoutDto>(
      endpoints.applications.screeningCheckout(id),
    );
    return response.data;
  },

  async getScreeningReport(id: string): Promise<ScreeningReportView> {
    const response = await http.get<ScreeningReportView>(
      endpoints.applications.screeningReport(id),
    );
    return response.data;
  },

  async downloadScreeningReportPdf(
    id: string,
  ): Promise<{ blob: Blob; filename: string } | null> {
    try {
      // Checkr generates the PDF on demand; the default 10s axios timeout
      // aborts before the file arrives.
      const response = await http.get(endpoints.applications.screeningReportPdf(id), {
        responseType: "blob",
        timeout: 90_000,
      });
      const raw = response.data as Blob;
      if (!(await blobLooksLikePdf(raw))) {
        return null;
      }
      const blob =
        raw.type.toLowerCase().includes("pdf")
          ? raw
          : new Blob([raw], { type: "application/pdf" });
      const disposition = response.headers["content-disposition"] as string | undefined;
      const match = disposition?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);
      const filename = match?.[1]
        ? decodeURIComponent(match[1])
        : `screening-report-${id}.pdf`;
      return { blob, filename };
    } catch {
      return null;
    }
  },

  async attachPayment(
    id: string,
    payload: AttachApplicationPaymentRequest,
  ): Promise<DealApplicationDto> {
    const response = await http.post<DealApplicationDto>(
      endpoints.applications.attachPayment(id),
      payload,
    );
    return response.data;
  },
};

async function blobLooksLikePdf(blob: Blob): Promise<boolean> {
  if (blob.type.toLowerCase().includes("pdf")) {
    return true;
  }

  const header = new Uint8Array(await blob.slice(0, 5).arrayBuffer());
  return (
    header.length >= 4 &&
    header[0] === 0x25 &&
    header[1] === 0x50 &&
    header[2] === 0x44 &&
    header[3] === 0x46
  );
}

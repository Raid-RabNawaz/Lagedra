import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  BillingStatusDto,
  ProrationQuoteDto,
  PaymentConfirmationDto,
  PaymentDetailsDto,
  CancellationResultDto,
  DamageClaimDto,
  DisputePaymentRequest,
  CancelBookingRequest,
  FileDamageClaimRequest,
} from "@/api/types";

export const billingApi = {
  async getBillingStatus(dealId: string): Promise<BillingStatusDto> {
    const response = await http.get<BillingStatusDto>(
      endpoints.billing.status(dealId),
    );
    return response.data;
  },

  async getProrationQuote(
    dealId: string,
    startDate: string,
    endDate: string,
  ): Promise<ProrationQuoteDto> {
    const response = await http.get<ProrationQuoteDto>(
      endpoints.billing.prorationQuote(dealId),
      { params: { startDate, endDate } },
    );
    return response.data;
  },

  async activateDeal(dealId: string): Promise<BillingStatusDto> {
    const response = await http.post<BillingStatusDto>(
      endpoints.billing.activate(dealId),
    );
    return response.data;
  },

  async stopBilling(dealId: string): Promise<BillingStatusDto> {
    const response = await http.post<BillingStatusDto>(
      endpoints.billing.stopBilling(dealId),
    );
    return response.data;
  },

  async getPaymentDetails(dealId: string): Promise<PaymentDetailsDto> {
    const response = await http.get<PaymentDetailsDto>(
      endpoints.payment.details(dealId),
    );
    return response.data;
  },

  async getPaymentStatus(dealId: string): Promise<PaymentConfirmationDto> {
    const response = await http.get<PaymentConfirmationDto>(
      endpoints.payment.status(dealId),
    );
    return response.data;
  },

  async confirmPayment(dealId: string): Promise<PaymentConfirmationDto> {
    const response = await http.post<PaymentConfirmationDto>(
      endpoints.payment.confirm(dealId),
    );
    return response.data;
  },

  async confirmPlatformPayment(
    dealId: string,
  ): Promise<PaymentConfirmationDto> {
    const response = await http.post<PaymentConfirmationDto>(
      endpoints.payment.confirmPlatform(dealId),
    );
    return response.data;
  },

  async disputePayment(
    dealId: string,
    payload: DisputePaymentRequest,
  ): Promise<PaymentConfirmationDto> {
    const response = await http.post<PaymentConfirmationDto>(
      endpoints.payment.dispute(dealId),
      payload,
    );
    return response.data;
  },

  async cancelBooking(
    dealId: string,
    payload: CancelBookingRequest,
  ): Promise<CancellationResultDto> {
    const response = await http.post<CancellationResultDto>(
      endpoints.payment.cancel(dealId),
      payload,
    );
    return response.data;
  },

  async fileDamageClaim(
    dealId: string,
    payload: FileDamageClaimRequest,
  ): Promise<DamageClaimDto> {
    const response = await http.post<DamageClaimDto>(
      endpoints.payment.damageClaim(dealId),
      payload,
    );
    return response.data;
  },
};

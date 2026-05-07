import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  HostStripeStatusDto,
  HostPaymentDetailsDto,
  SavePaymentDetailsRequest,
} from "@/api/types";

export const hostStripeApi = {
  async onboard(): Promise<HostStripeStatusDto> {
    const response = await http.post<HostStripeStatusDto>(
      endpoints.hostPayouts.start,
    );
    return response.data;
  },

  async refreshLink(): Promise<{ onboardingUrl: string }> {
    const response = await http.post<{ onboardingUrl: string }>(
      endpoints.hostPayouts.refreshLink,
    );
    return response.data;
  },

  async getStatus(): Promise<HostStripeStatusDto> {
    const response = await http.get<HostStripeStatusDto>(
      endpoints.hostPayouts.status,
    );
    return response.data;
  },

  async getPaymentDetails(): Promise<HostPaymentDetailsDto> {
    const response = await http.get<HostPaymentDetailsDto>(
      endpoints.hostPayment.details,
    );
    return response.data;
  },

  async savePaymentDetails(
    payload: SavePaymentDetailsRequest,
  ): Promise<void> {
    await http.put(endpoints.hostPayment.details, payload);
  },
};

import { isAxiosError } from "axios";
import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import { stripeConnectReturnUrls } from "@/features/host-onboarding/lib/stripeConnectUrls";
import type {
  HostStripeStatusDto,
  HostPaymentDetailsDto,
  SavePaymentDetailsRequest,
} from "@/api/types";

export const hostStripeApi = {
  async onboard(): Promise<HostStripeStatusDto> {
    const response = await http.post<HostStripeStatusDto>(
      endpoints.hostPayouts.start,
      stripeConnectReturnUrls(),
      { timeout: 30_000 },
    );
    return response.data;
  },

  async refreshLink(): Promise<{ onboardingUrl: string }> {
    const response = await http.post<{ onboardingUrl: string }>(
      endpoints.hostPayouts.refreshLink,
      stripeConnectReturnUrls(),
    );
    return response.data;
  },

  async openExpressDashboard(): Promise<{ url: string }> {
    const response = await http.post<{ url: string }>(
      endpoints.hostPayouts.expressLogin,
    );
    return response.data;
  },

  async updateAccountLink(): Promise<{ url: string }> {
    const response = await http.post<{ url: string }>(
      endpoints.hostPayouts.updateLink,
      stripeConnectReturnUrls(),
    );
    return response.data;
  },

  async getStatus(): Promise<HostStripeStatusDto | null> {
    try {
      const response = await http.get<HostStripeStatusDto>(
        endpoints.hostPayouts.status,
      );
      return response.data;
    } catch (error) {
      if (isAxiosError(error) && error.response?.status === 404) {
        return null;
      }
      throw error;
    }
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

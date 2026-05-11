import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type { CheckoutDto } from "@/api/types";

export const checkoutApi = {
  async createCheckout(dealId: string): Promise<CheckoutDto> {
    const response = await http.post<CheckoutDto>(
      endpoints.checkout.create(dealId),
      undefined,
      { timeout: 60_000 },
    );
    return response.data;
  },

  async confirmCheckout(dealId: string): Promise<CheckoutDto> {
    const response = await http.post<CheckoutDto>(
      endpoints.checkout.confirm(dealId),
    );
    return response.data;
  },

  async getCheckoutStatus(dealId: string): Promise<CheckoutDto> {
    const response = await http.get<CheckoutDto>(
      endpoints.checkout.status(dealId),
    );
    return response.data;
  },
};

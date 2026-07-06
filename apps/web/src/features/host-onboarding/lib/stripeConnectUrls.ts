/** Stripe Connect onboarding sends the user back to this route. */
export const PAYOUT_SETUP_PATH = "/app/payout-setup";

export function stripeConnectReturnUrls() {
  const origin =
    typeof window !== "undefined" ? window.location.origin : "http://localhost:3000";
  const returnUrl = `${origin}${PAYOUT_SETUP_PATH}`;
  return { returnUrl, refreshUrl: returnUrl };
}

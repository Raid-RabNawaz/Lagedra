/**
 * A2P 10DLC web-form copy. Keep in lockstep with
 * `Lagedra.Modules.Notifications.Domain.SmsProgram`.
 */
export const SMS_FREQUENCY = "up to 8 messages per month";

export const SMS_PROGRAM = {
  frequency: SMS_FREQUENCY,
  frequencySentence: `You will receive ${SMS_FREQUENCY}.`,
  checkboxLabel:
    `Yes, I would like to receive automated text messages from Lagedra about booking and payment activity, promotional offers, and important account updates. I understand I will receive ${SMS_FREQUENCY}.`,
  rates:
    "Message and data rates may apply depending on your mobile phone service plan.",
  helpStop:
    "Reply HELP for help or STOP to cancel any time. By providing your phone number and checking the box above, you agree to receive text messages from Lagedra. Consent is not required to book a stay or use Lagedra.",
  submitLabel: "Yes, sign me up!",
  unsubscribeLabel: "Unsubscribe this number",
  /** Must stay false so the A2P consent box is never pre-selected. */
  defaultConsent: false,
} as const;

/** 2FA / OTP program. Sent only when the user asks for a code. */
export const SMS_OTP_PROGRAM = {
  sample:
    "Lagedra: Your verification code is [VerificationCode]. It expires in 10 minutes. www.lagedra.com Msg & data rates may apply. Reply HELP for help or STOP to cancel.",
  optInLabel:
    "By tapping Send verification code I agree to receive a one-time Lagedra verification text at the mobile number on my account. Message and data rates may apply. Reply HELP for help or STOP to cancel.",
} as const;

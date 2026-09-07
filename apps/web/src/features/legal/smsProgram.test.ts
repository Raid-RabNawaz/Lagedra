import { describe, expect, it } from "vitest";
import { SMS_FREQUENCY, SMS_OTP_PROGRAM, SMS_PROGRAM } from "./smsProgram";

describe("SMS program copy", () => {
  it("never pre-selects the A2P consent checkbox", () => {
    expect(SMS_PROGRAM.defaultConsent).toBe(false);
  });

  it("states message type, frequency, rates, and HELP/STOP", () => {
    expect(SMS_PROGRAM.checkboxLabel).toContain("booking and payment activity");
    expect(SMS_PROGRAM.checkboxLabel).toContain("promotional offers");
    expect(SMS_PROGRAM.checkboxLabel).toContain(SMS_FREQUENCY);
    expect(SMS_PROGRAM.frequencySentence).toContain(SMS_FREQUENCY);
    expect(SMS_PROGRAM.rates.toLowerCase()).toContain("message and data rates");
    expect(SMS_PROGRAM.helpStop).toContain("HELP");
    expect(SMS_PROGRAM.helpStop).toContain("STOP");
    expect(SMS_PROGRAM.helpStop).toContain("Consent is not required");
    expect(SMS_PROGRAM.submitLabel).toBe("Yes, sign me up!");
  });

  it("documents 2FA opt-in and opt-out", () => {
    expect(SMS_OTP_PROGRAM.optInLabel).toContain("Send verification code");
    expect(SMS_OTP_PROGRAM.sample).toContain("Lagedra");
    expect(SMS_OTP_PROGRAM.sample).toContain("[VerificationCode]");
    expect(SMS_OTP_PROGRAM.sample).toContain("STOP");
  });
});

import { Link } from "react-router-dom";
import { SMS_PROGRAM } from "./smsProgram";

export function SmsProgramDisclosures() {
  return (
    <div className="space-y-2 text-sm leading-6 text-[#3D3D4E]">
      <p>
        <strong className="text-[#1A1A2E]">Message frequency.</strong>{" "}
        {SMS_PROGRAM.frequencySentence}
      </p>
      <p>
        <strong className="text-[#1A1A2E]">Standard rates.</strong> {SMS_PROGRAM.rates}
      </p>
      <p>
        <strong className="text-[#1A1A2E]">Help &amp; stop.</strong> {SMS_PROGRAM.helpStop}
      </p>
      <p>
        <Link to="/tc" className="font-medium text-[#5B3FE0] underline underline-offset-2">
          Terms of Service
        </Link>
        {" | "}
        <Link to="/privacy" className="font-medium text-[#5B3FE0] underline underline-offset-2">
          Privacy Policy
        </Link>
      </p>
    </div>
  );
}

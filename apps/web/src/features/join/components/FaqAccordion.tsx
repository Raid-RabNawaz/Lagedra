import { useState } from "react";
import { Plus } from "lucide-react";
import { cn } from "@/lib/utils";
import { howItWorksContent } from "../joinContent";

export function FaqAccordion({ className }: { className?: string }) {
  const [openFaq, setOpenFaq] = useState(0);

  return (
    <div className={cn("divide-y divide-[#E5E5EE] rounded-2xl border border-[#E5E5EE]", className)}>
      {howItWorksContent.faq.map((item, index) => {
        const open = openFaq === index;
        return (
          <div key={item.q}>
            <button
              type="button"
              onClick={() => setOpenFaq(open ? -1 : index)}
              className="flex w-full items-center justify-between gap-4 px-5 py-4 text-left"
            >
              <span className="font-semibold text-[#1A1A2E]">{item.q}</span>
              <Plus
                className={cn(
                  "h-5 w-5 shrink-0 text-[#5B3FE0] transition-transform",
                  open && "rotate-45",
                )}
              />
            </button>
            {open && (
              <p className="px-5 pb-5 text-sm leading-relaxed text-[#3D3D4E]">{item.a}</p>
            )}
          </div>
        );
      })}
    </div>
  );
}

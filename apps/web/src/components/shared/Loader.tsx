import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

type LoaderProps = {
  className?: string;
  label?: string;
  fullPage?: boolean;
};

export function Loader({ className, label = "Loading...", fullPage = false }: LoaderProps) {
  if (fullPage) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className={cn("h-8 w-8 animate-spin text-muted-foreground", className)} />
          <p className="text-sm text-muted-foreground">{label}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex items-center justify-center py-12">
      <div className="flex flex-col items-center gap-3">
        <Loader2 className={cn("h-6 w-6 animate-spin text-muted-foreground", className)} />
        <p className="text-sm text-muted-foreground">{label}</p>
      </div>
    </div>
  );
}

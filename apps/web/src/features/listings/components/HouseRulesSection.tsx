import {
  LogIn,
  LogOut,
  Users,
  Dog,
  Cigarette,
  PartyPopper,
  VolumeOff,
  FileText,
} from "lucide-react";
import type { HouseRulesDto } from "@/api/types";
import { Badge } from "@/components/ui/badge";

type HouseRulesSectionProps = {
  rules: HouseRulesDto;
};

function formatTime(t: string): string {
  const [h, m] = t.split(":");
  const hour = parseInt(h ?? "0", 10);
  const minute = m ?? "00";
  const ampm = hour >= 12 ? "PM" : "AM";
  const displayHour = hour % 12 || 12;
  return `${displayHour}:${minute} ${ampm}`;
}

function AllowedBadge({ allowed, label }: { allowed: boolean; label: string }) {
  return (
    <Badge variant={allowed ? "success" : "secondary"} className="text-xs">
      {allowed ? `${label} allowed` : `No ${label.toLowerCase()}`}
    </Badge>
  );
}

export function HouseRulesSection({ rules }: HouseRulesSectionProps) {
  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <RuleItem icon={LogIn} label="Check-in" value={formatTime(rules.checkInTime)} />
        <RuleItem icon={LogOut} label="Check-out" value={formatTime(rules.checkOutTime)} />
        <RuleItem icon={Users} label="Max guests" value={String(rules.maxGuests)} />
        {rules.quietHoursStart && rules.quietHoursEnd && (
          <RuleItem
            icon={VolumeOff}
            label="Quiet hours"
            value={`${formatTime(rules.quietHoursStart)}–${formatTime(rules.quietHoursEnd)}`}
          />
        )}
      </div>

      <div className="flex flex-wrap gap-2 pt-1">
        <AllowedBadge allowed={rules.petsAllowed} label="Pets" />
        <AllowedBadge allowed={rules.smokingAllowed} label="Smoking" />
        <AllowedBadge allowed={rules.partiesAllowed} label="Parties" />
      </div>

      {rules.petsNotes && (
        <div className="flex items-start gap-2 text-sm text-muted-foreground">
          <Dog className="h-4 w-4 mt-0.5 shrink-0" />
          <span>{rules.petsNotes}</span>
        </div>
      )}

      {rules.additionalRules && (
        <div className="flex items-start gap-2 text-sm text-muted-foreground border-t pt-3">
          <FileText className="h-4 w-4 mt-0.5 shrink-0" />
          <span>{rules.additionalRules}</span>
        </div>
      )}
    </div>
  );
}

function RuleItem({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof LogIn;
  label: string;
  value: string;
}) {
  return (
    <div className="flex items-center gap-2.5">
      <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-secondary shrink-0">
        <Icon className="h-4 w-4 text-muted-foreground" />
      </div>
      <div>
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="text-sm font-medium">{value}</p>
      </div>
    </div>
  );
}

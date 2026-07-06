import { Link } from "react-router-dom";
import {
  ArrowRight,
  BadgeCheck,
  Briefcase,
  CalendarDays,
  Languages,
  Mail,
  MapPin,
  Phone,
  User,
} from "lucide-react";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { Loader } from "@/components/shared/Loader";
import { usePublicProfile } from "@/features/auth/hooks/usePublicProfile";
import { getApiErrorMessage, isNotFoundError } from "@/api/errors";
import { formatDate } from "@/utils/format";
import {
  profileDisplayName,
  profileInitials,
  profileLocation,
  profileVerificationCount,
} from "@/features/applications/lib/applicationProfileUtils";

type Props = {
  userId: string | undefined;
  enabled?: boolean;
  roleLabel: "Guest" | "Host";
  profileLink?: string;
  compact?: boolean;
};

function VerificationChip({
  label,
  verified,
  icon,
}: {
  label: string;
  verified: boolean;
  icon: React.ReactNode;
}) {
  if (verified) {
    return (
      <Badge variant="secondary" className="gap-1">
        {icon}
        {label}
      </Badge>
    );
  }
  return (
    <Badge variant="outline" className="gap-1 text-muted-foreground">
      {icon}
      {label}
    </Badge>
  );
}

export function ApplicationProfilePanel({
  userId,
  enabled = true,
  roleLabel,
  profileLink,
  compact = false,
}: Props) {
  const query = usePublicProfile(enabled ? userId : undefined);
  const profile = query.data;
  const display = profileDisplayName(profile, roleLabel);
  const location = profile ? profileLocation(profile) : null;
  const verifiedCount = profileVerificationCount(profile);

  if (query.isLoading) {
    return (
      <div className={compact ? "py-2" : "py-4"}>
        <Loader label={`Loading ${roleLabel.toLowerCase()} profile…`} />
      </div>
    );
  }

  if (query.isError) {
    return (
      <Alert variant="destructive" className="text-sm">
        {isNotFoundError(query.error)
          ? `${roleLabel} profile not available.`
          : getApiErrorMessage(query.error, `Failed to load ${roleLabel.toLowerCase()} profile.`)}
      </Alert>
    );
  }

  if (!profile) {
    return (
      <p className="text-sm text-muted-foreground">
        {roleLabel} profile not available.
      </p>
    );
  }

  if (compact) {
    return (
      <div className="flex items-center gap-3 min-w-0">
        <Avatar className="h-10 w-10 shrink-0">
          {profile.profilePhotoUrl ? (
            <AvatarImage src={profile.profilePhotoUrl} alt={display} />
          ) : null}
          <AvatarFallback className="text-xs">
            {profileInitials(display, roleLabel[0] ?? "U")}
          </AvatarFallback>
        </Avatar>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-1.5 flex-wrap">
            <p className="text-sm font-semibold leading-none truncate">{display}</p>
            {profile.isGovernmentIdVerified && (
              <Badge variant="secondary" className="gap-1">
                <BadgeCheck className="h-3 w-3" />
                ID
              </Badge>
            )}
          </div>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {location ? (
              <span className="flex items-center gap-1">
                <MapPin className="h-3 w-3" />
                {location}
              </span>
            ) : (
              `${roleLabel} · ${verifiedCount}/3 verified`
            )}
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start">
        <Avatar className="h-16 w-16 shrink-0">
          {profile.profilePhotoUrl ? (
            <AvatarImage src={profile.profilePhotoUrl} alt={display} />
          ) : null}
          <AvatarFallback className="text-base">
            {profileInitials(display, roleLabel[0] ?? "U")}
          </AvatarFallback>
        </Avatar>
        <div className="min-w-0 flex-1 space-y-2">
          <div className="flex flex-wrap items-center gap-2">
            <p className="font-semibold text-base leading-none">{display}</p>
            <Badge variant="outline" className="text-[10px] uppercase tracking-wide">
              {roleLabel}
            </Badge>
            {profile.isGovernmentIdVerified && (
              <Badge variant="secondary" className="gap-1">
                <BadgeCheck className="h-3 w-3" />
                ID verified
              </Badge>
            )}
          </div>
          <div className="flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted-foreground">
            {location && (
              <span className="flex items-center gap-1">
                <MapPin className="h-3 w-3" />
                {location}
              </span>
            )}
            {profile.occupation && (
              <span className="flex items-center gap-1">
                <Briefcase className="h-3 w-3" />
                {profile.occupation}
              </span>
            )}
            <span className="flex items-center gap-1">
              <CalendarDays className="h-3 w-3" />
              Member since {formatDate(profile.memberSince)}
            </span>
            {profile.responseRatePercent != null && (
              <span>Responds {profile.responseRatePercent}% of the time</span>
            )}
          </div>
          {profile.bio && (
            <p className="text-sm text-muted-foreground whitespace-pre-line leading-relaxed">
              {profile.bio}
            </p>
          )}
          <div className="flex flex-wrap items-center gap-2 pt-1">
            <VerificationChip
              label="ID"
              verified={profile.isGovernmentIdVerified}
              icon={<BadgeCheck className="h-3 w-3" />}
            />
            <VerificationChip
              label="Phone"
              verified={profile.isPhoneVerified}
              icon={<Phone className="h-3 w-3" />}
            />
            <VerificationChip
              label="Email"
              verified={profile.isEmailVerified}
              icon={<Mail className="h-3 w-3" />}
            />
            {profile.languages && (
              <Badge variant="outline" className="gap-1">
                <Languages className="h-3 w-3" />
                {profile.languages}
              </Badge>
            )}
          </div>
          <p className="text-[11px] text-muted-foreground">
            {verifiedCount}/3 verifications complete
          </p>
        </div>
      </div>

      {profileLink && (
        <Link to={profileLink}>
          <Button variant="outline" size="sm" className="gap-1.5">
            <User className="h-3.5 w-3.5" />
            View full {roleLabel.toLowerCase()} profile
            <ArrowRight className="h-3.5 w-3.5" />
          </Button>
        </Link>
      )}
    </div>
  );
}

/** Hook wrapper for card previews — returns resolved display fields. */
export function useApplicationProfilePreview(
  userId: string | undefined,
  enabled: boolean,
  fallbackLabel: string,
) {
  const query = usePublicProfile(enabled ? userId : undefined);
  const profile = query.data;
  const name = profileDisplayName(profile, fallbackLabel);
  return {
    query,
    profile,
    name,
    location: profile ? profileLocation(profile) : null,
    initials: profileInitials(name, fallbackLabel[0] ?? "U"),
  };
}

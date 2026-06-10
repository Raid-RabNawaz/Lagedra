import { Link, useParams } from "react-router-dom";
import {
  ArrowLeft,
  BadgeCheck,
  CalendarDays,
  Globe,
  Languages,
  MapPin,
  Phone,
  ShieldAlert,
  ShieldCheck,
  Briefcase,
  Mail,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { Loader } from "@/components/shared/Loader";
import { usePublicProfile } from "@/features/auth/hooks/usePublicProfile";
import { getApiErrorMessage, isNotFoundError } from "@/api/errors";
import { formatDate } from "@/utils/format";

function initialsFor(name?: string | null, fallback = "U"): string {
  if (!name) return fallback;
  const parts = name
    .split(/\s+/)
    .filter((s) => s.length > 0)
    .slice(0, 2);
  if (parts.length === 0) return fallback;
  return parts.map((p) => p[0]?.toUpperCase() ?? "").join("");
}

function formatLocation(profile: {
  city?: string | null;
  state?: string | null;
  country?: string | null;
}): string | null {
  const parts = [profile.city, profile.state, profile.country]
    .map((p) => p?.trim())
    .filter((p): p is string => Boolean(p && p.length > 0));
  return parts.length > 0 ? parts.join(", ") : null;
}

/**
 * Read-only view of another user's profile. Used by hosts inspecting a
 * tenant who applied to one of their listings, and (long-term) by guests
 * viewing a host's profile from the listing detail page. Surfaces only
 * the fields exposed by `GET /v1/auth/users/{userId}/public-profile`.
 */
export const PublicProfilePage = () => {
  const { userId } = useParams<{ userId: string }>();
  const { data: profile, isLoading, isError, error } = usePublicProfile(userId);

  if (!userId) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-12">
        <Alert variant="destructive">Missing user id.</Alert>
      </div>
    );
  }

  if (isLoading) {
    return <Loader fullPage label="Loading profile…" />;
  }

  if (isError || !profile) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-12 text-center">
        <p className="text-destructive font-medium">
          {isNotFoundError(error)
            ? "This user's profile is not available."
            : getApiErrorMessage(error, "Failed to load profile.")}
        </p>
        <Link to=".." className="mt-4 inline-block">
          <Button variant="outline" size="sm">
            <ArrowLeft className="h-4 w-4" />
            Go back
          </Button>
        </Link>
      </div>
    );
  }

  const fullName = profile.displayName
    ?? [profile.firstName, profile.lastName].filter(Boolean).join(" ").trim();
  const heading = fullName && fullName.length > 0 ? fullName : "Lagedra member";
  const location = formatLocation(profile);
  const verifiedCount = [
    profile.isGovernmentIdVerified,
    profile.isPhoneVerified,
    profile.isEmailVerified,
  ].filter(Boolean).length;

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <Link
        to=".."
        relative="path"
        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors"
      >
        <ArrowLeft className="h-4 w-4" />
        Back
      </Link>

      <Card>
        <CardContent className="flex flex-col gap-6 p-6 sm:flex-row sm:items-start">
          <Avatar className="h-24 w-24 shrink-0">
            {profile.profilePhotoUrl ? (
              <AvatarImage src={profile.profilePhotoUrl} alt={heading} />
            ) : null}
            <AvatarFallback className="text-xl">
              {initialsFor(heading)}
            </AvatarFallback>
          </Avatar>

          <div className="flex-1 min-w-0 space-y-3">
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-2xl font-bold tracking-tight">{heading}</h1>
              {profile.isGovernmentIdVerified && (
                <Badge variant="secondary" className="gap-1">
                  <BadgeCheck className="h-3 w-3" />
                  ID verified
                </Badge>
              )}
            </div>

            <div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted-foreground">
              {location && (
                <span className="flex items-center gap-1">
                  <MapPin className="h-3.5 w-3.5" />
                  {location}
                </span>
              )}
              {profile.occupation && (
                <span className="flex items-center gap-1">
                  <Briefcase className="h-3.5 w-3.5" />
                  {profile.occupation}
                </span>
              )}
              <span className="flex items-center gap-1">
                <CalendarDays className="h-3.5 w-3.5" />
                Member since {formatDate(profile.memberSince)}
              </span>
            </div>

            {profile.bio && (
              <p className="text-sm leading-relaxed text-foreground whitespace-pre-wrap">
                {profile.bio}
              </p>
            )}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Verifications</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          <VerificationRow
            label="Government ID"
            verified={profile.isGovernmentIdVerified}
            icon={<BadgeCheck className="h-4 w-4" />}
          />
          <VerificationRow
            label="Phone number"
            verified={profile.isPhoneVerified}
            icon={<Phone className="h-4 w-4" />}
          />
          <VerificationRow
            label="Email address"
            verified={profile.isEmailVerified}
            icon={<Mail className="h-4 w-4" />}
          />
          <p className="pt-2 text-xs text-muted-foreground">
            {verifiedCount === 3
              ? "Every available verification is complete."
              : verifiedCount === 0
                ? "No verifications completed yet."
                : `${verifiedCount} of 3 verifications complete.`}
          </p>
        </CardContent>
      </Card>

      {(profile.languages || profile.responseRatePercent != null) && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base">About</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            {profile.languages && (
              <div className="flex items-center gap-2">
                <Languages className="h-4 w-4 text-muted-foreground" />
                <span className="text-muted-foreground">Languages:</span>
                <span>{profile.languages}</span>
              </div>
            )}
            {profile.responseRatePercent != null && (
              <div className="flex items-center gap-2">
                <Globe className="h-4 w-4 text-muted-foreground" />
                <span className="text-muted-foreground">Response rate:</span>
                <span>{profile.responseRatePercent}%</span>
              </div>
            )}
            {profile.responseTimeMinutes != null && (
              <div className="flex items-center gap-2">
                <Globe className="h-4 w-4 text-muted-foreground" />
                <span className="text-muted-foreground">Typical response time:</span>
                <span>{formatResponseTime(profile.responseTimeMinutes)}</span>
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
};

function VerificationRow({
  label,
  verified,
  icon,
}: {
  label: string;
  verified: boolean;
  icon: React.ReactNode;
}) {
  return (
    <div className="flex items-center justify-between">
      <span className="flex items-center gap-2 text-muted-foreground">
        {icon}
        {label}
      </span>
      {verified ? (
        <Badge variant="secondary" className="gap-1">
          <ShieldCheck className="h-3 w-3" />
          Verified
        </Badge>
      ) : (
        <Badge variant="outline" className="gap-1 text-muted-foreground">
          <ShieldAlert className="h-3 w-3" />
          Not verified
        </Badge>
      )}
    </div>
  );
}

function formatResponseTime(minutes: number): string {
  if (minutes < 60) return `${minutes} min`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours}h`;
  const days = Math.round(hours / 24);
  return `${days}d`;
}

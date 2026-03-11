import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import {
  Shield,
  Phone,
  IdCard,
  MapPin,
  AlertTriangle,
  Save,
  RotateCcw,
  CheckCircle2,
} from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { useAuthStore } from "@/app/auth/authStore";
import type { UpdateProfileRequest } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Loader } from "@/components/shared/Loader";

type ProfileFormData = {
  firstName: string;
  lastName: string;
  displayName: string;
  phoneNumber: string;
  bio: string;
  profilePhotoUrl: string;
  city: string;
  state: string;
  country: string;
  languages: string;
  occupation: string;
  dateOfBirth: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
};

const toNullable = (value: string): string | null => {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
};

const toFormData = (
  profile: ReturnType<typeof useAuthStore.getState>["user"],
): ProfileFormData => ({
  firstName: profile?.firstName ?? "",
  lastName: profile?.lastName ?? "",
  displayName: profile?.displayName ?? "",
  phoneNumber: profile?.phoneNumber ?? "",
  bio: profile?.bio ?? "",
  profilePhotoUrl: profile?.profilePhotoUrl ?? "",
  city: profile?.city ?? "",
  state: profile?.state ?? "",
  country: profile?.country ?? "",
  languages: profile?.languages ?? "",
  occupation: profile?.occupation ?? "",
  dateOfBirth: profile?.dateOfBirth ?? "",
  emergencyContactName: profile?.emergencyContactName ?? "",
  emergencyContactPhone: profile?.emergencyContactPhone ?? "",
});

export const ProfilePage = () => {
  const user = useAuthStore((state) => state.user);
  const setUser = useAuthStore((state) => state.setUser);
  const [isLoading, setIsLoading] = useState(!user);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const form = useForm<ProfileFormData>({
    defaultValues: toFormData(user),
  });

  useEffect(() => {
    if (user) {
      form.reset(toFormData(user));
      setIsLoading(false);
      return;
    }

    let cancelled = false;
    authApi
      .getCurrentUser()
      .then((profile) => {
        if (!cancelled) {
          setUser(profile);
          form.reset(toFormData(profile));
        }
      })
      .catch(() => {
        if (!cancelled) setError("Could not load profile details.");
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [form, setUser, user]);

  const onSubmit = form.handleSubmit(async (values) => {
    setMessage(null);
    setError(null);

    const payload: UpdateProfileRequest = {
      firstName: toNullable(values.firstName),
      lastName: toNullable(values.lastName),
      displayName: toNullable(values.displayName),
      phoneNumber: toNullable(values.phoneNumber),
      bio: toNullable(values.bio),
      profilePhotoUrl: toNullable(values.profilePhotoUrl),
      city: toNullable(values.city),
      state: toNullable(values.state),
      country: toNullable(values.country),
      languages: toNullable(values.languages),
      occupation: toNullable(values.occupation),
      dateOfBirth: toNullable(values.dateOfBirth),
      emergencyContactName: toNullable(values.emergencyContactName),
      emergencyContactPhone: toNullable(values.emergencyContactPhone),
    };

    try {
      const updated = await authApi.updateProfile(payload);
      setUser(updated);
      form.reset(toFormData(updated));
      setMessage("Profile updated successfully.");
    } catch {
      setError("Could not update profile.");
    }
  });

  if (isLoading) return <Loader label="Loading profile..." />;

  const fullName =
    [user?.firstName, user?.lastName]
      .filter((part) => Boolean(part && part.trim().length > 0))
      .join(" ") ||
    user?.displayName ||
    "No name set";

  const initials = fullName
    .split(" ")
    .filter((s) => s.length > 0)
    .slice(0, 2)
    .map((s) => s[0]?.toUpperCase())
    .join("");

  const hasAvatar = Boolean(user?.profilePhotoUrl?.trim());

  const memberSince = user?.memberSince
    ? new Date(user.memberSince).toLocaleDateString("en-US", {
        month: "long",
        year: "numeric",
      })
    : null;

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Profile</h1>
        <p className="mt-1 text-muted-foreground">
          Manage your personal information and verification status.
        </p>
      </div>

      <div className="grid gap-8 lg:grid-cols-3">
        {/* Sidebar */}
        <div className="space-y-6 lg:col-span-1">
          <Card>
            <CardContent className="p-6">
              <div className="flex flex-col items-center text-center">
                <Avatar className="h-24 w-24 text-lg">
                  {hasAvatar ? (
                    <AvatarImage src={user?.profilePhotoUrl ?? ""} alt={fullName} />
                  ) : null}
                  <AvatarFallback className="text-xl">{initials || "U"}</AvatarFallback>
                </Avatar>
                <h2 className="mt-4 text-xl font-semibold">{fullName}</h2>
                <p className="text-sm text-muted-foreground">{user?.email}</p>
                {memberSince && (
                  <p className="mt-1 text-xs text-muted-foreground">
                    Member since {memberSince}
                  </p>
                )}
                <Badge variant="secondary" className="mt-3">
                  {String(user?.role ?? "N/A")}
                </Badge>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Verification</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <VerificationRow
                icon={Shield}
                label="Email"
                verified={Boolean(user?.isActive)}
              />
              <VerificationRow
                icon={Phone}
                label="Phone"
                verified={Boolean(user?.isPhoneVerified)}
              />
              <VerificationRow
                icon={IdCard}
                label="Government ID"
                verified={Boolean(user?.isGovernmentIdVerified)}
              />
              {user?.city && (
                <div className="flex items-center gap-2 text-sm text-muted-foreground pt-1">
                  <MapPin className="h-4 w-4" />
                  <span>
                    {[user.city, user.state, user.country]
                      .filter(Boolean)
                      .join(", ")}
                  </span>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Main form */}
        <div className="lg:col-span-2">
          <form onSubmit={onSubmit} className="space-y-6">
            {message && (
              <Alert variant="success">
                <CheckCircle2 className="h-4 w-4" />
                <AlertDescription>{message}</AlertDescription>
              </Alert>
            )}
            {error && (
              <Alert variant="destructive">
                <AlertTriangle className="h-4 w-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Personal information</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 sm:grid-cols-2">
                  <FormField label="First name" id="firstName">
                    <Input
                      id="firstName"
                      placeholder="Jane"
                      {...form.register("firstName")}
                    />
                  </FormField>
                  <FormField label="Last name" id="lastName">
                    <Input
                      id="lastName"
                      placeholder="Doe"
                      {...form.register("lastName")}
                    />
                  </FormField>
                  <FormField label="Display name" id="displayName">
                    <Input
                      id="displayName"
                      placeholder="Jane Doe"
                      {...form.register("displayName")}
                    />
                  </FormField>
                  <FormField label="Phone number" id="phoneNumber">
                    <Input
                      id="phoneNumber"
                      placeholder="+1 555 123 4567"
                      {...form.register("phoneNumber")}
                    />
                  </FormField>
                  <FormField label="Occupation" id="occupation">
                    <Input
                      id="occupation"
                      placeholder="Product Manager"
                      {...form.register("occupation")}
                    />
                  </FormField>
                  <FormField label="Languages" id="languages">
                    <Input
                      id="languages"
                      placeholder="English, Spanish"
                      {...form.register("languages")}
                    />
                  </FormField>
                  <FormField label="Date of birth" id="dateOfBirth">
                    <Input
                      id="dateOfBirth"
                      type="date"
                      {...form.register("dateOfBirth")}
                    />
                  </FormField>
                  <FormField label="Profile photo URL" id="profilePhotoUrl">
                    <Input
                      id="profilePhotoUrl"
                      placeholder="https://..."
                      {...form.register("profilePhotoUrl")}
                    />
                  </FormField>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Location</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid gap-4 sm:grid-cols-3">
                  <FormField label="City" id="city">
                    <Input
                      id="city"
                      placeholder="San Francisco"
                      {...form.register("city")}
                    />
                  </FormField>
                  <FormField label="State" id="state">
                    <Input
                      id="state"
                      placeholder="CA"
                      {...form.register("state")}
                    />
                  </FormField>
                  <FormField label="Country" id="country">
                    <Input
                      id="country"
                      placeholder="USA"
                      {...form.register("country")}
                    />
                  </FormField>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Emergency contact</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid gap-4 sm:grid-cols-2">
                  <FormField label="Contact name" id="emergencyContactName">
                    <Input
                      id="emergencyContactName"
                      placeholder="John Doe"
                      {...form.register("emergencyContactName")}
                    />
                  </FormField>
                  <FormField label="Contact phone" id="emergencyContactPhone">
                    <Input
                      id="emergencyContactPhone"
                      placeholder="+1 555 222 3333"
                      {...form.register("emergencyContactPhone")}
                    />
                  </FormField>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-lg">About</CardTitle>
              </CardHeader>
              <CardContent>
                <FormField label="Bio" id="bio">
                  <Textarea
                    id="bio"
                    rows={4}
                    placeholder="Tell others a little about yourself, your interests, and what you're looking for..."
                    {...form.register("bio")}
                  />
                </FormField>
              </CardContent>
            </Card>

            <Separator />

            <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <Button
                type="button"
                variant="outline"
                onClick={() => {
                  form.reset(toFormData(user));
                  setMessage(null);
                  setError(null);
                }}
                disabled={form.formState.isSubmitting || !form.formState.isDirty}
              >
                <RotateCcw className="h-4 w-4" />
                Discard changes
              </Button>
              <Button
                type="submit"
                variant="accent"
                disabled={form.formState.isSubmitting || !form.formState.isDirty}
              >
                <Save className="h-4 w-4" />
                {form.formState.isSubmitting ? "Saving..." : "Save profile"}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

function FormField({
  label,
  id,
  children,
}: {
  label: string;
  id: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-2">
      <Label htmlFor={id}>{label}</Label>
      {children}
    </div>
  );
}

function VerificationRow({
  icon: Icon,
  label,
  verified,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  verified: boolean;
}) {
  return (
    <div className="flex items-center justify-between">
      <div className="flex items-center gap-2">
        <Icon className="h-4 w-4 text-muted-foreground" />
        <span className="text-sm">{label}</span>
      </div>
      {verified ? (
        <CheckCircle2 className="h-4 w-4 text-success" />
      ) : (
        <span className="text-xs text-muted-foreground">Not verified</span>
      )}
    </div>
  );
}

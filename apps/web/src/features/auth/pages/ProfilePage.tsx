import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import {
  Phone,
  IdCard,
  MapPin,
  AlertTriangle,
  Save,
  RotateCcw,
  CheckCircle2,
  Lock,
  Eye,
  EyeOff,
  Mail,
  ChevronRight,
  Globe,
  Wallet,
  CalendarDays,
  Camera,
  Loader2,
  Trash2,
  Link as LinkIcon,
  BellRing,
  LogOut,
  Send,
  Sparkles,
  ShieldCheck,
} from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { useAuthStore } from "@/app/auth/authStore";
import { roleLabel, roles } from "@/app/auth/roles";
import type { UpdateProfileRequest } from "@/api/types";
import { useHostStripeStatus } from "@/features/host-onboarding/hooks/useHostStripe";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Loader } from "@/components/shared/Loader";
import { FormError } from "@/components/shared/FormError";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { TenantEndorsementsPanel } from "@/features/partners/components/TenantEndorsementsPanel";

const BIO_MAX = 500;

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

const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, "Current password is required"),
    newPassword: z
      .string()
      .min(8, "At least 8 characters")
      .regex(/[A-Z]/, "Include at least one uppercase letter")
      .regex(/[0-9]/, "Include at least one number"),
    confirmPassword: z.string().min(1, "Please confirm your new password"),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;

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

const ALLOWED_PHOTO_MIME = [
  "image/jpeg",
  "image/png",
  "image/gif",
  "image/webp",
  "image/heic",
  "image/heif",
];
const MAX_PHOTO_BYTES = 5 * 1024 * 1024;

const PROFILE_TABS = ["profile", "security", "account"] as const;
type ProfileTab = (typeof PROFILE_TABS)[number];

export const ProfilePage = () => {
  const user = useAuthStore((state) => state.user);
  const setUser = useAuthStore((state) => state.setUser);
  const [isLoading, setIsLoading] = useState(!user);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [photoUploading, setPhotoUploading] = useState(false);
  const [photoError, setPhotoError] = useState<string | null>(null);
  const [photoMessage, setPhotoMessage] = useState<string | null>(null);
  const [showPhotoUrlField, setShowPhotoUrlField] = useState(false);

  const [searchParams, setSearchParams] = useSearchParams();
  const tabParam = searchParams.get("tab") as ProfileTab | null;
  const activeTab: ProfileTab =
    tabParam && PROFILE_TABS.includes(tabParam) ? tabParam : "profile";
  const setActiveTab = (next: string) => {
    const params = new URLSearchParams(searchParams);
    if (next === "profile") {
      params.delete("tab");
    } else {
      params.set("tab", next);
    }
    setSearchParams(params, { replace: true });
  };

  const form = useForm<ProfileFormData>({
    defaultValues: toFormData(user),
  });
  const bioValue = form.watch("bio") ?? "";

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

    if (values.bio && values.bio.length > BIO_MAX) {
      setError(`Bio is too long (${values.bio.length}/${BIO_MAX}).`);
      return;
    }

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

  const handlePhotoFile = async (file: File) => {
    setPhotoError(null);
    setPhotoMessage(null);

    if (!ALLOWED_PHOTO_MIME.includes(file.type)) {
      setPhotoError("Unsupported file type. Use JPEG, PNG, GIF, WebP, or HEIC.");
      return;
    }
    if (file.size > MAX_PHOTO_BYTES) {
      setPhotoError("Photo is too large. Keep it under 5 MB.");
      return;
    }

    setPhotoUploading(true);
    try {
      const updated = await authApi.uploadProfilePhoto(file);
      setUser(updated);
      form.reset(toFormData(updated));
      setPhotoMessage("Profile photo updated.");
    } catch (err: unknown) {
      const detail =
        (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
        (err instanceof Error ? err.message : "Could not upload photo.");
      setPhotoError(detail);
    } finally {
      setPhotoUploading(false);
    }
  };

  const handleRemovePhoto = async () => {
    if (!window.confirm("Remove your profile photo?")) return;
    setPhotoError(null);
    setPhotoMessage(null);
    setPhotoUploading(true);
    try {
      const updated = await authApi.removeProfilePhoto();
      setUser(updated);
      form.reset(toFormData(updated));
      setPhotoMessage("Profile photo removed.");
    } catch (err: unknown) {
      const detail =
        (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
        (err instanceof Error ? err.message : "Could not remove photo.");
      setPhotoError(detail);
    } finally {
      setPhotoUploading(false);
    }
  };

  // Inline resend verification email (mirrors the verification page).
  const [resendingEmail, setResendingEmail] = useState(false);
  const [emailCooldownSec, setEmailCooldownSec] = useState(0);
  const [resendMessage, setResendMessage] = useState<string | null>(null);
  useEffect(() => {
    if (emailCooldownSec <= 0) return;
    const t = window.setTimeout(() => setEmailCooldownSec((n) => n - 1), 1_000);
    return () => window.clearTimeout(t);
  }, [emailCooldownSec]);
  const handleResendVerification = async () => {
    if (!user?.email || resendingEmail || emailCooldownSec > 0) return;
    setResendingEmail(true);
    setResendMessage(null);
    try {
      await authApi.resendVerification(user.email);
      setResendMessage("Verification link sent. Check your inbox.");
      setEmailCooldownSec(45);
    } catch {
      setResendMessage("Could not resend the verification email.");
    } finally {
      setResendingEmail(false);
    }
  };

  // Profile completeness — counts the fields that meaningfully build trust.
  const completeness = useMemo(() => {
    if (!user) return { filled: 0, total: 1, percent: 0, missing: [] as string[] };
    const checks: { label: string; ok: boolean }[] = [
      { label: "Profile photo", ok: Boolean(user.profilePhotoUrl?.trim()) },
      { label: "First & last name", ok: Boolean(user.firstName?.trim() && user.lastName?.trim()) },
      { label: "Phone number", ok: Boolean(user.phoneNumber?.trim()) },
      { label: "Date of birth", ok: Boolean(user.dateOfBirth) },
      { label: "City / location", ok: Boolean(user.city?.trim()) },
      { label: "Bio", ok: Boolean(user.bio?.trim() && user.bio.trim().length >= 40) },
      { label: "Email verified", ok: Boolean(user.emailConfirmed ?? user.isActive) },
      { label: "Government ID verified", ok: Boolean(user.isGovernmentIdVerified) },
    ];
    const filled = checks.filter((c) => c.ok).length;
    const missing = checks.filter((c) => !c.ok).map((c) => c.label);
    return {
      filled,
      total: checks.length,
      percent: Math.round((filled / checks.length) * 100),
      missing,
    };
  }, [user]);

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
  const emailVerified = Boolean(user?.emailConfirmed ?? user?.isActive);

  const memberSince = user?.memberSince
    ? new Date(user.memberSince).toLocaleDateString("en-US", {
        month: "long",
        year: "numeric",
      })
    : null;

  const isHostProfile = String(user?.role) === roles.member || String(user?.role) === roles.platformAdmin;
  const locationText = [user?.city, user?.state, user?.country].filter(Boolean).join(", ");
  const isDirty = form.formState.isDirty;

  return (
    <div className="space-y-8 pb-24">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Profile</h1>
        <p className="mt-1 text-muted-foreground">
          Manage your personal information, security, and account settings.
        </p>
      </div>

      {/* Completeness meter */}
      {completeness.percent < 100 && (
        <Card className="border-primary/30 bg-primary/5">
          <CardContent className="flex flex-col gap-3 p-5 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-start gap-3">
              <div className="rounded-full bg-primary/10 p-2 text-primary">
                <Sparkles className="h-5 w-5" />
              </div>
              <div className="min-w-0">
                <div className="flex items-baseline gap-2">
                  <p className="font-semibold">Your profile is {completeness.percent}% complete</p>
                  <span className="text-xs text-muted-foreground">
                    {completeness.filled}/{completeness.total} done
                  </span>
                </div>
                <div className="mt-2 h-2 w-full max-w-xs rounded-full bg-muted">
                  <div
                    className="h-full rounded-full bg-primary transition-all duration-500"
                    style={{ width: `${completeness.percent}%` }}
                  />
                </div>
                {completeness.missing.length > 0 && (
                  <p className="mt-2 text-sm text-muted-foreground">
                    Add: {completeness.missing.slice(0, 3).join(", ")}
                    {completeness.missing.length > 3 ? ` and ${completeness.missing.length - 3} more` : ""}
                  </p>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid gap-8 lg:grid-cols-3">
        {/* Sidebar */}
        <div className="space-y-6 lg:col-span-1">
          <Card className={isHostProfile ? "border-primary/20 shadow-sm" : undefined}>
            <CardContent className="p-6">
              <div className="flex flex-col items-center text-center">
                <div className="relative group">
                  <Avatar className="h-24 w-24 text-lg">
                    {hasAvatar ? (
                      <AvatarImage src={user?.profilePhotoUrl ?? ""} alt={fullName} />
                    ) : null}
                    <AvatarFallback className="text-xl">{initials || "U"}</AvatarFallback>
                  </Avatar>
                  <label
                    className={`absolute inset-0 flex items-center justify-center rounded-full bg-black/55 text-white text-xs font-medium transition-opacity ${
                      photoUploading ? "opacity-100 cursor-wait" : "opacity-0 group-hover:opacity-100 cursor-pointer"
                    }`}
                    title={photoUploading ? "Uploading..." : "Change profile photo"}
                  >
                    {photoUploading ? (
                      <Loader2 className="h-5 w-5 animate-spin" />
                    ) : (
                      <span className="flex flex-col items-center gap-1">
                        <Camera className="h-5 w-5" />
                        <span>Change</span>
                      </span>
                    )}
                    <input
                      type="file"
                      accept={ALLOWED_PHOTO_MIME.join(",")}
                      className="sr-only"
                      disabled={photoUploading}
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        const inputEl = e.target;
                        if (file) {
                          void handlePhotoFile(file);
                        }
                        inputEl.value = "";
                      }}
                    />
                  </label>
                </div>

                <div className="mt-3 flex flex-col items-center gap-1">
                  <p className="text-[11px] text-muted-foreground">
                    Hover the avatar to change. JPEG, PNG, GIF, WebP, HEIC up to 5 MB.
                  </p>
                  {hasAvatar && (
                    <button
                      type="button"
                      onClick={() => void handleRemovePhoto()}
                      disabled={photoUploading}
                      className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-destructive disabled:opacity-50"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                      Remove photo
                    </button>
                  )}
                </div>

                {photoError && (
                  <p className="mt-2 text-xs text-destructive">{photoError}</p>
                )}
                {photoMessage && !photoError && (
                  <p className="mt-2 text-xs text-success">{photoMessage}</p>
                )}

                <h2 className="mt-4 text-xl font-semibold">{fullName}</h2>
                <p className="text-sm text-muted-foreground">{user?.email}</p>
                {isHostProfile && locationText && (
                  <p className="mt-2 flex items-center gap-1 text-sm text-muted-foreground">
                    <MapPin className="h-4 w-4" />
                    {locationText}
                  </p>
                )}
                {memberSince && (
                  <p className="mt-2 flex items-center gap-1 text-xs text-muted-foreground">
                    <CalendarDays className="h-3.5 w-3.5" />
                    Member since {memberSince}
                  </p>
                )}
                <Badge variant="secondary" className="mt-3">
                  {roleLabel(String(user?.role ?? "N/A"))}
                </Badge>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Trust and verification</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <VerificationRow
                icon={Mail}
                label="Email"
                verified={emailVerified}
                action={
                  !emailVerified && user?.email ? (
                    <button
                      type="button"
                      onClick={() => void handleResendVerification()}
                      disabled={resendingEmail || emailCooldownSec > 0}
                      className="inline-flex items-center gap-1 text-xs text-primary hover:underline disabled:opacity-50"
                    >
                      {resendingEmail ? (
                        <Loader2 className="h-3 w-3 animate-spin" />
                      ) : (
                        <Send className="h-3 w-3" />
                      )}
                      {emailCooldownSec > 0 ? `Resend in ${emailCooldownSec}s` : "Resend"}
                    </button>
                  ) : undefined
                }
              />
              {resendMessage && (
                <p className="text-xs text-muted-foreground">{resendMessage}</p>
              )}
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
              <Separator className="my-2" />
              <Link
                to="/app/verification"
                className="flex items-center justify-between text-sm text-primary hover:underline group"
              >
                <span>{isHostProfile ? "Manage trust checks" : "Manage verification"}</span>
                <ChevronRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
              </Link>
              {isHostProfile && (
                <Link
                  to="/app/payout-setup"
                  className="flex items-center justify-between text-sm text-primary hover:underline group"
                >
                  <span className="inline-flex items-center gap-1.5">
                    <Wallet className="h-4 w-4" />
                    Set up payouts
                  </span>
                  <ChevronRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
                </Link>
              )}
            </CardContent>
          </Card>

          {isHostProfile && (
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-base">About this host</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm text-muted-foreground">
                <div className="flex items-start gap-2">
                  <Globe className="mt-0.5 h-4 w-4 shrink-0" />
                  <span>{user?.languages || "Languages not added yet"}</span>
                </div>
                <div className="flex items-start gap-2">
                  <IdCard className="mt-0.5 h-4 w-4 shrink-0" />
                  <span>{user?.occupation || "Occupation not added yet"}</span>
                </div>
                <p className="pt-1 text-foreground">
                  {user?.bio?.trim() ? user.bio : "Add a short host bio to build trust with tenants."}
                </p>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Main content */}
        <div className="lg:col-span-2">
          {isHostProfile && <HostingStatusCard />}

          <Tabs value={activeTab} onValueChange={setActiveTab}>
            <TabsList>
              <TabsTrigger value="profile">Profile</TabsTrigger>
              <TabsTrigger value="security">Security</TabsTrigger>
              <TabsTrigger value="account">Account</TabsTrigger>
            </TabsList>

            <TabsContent value="profile">
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
                        <Input id="firstName" placeholder="Jane" {...form.register("firstName")} />
                      </FormField>
                      <FormField label="Last name" id="lastName">
                        <Input id="lastName" placeholder="Doe" {...form.register("lastName")} />
                      </FormField>
                      <FormField label="Display name" id="displayName">
                        <Input id="displayName" placeholder="Jane Doe" {...form.register("displayName")} />
                      </FormField>
                      <FormField label="Phone number" id="phoneNumber" hint="Use international format, e.g. +1 555 123 4567">
                        <Input id="phoneNumber" placeholder="+1 555 123 4567" {...form.register("phoneNumber")} />
                      </FormField>
                      <FormField label="Occupation" id="occupation">
                        <Input id="occupation" placeholder="Product Manager" {...form.register("occupation")} />
                      </FormField>
                      <FormField label="Languages" id="languages">
                        <Input id="languages" placeholder="English, Spanish" {...form.register("languages")} />
                      </FormField>
                      <FormField label="Date of birth" id="dateOfBirth">
                        <Input id="dateOfBirth" type="date" {...form.register("dateOfBirth")} />
                      </FormField>
                    </div>

                    <div className="rounded-lg border bg-muted/40 p-3">
                      <p className="text-sm font-medium">Profile photo</p>
                      <p className="text-xs text-muted-foreground mt-0.5">
                        Use the avatar in the sidebar to upload an image. Or, if you host a photo elsewhere,
                        you can paste a public URL.
                      </p>
                      <button
                        type="button"
                        className="mt-2 inline-flex items-center gap-1 text-xs text-primary hover:underline"
                        onClick={() => setShowPhotoUrlField((v) => !v)}
                      >
                        <LinkIcon className="h-3.5 w-3.5" />
                        {showPhotoUrlField ? "Hide URL field" : "Use a URL instead"}
                      </button>
                      {showPhotoUrlField && (
                        <div className="mt-3">
                          <FormField label="Profile photo URL" id="profilePhotoUrl">
                            <Input
                              id="profilePhotoUrl"
                              placeholder="https://..."
                              {...form.register("profilePhotoUrl")}
                            />
                          </FormField>
                        </div>
                      )}
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
                        <Input id="city" placeholder="San Francisco" {...form.register("city")} />
                      </FormField>
                      <FormField label="State" id="state">
                        <Input id="state" placeholder="CA" {...form.register("state")} />
                      </FormField>
                      <FormField label="Country" id="country">
                        <Input id="country" placeholder="USA" {...form.register("country")} />
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
                        <Input id="emergencyContactName" placeholder="John Doe" {...form.register("emergencyContactName")} />
                      </FormField>
                      <FormField label="Contact phone" id="emergencyContactPhone">
                        <Input id="emergencyContactPhone" placeholder="+1 555 222 3333" {...form.register("emergencyContactPhone")} />
                      </FormField>
                    </div>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle className="text-lg">About</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <FormField
                      label="Bio"
                      id="bio"
                      hint={`${bioValue.length}/${BIO_MAX} characters${bioValue.length > BIO_MAX ? " — too long" : ""}`}
                      hintClass={bioValue.length > BIO_MAX ? "text-destructive" : undefined}
                    >
                      <Textarea
                        id="bio"
                        rows={4}
                        maxLength={BIO_MAX + 50}
                        placeholder="Tell others a little about yourself, your interests, and what you're looking for..."
                        {...form.register("bio")}
                      />
                    </FormField>
                  </CardContent>
                </Card>

                <Separator />

                {/* Inline footer kept for accessibility / mobile fallback. */}
                <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => {
                      form.reset(toFormData(user));
                      setMessage(null);
                      setError(null);
                    }}
                    disabled={form.formState.isSubmitting || !isDirty}
                  >
                    <RotateCcw className="h-4 w-4" />
                    Discard changes
                  </Button>
                  <Button
                    type="submit"
                    variant="accent"
                    disabled={form.formState.isSubmitting || !isDirty || bioValue.length > BIO_MAX}
                  >
                    <Save className="h-4 w-4" />
                    {form.formState.isSubmitting ? "Saving..." : "Save profile"}
                  </Button>
                </div>

                {/* Sticky save bar (only visible while form is dirty) */}
                {isDirty && (
                  <div className="fixed inset-x-0 bottom-0 z-40 border-t bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/80 shadow-[0_-2px_12px_rgba(0,0,0,0.06)]">
                    <div className="mx-auto flex max-w-5xl items-center justify-between gap-3 px-4 py-3 sm:px-6">
                      <p className="text-sm text-muted-foreground">You have unsaved changes</p>
                      <div className="flex items-center gap-2">
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            form.reset(toFormData(user));
                            setMessage(null);
                            setError(null);
                          }}
                          disabled={form.formState.isSubmitting}
                        >
                          Discard
                        </Button>
                        <Button
                          type="submit"
                          variant="accent"
                          size="sm"
                          disabled={form.formState.isSubmitting || bioValue.length > BIO_MAX}
                          className="gap-2"
                        >
                          <Save className="h-4 w-4" />
                          {form.formState.isSubmitting ? "Saving..." : "Save profile"}
                        </Button>
                      </div>
                    </div>
                  </div>
                )}
              </form>
            </TabsContent>

            <TabsContent value="security">
              <div className="space-y-6">
                <ChangePasswordSection />
                <SetPasswordSection email={user?.email ?? ""} />
              </div>
            </TabsContent>

            <TabsContent value="account">
              <div className="space-y-6">
                <TenantEndorsementsPanel />
                <AccountAndDataSection />
              </div>
            </TabsContent>
          </Tabs>
        </div>
      </div>
    </div>
  );
};

function HostingStatusCard() {
  const { data: stripeStatus } = useHostStripeStatus();
  const user = useAuthStore((s) => s.user);

  const payoutLabel = stripeStatus
    ? stripeStatus.payoutsEnabled
      ? "Active"
      : stripeStatus.onboardingStatus === "Pending"
        ? "Onboarding"
        : stripeStatus.onboardingStatus === "Restricted"
          ? "Restricted"
          : "Set up needed"
    : "Set up needed";
  const payoutGood = stripeStatus?.payoutsEnabled === true;
  const idGood = Boolean(user?.isGovernmentIdVerified);
  const phoneGood = Boolean(user?.isPhoneVerified);

  return (
    <Card className="mb-6 border-primary/20 bg-primary/5">
      <CardHeader>
        <CardTitle className="text-lg">Hosting profile</CardTitle>
        <CardDescription>
          Keep this profile complete to improve booking confidence and payout readiness.
        </CardDescription>
      </CardHeader>
      <CardContent className="grid gap-3 sm:grid-cols-3">
        <HostStatTile
          label="Identity"
          value={idGood ? "Verified" : "Pending"}
          good={idGood}
          to="/app/verification#kyc"
        />
        <HostStatTile
          label="Phone"
          value={phoneGood ? "Verified" : "Pending"}
          good={phoneGood}
          to="/app/verification#phone"
        />
        <HostStatTile
          label="Payouts"
          value={payoutLabel}
          good={payoutGood}
          to="/app/payout-setup"
        />
      </CardContent>
    </Card>
  );
}

function HostStatTile({
  label,
  value,
  good,
  to,
}: {
  label: string;
  value: string;
  good: boolean;
  to: string;
}) {
  return (
    <Link
      to={to}
      className="group rounded-lg border bg-background p-3 transition-colors hover:border-primary/40 hover:bg-muted/40"
    >
      <p className="text-xs text-muted-foreground">{label}</p>
      <div className="mt-1 flex items-center justify-between gap-2">
        <p className={`text-sm font-medium ${good ? "text-emerald-600" : ""}`}>{value}</p>
        <ChevronRight className="h-4 w-4 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
      </div>
    </Link>
  );
}

function AccountAndDataSection() {
  const navigate = useNavigate();
  const [signingOut, setSigningOut] = useState(false);

  const handleSignOut = async () => {
    if (!window.confirm("Sign out of this device?")) return;
    setSigningOut(true);
    try {
      await authApi.logout();
      navigate("/auth/login", { replace: true });
    } finally {
      setSigningOut(false);
    }
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <BellRing className="h-5 w-5" />
            Notifications
          </CardTitle>
          <CardDescription>
            Choose which emails, SMS, and in-app alerts you want to receive.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Link
            to="/app/notification-preferences"
            className="inline-flex items-center gap-2 text-sm text-primary hover:underline"
          >
            Open notification preferences
            <ChevronRight className="h-4 w-4" />
          </Link>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <ShieldCheck className="h-5 w-5" />
            Privacy & data
          </CardTitle>
          <CardDescription>
            Review your trust ledger and how your data is used across the platform.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-2">
          <Link
            to="/app/trust-ledger"
            className="inline-flex items-center gap-2 text-sm text-primary hover:underline"
          >
            View your trust ledger
            <ChevronRight className="h-4 w-4" />
          </Link>
        </CardContent>
      </Card>

      <Card className="border-destructive/30">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <LogOut className="h-5 w-5" />
            Sign out
          </CardTitle>
          <CardDescription>
            Sign out of this device. Your other devices stay signed in.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button
            variant="outline"
            onClick={() => void handleSignOut()}
            disabled={signingOut}
            className="gap-2"
          >
            <LogOut className="h-4 w-4" />
            {signingOut ? "Signing out..." : "Sign out of this device"}
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}

function ChangePasswordSection() {
  const [message, setMessage] = useState<string | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);
  const [showNew, setShowNew] = useState(false);

  const form = useForm<ChangePasswordFormData>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" },
  });

  const onSubmit = form.handleSubmit(async (data) => {
    setMessage(null);
    setServerError(null);

    try {
      await authApi.changePassword({
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
      });
      setMessage("Password changed successfully.");
      form.reset();
    } catch {
      setServerError("Could not change password. Ensure your current password is correct.");
    }
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <Lock className="h-5 w-5" />
          Change password
        </CardTitle>
        <CardDescription>
          Update your password to keep your account secure.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={onSubmit} className="space-y-4 max-w-md">
          {message && (
            <Alert variant="success">
              <CheckCircle2 className="h-4 w-4" />
              <AlertDescription>{message}</AlertDescription>
            </Alert>
          )}
          {serverError && (
            <Alert variant="destructive">
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>{serverError}</AlertDescription>
            </Alert>
          )}

          <FormField label="Current password" id="currentPassword">
            <Input
              id="currentPassword"
              type="password"
              placeholder="Enter current password"
              disabled={form.formState.isSubmitting}
              {...form.register("currentPassword")}
            />
            <FormError message={form.formState.errors.currentPassword?.message} />
          </FormField>

          <FormField label="New password" id="newPassword">
            <div className="relative">
              <Input
                id="newPassword"
                type={showNew ? "text" : "password"}
                placeholder="Enter new password"
                className="pr-10"
                disabled={form.formState.isSubmitting}
                {...form.register("newPassword")}
              />
              <button
                type="button"
                onClick={() => setShowNew((v) => !v)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground cursor-pointer"
              >
                {showNew ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              </button>
            </div>
            <FormError message={form.formState.errors.newPassword?.message} />
          </FormField>

          <FormField label="Confirm new password" id="confirmPassword">
            <Input
              id="confirmPassword"
              type="password"
              placeholder="Repeat new password"
              disabled={form.formState.isSubmitting}
              {...form.register("confirmPassword")}
            />
            <FormError message={form.formState.errors.confirmPassword?.message} />
          </FormField>

          <Button
            type="submit"
            variant="accent"
            disabled={form.formState.isSubmitting}
          >
            <Lock className="h-4 w-4" />
            {form.formState.isSubmitting ? "Changing..." : "Change password"}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function SetPasswordSection({ email }: { email: string }) {
  const [sent, setSent] = useState(false);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSendLink = async () => {
    if (!email) return;
    setSending(true);
    setError(null);
    try {
      await authApi.forgotPassword({ email });
      setSent(true);
    } catch {
      setError("Failed to send reset link. Please try again.");
    } finally {
      setSending(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <Mail className="h-5 w-5" />
          Set a password
        </CardTitle>
        <CardDescription>
          Signed up with Google or another provider? Set an email password so you can sign in either way.
        </CardDescription>
      </CardHeader>
      <CardContent>
        {sent ? (
          <Alert variant="success">
            <CheckCircle2 className="h-4 w-4" />
            <AlertDescription>
              A password reset link has been sent to <strong>{email}</strong>. Check your inbox and follow the link to set your password.
            </AlertDescription>
          </Alert>
        ) : (
          <div className="space-y-3 max-w-md">
            {error && (
              <Alert variant="destructive">
                <AlertTriangle className="h-4 w-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}
            <p className="text-sm text-muted-foreground">
              We'll send a password setup link to <strong>{email}</strong>.
            </p>
            <Button
              variant="outline"
              onClick={handleSendLink}
              disabled={sending || !email}
            >
              <Mail className="h-4 w-4" />
              {sending ? "Sending..." : "Send password setup link"}
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function FormField({
  label,
  id,
  children,
  hint,
  hintClass,
}: {
  label: string;
  id: string;
  children: React.ReactNode;
  hint?: string;
  hintClass?: string;
}) {
  return (
    <div className="space-y-2">
      <Label htmlFor={id}>{label}</Label>
      {children}
      {hint && (
        <p className={`text-xs ${hintClass ?? "text-muted-foreground"}`}>{hint}</p>
      )}
    </div>
  );
}

function VerificationRow({
  icon: Icon,
  label,
  verified,
  action,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  verified: boolean;
  action?: React.ReactNode;
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
        <div className="flex items-center gap-2">
          <span className="text-xs text-muted-foreground">Not verified</span>
          {action}
        </div>
      )}
    </div>
  );
}

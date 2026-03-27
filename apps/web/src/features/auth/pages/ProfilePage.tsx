import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  Shield,
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
} from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { useAuthStore } from "@/app/auth/authStore";
import { roleLabel } from "@/app/auth/roles";
import type { UpdateProfileRequest } from "@/api/types";
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

export const ProfilePage = () => {
  const user = useAuthStore((state) => state.user);
  const setUser = useAuthStore((state) => state.setUser);
  const [isLoading, setIsLoading] = useState(!user);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState("profile");

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
          Manage your personal information and account security.
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
                  {roleLabel(String(user?.role ?? "N/A"))}
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

        {/* Main content */}
        <div className="lg:col-span-2">
          <Tabs value={activeTab} onValueChange={setActiveTab}>
            <TabsList>
              <TabsTrigger value="profile">Profile</TabsTrigger>
              <TabsTrigger value="security">Security</TabsTrigger>
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
                      <FormField label="Phone number" id="phoneNumber">
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
                      <FormField label="Profile photo URL" id="profilePhotoUrl">
                        <Input id="profilePhotoUrl" placeholder="https://..." {...form.register("profilePhotoUrl")} />
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
            </TabsContent>

            <TabsContent value="security">
              <div className="space-y-6">
                <ChangePasswordSection />
                <SetPasswordSection email={user?.email ?? ""} />
              </div>
            </TabsContent>
          </Tabs>
        </div>
      </div>
    </div>
  );
};

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

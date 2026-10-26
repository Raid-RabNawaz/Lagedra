import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Home, Loader2 } from "lucide-react";
import { useAuthStore } from "@/app/auth/authStore";
import { useModeStore } from "@/app/auth/modeStore";
import { authApi } from "@/features/auth/services/authApi";
import { useSetHostKind } from "@/features/verification/hooks/useVerification";
import type { HostKind } from "@/api/types";
import { getApiErrorMessage } from "@/api/errors";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { cn } from "@/lib/utils";

const MIN_BIO = 40;

/**
 * Unlocks hosting on a guest account. After this, the travelling / hosting
 * switch stays available.
 */
export function BecomeAHostPage() {
  const user = useAuthStore((s) => s.user);
  const navigate = useNavigate();
  const setMode = useModeStore((s) => s.setMode);
  const setHostKind = useSetHostKind();

  const [kind, setKind] = useState<HostKind>(user?.hostKind ?? "Individual");
  const [companyName, setCompanyName] = useState("");
  const [city, setCity] = useState(user?.city ?? "");
  const [state, setState] = useState(user?.state ?? "");
  const [country, setCountry] = useState(user?.country ?? "");
  const [languages, setLanguages] = useState(user?.languages ?? "");
  const [occupation, setOccupation] = useState(user?.occupation ?? "");
  const [bio, setBio] = useState(user?.bio ?? "");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (user?.hostingEnabled) {
      navigate("/app", { replace: true });
    }
  }, [user?.hostingEnabled, navigate]);

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    if (!city.trim() || !country.trim()) {
      setError("Enter the city and country where you host.");
      return;
    }
    if (bio.trim().length < MIN_BIO) {
      setError(`Add a short bio of at least ${MIN_BIO} characters so guests know who they're staying with.`);
      return;
    }
    if (kind === "PropertyManagementCompany" && !companyName.trim()) {
      setError("Enter the property management company's legal name.");
      return;
    }

    setSubmitting(true);
    try {
      await setHostKind.mutateAsync(kind);
      await authApi.enableHosting({
        city: city.trim(),
        state: state.trim() || undefined,
        country: country.trim(),
        bio: bio.trim(),
        languages: languages.trim() || undefined,
        occupation: occupation.trim() || undefined,
        hostKind: kind,
        companyName: kind === "PropertyManagementCompany" ? companyName.trim() : undefined,
      });
      setMode("host");
      navigate("/app", { replace: true });
    } catch (e) {
      setError(getApiErrorMessage(e) || "Could not enable hosting. Please try again.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Become a host</h1>
        <p className="mt-1 text-muted-foreground">
          Tell guests who you are. After this you can switch between travelling and hosting whenever you like.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <Home className="h-5 w-5" />
            Host details
          </CardTitle>
          <CardDescription>
            This is separate from booking a stay. Individuals verify with an ID later. Property managers verify the business instead.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={(e) => void onSubmit(e)} className="space-y-5">
            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <div className="grid gap-2 sm:grid-cols-2">
              <KindOption
                title="Individual"
                body="You list homes in your own name."
                selected={kind === "Individual"}
                onSelect={() => setKind("Individual")}
              />
              <KindOption
                title="Property management company"
                body="You list on behalf of a company. No personal ID."
                selected={kind === "PropertyManagementCompany"}
                onSelect={() => setKind("PropertyManagementCompany")}
              />
            </div>

            {kind === "PropertyManagementCompany" && (
              <div className="space-y-2">
                <Label htmlFor="host-company">Legal business name</Label>
                <Input
                  id="host-company"
                  value={companyName}
                  onChange={(e) => setCompanyName(e.target.value)}
                  placeholder="Northwind Property Management LLC"
                />
              </div>
            )}

            <div className="grid gap-4 sm:grid-cols-3">
              <div className="space-y-2">
                <Label htmlFor="host-city">City</Label>
                <Input id="host-city" value={city} onChange={(e) => setCity(e.target.value)} placeholder="Los Angeles" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="host-state">State</Label>
                <Input id="host-state" value={state} onChange={(e) => setState(e.target.value)} placeholder="CA" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="host-country">Country</Label>
                <Input id="host-country" value={country} onChange={(e) => setCountry(e.target.value)} placeholder="United States" />
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="host-languages">Languages</Label>
                <Input
                  id="host-languages"
                  value={languages}
                  onChange={(e) => setLanguages(e.target.value)}
                  placeholder="English, Spanish"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="host-occupation">Occupation</Label>
                <Input
                  id="host-occupation"
                  value={occupation}
                  onChange={(e) => setOccupation(e.target.value)}
                  placeholder="Property manager"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="host-bio">About you</Label>
              <Textarea
                id="host-bio"
                value={bio}
                onChange={(e) => setBio(e.target.value)}
                rows={5}
                maxLength={500}
                placeholder="A few sentences about you and the kind of stays you offer."
              />
              <p className="text-xs text-muted-foreground">{bio.trim().length}/500 · at least {MIN_BIO} characters</p>
            </div>

            <Button type="submit" disabled={submitting} className="gap-2">
              {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
              {submitting ? "Saving..." : "Start hosting"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

function KindOption({
  title,
  body,
  selected,
  onSelect,
}: {
  title: string;
  body: string;
  selected: boolean;
  onSelect: () => void;
}) {
  return (
    <button
      type="button"
      aria-pressed={selected}
      onClick={onSelect}
      className={cn(
        "rounded-lg border p-3 text-left transition-colors",
        selected ? "border-primary bg-primary/5" : "hover:bg-muted/40",
      )}
    >
      <p className="text-sm font-medium">{title}</p>
      <p className="mt-1 text-xs text-muted-foreground">{body}</p>
    </button>
  );
}

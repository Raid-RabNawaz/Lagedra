import { useEffect, useState } from "react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { UpsertSeoPageRequest } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Loader } from "@/components/shared/Loader";

const KNOWN_SLUGS = [
  "how-it-works",
  "pricing",
  "about",
  "contact",
  "faq",
  "terms",
  "tc",
  "privacy",
  "sms",
] as const;

type PageState = UpsertSeoPageRequest & { saving: boolean; saved: boolean };

const defaultState = (): PageState => ({
  metaTitle: "",
  metaDescription: "",
  noIndex: false,
  saving: false,
  saved: false,
});

export const SeoPage = () => {
  const [pages, setPages] = useState<Record<string, PageState>>({});
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      const result: Record<string, PageState> = {};
      await Promise.all(
        KNOWN_SLUGS.map(async (slug) => {
          try {
            const dto = await adminApi.getSeoPage(slug);
            result[slug] = {
              metaTitle: dto.metaTitle,
              metaDescription: dto.metaDescription,
              noIndex: dto.noIndex,
              saving: false,
              saved: false,
            };
          } catch {
            result[slug] = defaultState();
          }
        }),
      );
      setPages(result);
      setIsLoading(false);
    };
    void load();
  }, []);

  const updateField = (slug: string, field: keyof UpsertSeoPageRequest, value: unknown) => {
    setPages((prev) => ({
      ...prev,
      [slug]: { ...prev[slug], [field]: value, saved: false },
    }));
  };

  const handleSave = async (slug: string) => {
    const state = pages[slug];
    if (!state) return;
    setPages((prev) => ({ ...prev, [slug]: { ...prev[slug], saving: true } }));
    try {
      await adminApi.upsertSeoPage(slug, {
        metaTitle: state.metaTitle,
        metaDescription: state.metaDescription,
        noIndex: state.noIndex,
      });
      setPages((prev) => ({
        ...prev,
        [slug]: { ...prev[slug], saving: false, saved: true },
      }));
    } catch {
      setPages((prev) => ({ ...prev, [slug]: { ...prev[slug], saving: false } }));
    }
  };

  if (isLoading) return <Loader label="Loading SEO pages..." />;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">SEO Pages</h1>
        <p className="mt-1 text-muted-foreground">
          Edit static page metadata for SEO.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        {KNOWN_SLUGS.map((slug) => {
          const state = pages[slug] ?? defaultState();
          return (
            <Card key={slug}>
              <CardHeader className="pb-3">
                <CardTitle className="text-base font-mono">/{slug}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="space-y-1">
                  <Label htmlFor={`${slug}-title`}>Meta Title</Label>
                  <Input
                    id={`${slug}-title`}
                    value={state.metaTitle}
                    onChange={(e) => updateField(slug, "metaTitle", e.target.value)}
                    placeholder="Page title"
                  />
                </div>
                <div className="space-y-1">
                  <Label htmlFor={`${slug}-desc`}>Meta Description</Label>
                  <Textarea
                    id={`${slug}-desc`}
                    rows={2}
                    value={state.metaDescription}
                    onChange={(e) => updateField(slug, "metaDescription", e.target.value)}
                    placeholder="Page description"
                  />
                </div>
                <div className="flex items-center gap-2">
                  <Checkbox
                    id={`${slug}-noindex`}
                    checked={state.noIndex}
                    onCheckedChange={(checked) => updateField(slug, "noIndex", checked)}
                  />
                  <Label htmlFor={`${slug}-noindex`} className="text-sm font-normal cursor-pointer">
                    noIndex
                  </Label>
                </div>
                <div className="flex items-center justify-between pt-1">
                  {state.saved && (
                    <span className="text-sm text-success">Saved</span>
                  )}
                  <Button
                    size="sm"
                    className="ml-auto"
                    disabled={state.saving}
                    onClick={() => void handleSave(slug)}
                  >
                    {state.saving ? "Saving..." : "Save"}
                  </Button>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>
    </div>
  );
};

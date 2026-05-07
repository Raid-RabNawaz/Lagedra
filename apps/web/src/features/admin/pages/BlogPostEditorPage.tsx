import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { adminApi } from "@/features/admin/services/adminApi";
import type { CreateBlogPostRequest } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Loader } from "@/components/shared/Loader";

const empty: CreateBlogPostRequest = {
  slug: "",
  title: "",
  excerpt: "",
  content: "",
  tags: [],
  metaTitle: "",
  metaDescription: "",
  ogImageUrl: null,
  readingTimeMinutes: 5,
};

export const BlogPostEditorPage = () => {
  const { postId } = useParams<{ postId: string }>();
  const navigate = useNavigate();
  const isEdit = Boolean(postId);

  const [form, setForm] = useState<CreateBlogPostRequest>({ ...empty });
  const [tagsInput, setTagsInput] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!postId) return;
    setIsLoading(true);
    adminApi
      .getBlogPost(postId)
      .then((post) => {
        setForm({
          slug: post.slug,
          title: post.title,
          excerpt: post.excerpt,
          content: post.content,
          tags: post.tags,
          metaTitle: post.metaTitle,
          metaDescription: post.metaDescription,
          ogImageUrl: post.ogImageUrl,
          readingTimeMinutes: post.readingTimeMinutes,
        });
        setTagsInput(post.tags.join(", "));
      })
      .catch(() => setError("Failed to load post."))
      .finally(() => setIsLoading(false));
  }, [postId]);

  const update = (field: keyof CreateBlogPostRequest, value: unknown) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleTagsChange = (raw: string) => {
    setTagsInput(raw);
    update(
      "tags",
      raw
        .split(",")
        .map((t) => t.trim())
        .filter(Boolean),
    );
  };

  const handleSave = async () => {
    setIsSaving(true);
    setError(null);
    try {
      if (isEdit && postId) {
        await adminApi.updateBlogPost(postId, form);
      } else {
        await adminApi.createBlogPost(form);
      }
      navigate("/app/admin/blog");
    } catch {
      setError("Failed to save post.");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) return <Loader label="Loading post..." />;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">
          {isEdit ? "Edit Blog Post" : "New Blog Post"}
        </h1>
      </div>

      {error && <p className="text-destructive">{error}</p>}

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Post Details</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="slug">Slug</Label>
              <Input
                id="slug"
                value={form.slug}
                onChange={(e) => update("slug", e.target.value)}
                placeholder="my-blog-post"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="title">Title</Label>
              <Input
                id="title"
                value={form.title}
                onChange={(e) => update("title", e.target.value)}
                placeholder="Post title"
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="excerpt">Excerpt</Label>
            <Textarea
              id="excerpt"
              rows={3}
              value={form.excerpt}
              onChange={(e) => update("excerpt", e.target.value)}
              placeholder="Short description..."
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="content">Content (Markdown)</Label>
            <Textarea
              id="content"
              rows={15}
              value={form.content}
              onChange={(e) => update("content", e.target.value)}
              placeholder="Write your post content here..."
              className="font-mono text-sm"
            />
          </div>

          {form.content && (
            <div className="space-y-2">
              <Label>Preview</Label>
              <div className="rounded-lg border bg-muted/50 p-4 whitespace-pre-wrap text-sm">
                {form.content}
              </div>
            </div>
          )}

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="tags">Tags (comma-separated)</Label>
              <Input
                id="tags"
                value={tagsInput}
                onChange={(e) => handleTagsChange(e.target.value)}
                placeholder="news, updates, tips"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="readingTime">Reading Time (minutes)</Label>
              <Input
                id="readingTime"
                type="number"
                min={1}
                value={form.readingTimeMinutes}
                onChange={(e) => update("readingTimeMinutes", Number(e.target.value))}
              />
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="metaTitle">Meta Title</Label>
              <Input
                id="metaTitle"
                value={form.metaTitle}
                onChange={(e) => update("metaTitle", e.target.value)}
                placeholder="SEO title"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="ogImage">OG Image URL</Label>
              <Input
                id="ogImage"
                value={form.ogImageUrl ?? ""}
                onChange={(e) => update("ogImageUrl", e.target.value || null)}
                placeholder="https://..."
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="metaDesc">Meta Description</Label>
            <Textarea
              id="metaDesc"
              rows={2}
              value={form.metaDescription}
              onChange={(e) => update("metaDescription", e.target.value)}
              placeholder="SEO description..."
            />
          </div>

          <div className="flex justify-end pt-2">
            <Button onClick={() => void handleSave()} disabled={isSaving}>
              {isSaving ? "Saving..." : "Save"}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

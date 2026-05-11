import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Plus } from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { BlogPostSummaryDto, BlogStatus } from "@/api/types";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";

const statusBadgeVariant = (status: BlogStatus) => {
  if (status === "Published") return "success" as const;
  if (status === "Archived") return "accent" as const;
  return "secondary" as const;
};

type TabValue = "all" | BlogStatus;

export const BlogPostsPage = () => {
  const [posts, setPosts] = useState<BlogPostSummaryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<TabValue>("all");

  const loadPosts = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.listBlogPosts();
      setPosts(data);
    } catch {
      setError("Failed to load blog posts.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadPosts();
  }, []);

  const filtered = tab === "all" ? posts : posts.filter((p) => p.status === tab);

  const handlePublish = async (id: string) => {
    try {
      await adminApi.publishBlogPost(id);
      await loadPosts();
    } catch {
      setError("Failed to publish post.");
    }
  };

  const handleArchive = async (id: string) => {
    try {
      await adminApi.archiveBlogPost(id);
      await loadPosts();
    } catch {
      setError("Failed to archive post.");
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Blog Posts</h1>
          <p className="mt-1 text-muted-foreground">Manage blog content.</p>
        </div>
        <Link to="/app/admin/blog/new">
          <Button>
            <Plus className="mr-2 h-4 w-4" />
            New Post
          </Button>
        </Link>
      </div>

      <Tabs value={tab} onValueChange={(v) => setTab(v as TabValue)}>
        <TabsList>
          <TabsTrigger value="all">All</TabsTrigger>
          <TabsTrigger value="Draft">Draft</TabsTrigger>
          <TabsTrigger value="Published">Published</TabsTrigger>
          <TabsTrigger value="Archived">Archived</TabsTrigger>
        </TabsList>
      </Tabs>

      <Card>
        <CardContent className="pt-6">
          {isLoading ? (
            <Loader label="Loading posts..." />
          ) : error ? (
            <p className="py-8 text-center text-destructive">{error}</p>
          ) : filtered.length === 0 ? (
            <p className="py-8 text-center text-muted-foreground">No posts found.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Title</TableHead>
                  <TableHead>Slug</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="hidden md:table-cell">Tags</TableHead>
                  <TableHead className="hidden md:table-cell">Reading Time</TableHead>
                  <TableHead className="hidden lg:table-cell">Published At</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filtered.map((post) => (
                  <TableRow key={post.id}>
                    <TableCell className="font-medium">{post.title}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">{post.slug}</TableCell>
                    <TableCell>
                      <Badge variant={statusBadgeVariant(post.status)}>{post.status}</Badge>
                    </TableCell>
                    <TableCell className="hidden md:table-cell text-sm text-muted-foreground">
                      {post.tags.join(", ") || "—"}
                    </TableCell>
                    <TableCell className="hidden md:table-cell text-sm">
                      {post.readingTimeMinutes} min
                    </TableCell>
                    <TableCell className="hidden lg:table-cell text-sm text-muted-foreground">
                      {post.publishedAt
                        ? new Date(post.publishedAt).toLocaleDateString()
                        : "—"}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-2">
                        {post.status === "Draft" && (
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => void handlePublish(post.id)}
                          >
                            Publish
                          </Button>
                        )}
                        {post.status === "Published" && (
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => void handleArchive(post.id)}
                          >
                            Archive
                          </Button>
                        )}
                        <Link to={`/app/admin/blog/${post.id}/edit`}>
                          <Button variant="ghost" size="sm">Edit</Button>
                        </Link>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
};

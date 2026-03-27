import { Link } from "react-router-dom";
import { Plus, Pencil } from "lucide-react";
import { useMyListings } from "@/features/listings/hooks/useMyListings";
import { buttonVariants } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { formatMoney } from "@/utils/format";
import { cn } from "@/lib/utils";

const statusVariant: Record<string, "secondary" | "success" | "accent" | "outline"> = {
  Draft: "secondary",
  Published: "success",
  Activated: "accent",
  Closed: "outline",
};

export const MyListingsPage = () => {
  const { data, isLoading, isError } = useMyListings();

  if (isLoading) return <Loader label="Loading your listings..." />;
  if (isError) {
    return <p className="text-destructive">Failed to load listings.</p>;
  }

  const items = data ?? [];

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">My listings</h1>
          <p className="mt-1 text-muted-foreground">Manage your properties and publish to the marketplace.</p>
        </div>
        <Link to="/app/listings/new" className={cn(buttonVariants({ variant: "accent" }), "gap-2")}>
          <Plus className="h-4 w-4" />
          New listing
        </Link>
      </div>

      {items.length === 0 ? (
        <EmptyState
          title="No listings yet"
          description="Create your first listing to appear in search results."
        >
          <Link to="/app/listings/new" className={cn(buttonVariants({ variant: "accent" }), "gap-2")}>
            <Plus className="h-4 w-4" />
            Create listing
          </Link>
        </EmptyState>
      ) : (
        <div className="space-y-3">
          {items.map((l) => (
            <Card key={l.id}>
              <CardContent className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="font-semibold">{l.title}</h2>
                    <Badge variant={statusVariant[l.status] ?? "secondary"}>{l.status}</Badge>
                  </div>
                  <p className="text-sm text-muted-foreground mt-1">
                    {formatMoney(l.monthlyRentCents)}/mo · {l.bedrooms === 0 ? "Studio" : `${l.bedrooms} bed`} ·{" "}
                    {l.bathrooms} bath
                  </p>
                </div>
                <Link
                  to={`/app/listings/${l.id}/edit`}
                  className={cn(buttonVariants({ variant: "outline", size: "sm" }), "gap-2")}
                >
                  <Pencil className="h-4 w-4" />
                  Edit
                </Link>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
};

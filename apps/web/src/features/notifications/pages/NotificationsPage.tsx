import { useNavigate, Link } from "react-router-dom";
import {
  Bell,
  CheckCheck,
  Circle,
  ExternalLink,
  Inbox,
  Settings,
} from "lucide-react";
import {
  useAllNotifications,
  useMarkRead,
  useMarkAllRead,
  useUnreadCount,
} from "@/features/notifications/hooks/useNotifications";
import { getNotificationRoute } from "@/features/notifications/utils/getNotificationRoute";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { BackLink } from "@/components/shared/BackLink";
import { Loader } from "@/components/shared/Loader";
import { cn } from "@/lib/utils";
import type { InAppNotificationDto } from "@/api/types";

function formatTimestamp(dateStr: string): string {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMin = Math.floor(diffMs / 60000);

  if (diffMin < 1) return "Just now";
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHours = Math.floor(diffMin / 60);
  if (diffHours < 24) return `${diffHours}h ago`;
  const diffDays = Math.floor(diffHours / 24);
  if (diffDays < 7) return `${diffDays}d ago`;

  return date.toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: date.getFullYear() !== now.getFullYear() ? "numeric" : undefined,
  });
}

const categoryColor: Record<string, string> = {
  payment: "text-emerald-500",
  verification: "text-blue-500",
  application: "text-purple-500",
  insurance: "text-amber-500",
  billing: "text-indigo-500",
  deal: "text-emerald-500",
  truth_surface: "text-sky-500",
  damage_claim: "text-red-500",
  booking: "text-orange-500",
  identity: "text-blue-500",
  compliance: "text-red-500",
  arbitration: "text-slate-500",
  listing: "text-violet-500",
  welcome: "text-emerald-500",
};

function getCategoryColor(category: string): string {
  for (const [key, color] of Object.entries(categoryColor)) {
    if (category.startsWith(key)) return color;
  }
  return "text-muted-foreground";
}

export const NotificationsPage = () => {
  const navigate = useNavigate();
  const { data: notifications, isLoading } = useAllNotifications();
  const { data: unreadCount } = useUnreadCount();
  const markRead = useMarkRead();
  const markAllRead = useMarkAllRead();

  if (isLoading) {
    return <Loader fullPage label="Loading notifications..." />;
  }

  const items = notifications ?? [];

  const handleClick = (n: InAppNotificationDto) => {
    if (!n.isRead) {
      markRead.mutate(n.id);
    }
    const route = getNotificationRoute(n);
    if (route) {
      navigate(route);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <BackLink fallbackTo="/app" className="mb-4" />
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">
              Notifications
            </h1>
            <p className="mt-1 text-muted-foreground">
              All your notifications in one place.
            </p>
          </div>
          <div className="flex items-center gap-2">
            {(unreadCount ?? 0) > 0 && (
              <Button
                variant="outline"
                size="sm"
                className="gap-1.5"
                onClick={() => markAllRead.mutate()}
                disabled={markAllRead.isPending}
              >
                <CheckCheck className="h-4 w-4" />
                {markAllRead.isPending
                  ? "Marking..."
                  : `Mark all read (${unreadCount})`}
              </Button>
            )}
            <Link to="/app/notification-preferences">
              <Button variant="ghost" size="sm" className="gap-1.5">
                <Settings className="h-4 w-4" />
                Preferences
              </Button>
            </Link>
          </div>
        </div>
      </div>

      {items.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-16">
            <Inbox className="h-16 w-16 text-muted-foreground/30 mb-4" />
            <p className="text-lg font-medium text-muted-foreground">
              No notifications yet
            </p>
            <p className="text-sm text-muted-foreground/60 mt-1">
              You'll see updates about your applications, payments, and deals
              here.
            </p>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader className="pb-0">
            <div className="flex items-center justify-between">
              <CardTitle className="text-base flex items-center gap-2">
                <Bell className="h-4 w-4" />
                All Notifications
              </CardTitle>
              <Badge variant="secondary">{items.length}</Badge>
            </div>
          </CardHeader>
          <CardContent className="p-0 mt-3">
            <div className="divide-y">
              {items.map((n) => {
                const dotColor = getCategoryColor(n.category);
                const route = getNotificationRoute(n);

                return (
                  <button
                    key={n.id}
                    onClick={() => handleClick(n)}
                    className={cn(
                      "w-full flex items-start gap-4 px-6 py-4 text-left transition-colors hover:bg-secondary/50 cursor-pointer",
                      !n.isRead && "bg-accent/5",
                    )}
                  >
                    <div className="mt-2 shrink-0">
                      {!n.isRead ? (
                        <Circle
                          className={cn(
                            "h-2.5 w-2.5 fill-current",
                            dotColor,
                          )}
                        />
                      ) : (
                        <div className="h-2.5 w-2.5" />
                      )}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <p
                          className={cn(
                            "text-sm leading-tight",
                            !n.isRead ? "font-semibold" : "font-medium",
                          )}
                        >
                          {n.title}
                        </p>
                        {!n.isRead && (
                          <Badge
                            variant="secondary"
                            className="text-[10px] px-1.5 py-0"
                          >
                            New
                          </Badge>
                        )}
                      </div>
                      <p className="text-sm text-muted-foreground mt-1">
                        {n.body}
                      </p>
                      <div className="flex items-center gap-3 mt-1.5">
                        <span className="text-xs text-muted-foreground/60">
                          {formatTimestamp(n.createdAt)}
                        </span>
                        {route && (
                          <span className="flex items-center gap-1 text-xs text-primary/60">
                            <ExternalLink className="h-3 w-3" />
                            View details
                          </span>
                        )}
                      </div>
                    </div>
                  </button>
                );
              })}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

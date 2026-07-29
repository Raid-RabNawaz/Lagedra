import { useNavigate, Link } from "react-router-dom";
import {
  CheckCheck,
  Settings,
  Inbox,
  Circle,
  ExternalLink,
} from "lucide-react";
import {
  useUnreadNotifications,
  useMarkRead,
  useMarkAllRead,
} from "@/features/notifications/hooks/useNotifications";
import { getNotificationRoute } from "@/features/notifications/utils/getNotificationRoute";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { InAppNotificationDto } from "@/api/types";

function timeAgo(dateStr: string): string {
  const seconds = Math.floor(
    (Date.now() - new Date(dateStr).getTime()) / 1000,
  );
  if (seconds < 60) return "just now";
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
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

type Props = {
  onClose: () => void;
};

export const NotificationPanel = ({ onClose }: Props) => {
  const navigate = useNavigate();
  const { data: notifications, isLoading } = useUnreadNotifications();
  const markRead = useMarkRead();
  const markAllRead = useMarkAllRead();

  const items = notifications ?? [];

  const handleClick = (n: InAppNotificationDto) => {
    markRead.mutate(n.id);
    const route = getNotificationRoute(n);
    if (route) {
      onClose();
      navigate(route);
    }
  };

  return (
    <div className="absolute right-0 top-full mt-2 w-96 max-h-[32rem] rounded-xl border bg-background shadow-lg animate-fade-in flex flex-col z-50">
      <div className="flex items-center justify-between px-4 py-3 border-b">
        <h3 className="font-semibold text-sm">Notifications</h3>
        <div className="flex items-center gap-1">
          {items.length > 0 && (
            <Button
              variant="ghost"
              size="sm"
              className="h-7 gap-1 text-xs"
              onClick={() => markAllRead.mutate()}
              disabled={markAllRead.isPending}
            >
              <CheckCheck className="h-3.5 w-3.5" />
              Mark all read
            </Button>
          )}
          <Link
            to="/app/notification-preferences"
            onClick={onClose}
            className="flex items-center justify-center h-7 w-7 rounded-md hover:bg-secondary transition-colors"
            title="Notification preferences"
          >
            <Settings className="h-3.5 w-3.5 text-muted-foreground" />
          </Link>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto">
        {isLoading && (
          <div className="p-8 text-center text-sm text-muted-foreground">
            Loading...
          </div>
        )}

        {!isLoading && items.length === 0 && (
          <div className="flex flex-col items-center justify-center p-8 text-center">
            <Inbox className="h-10 w-10 text-muted-foreground/40 mb-2" />
            <p className="text-sm text-muted-foreground">
              You're all caught up
            </p>
          </div>
        )}

        {items.length > 0 && (
          <Link
            to="/app/notifications"
            onClick={onClose}
            className="block px-4 py-2.5 text-center text-xs font-medium text-primary hover:bg-secondary/50 transition-colors border-b"
          >
            See all notifications
          </Link>
        )}

        {items.map((n) => {
          const dotColor = getCategoryColor(n.category);
          const route = getNotificationRoute(n);

          return (
            <button
              key={n.id}
              onClick={() => handleClick(n)}
              className={cn(
                "w-full flex items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-secondary/50 cursor-pointer border-b last:border-b-0",
                !n.isRead && "bg-accent/5",
              )}
            >
              <div className="mt-1.5 shrink-0">
                {!n.isRead ? (
                  <Circle
                    className={cn("h-2 w-2 fill-current", dotColor)}
                  />
                ) : (
                  <div className="h-2 w-2" />
                )}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium leading-tight truncate">
                  {n.title}
                </p>
                <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">
                  {n.body}
                </p>
                <div className="flex items-center justify-between mt-1">
                  <p className="text-[10px] text-muted-foreground/60">
                    {timeAgo(n.createdAt)}
                  </p>
                  {route && (
                    <ExternalLink className="h-3 w-3 text-muted-foreground/40" />
                  )}
                </div>
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
};

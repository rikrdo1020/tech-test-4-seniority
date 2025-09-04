import { useNavigate, useSearchParams } from "react-router-dom";
import { formatDistanceToNow, parseISO } from "date-fns";
import {
  useMarkAllAsRead,
  useMarkAsRead,
  useNotifications,
} from "../../app/hooks/useNotification";
import type { NotificationDto } from "../../app/types/Notification";

const NotificationsPage = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const page = parseInt(searchParams.get("page") || "1", 10);
  const pageSize = 10;

  const { data, isFetching } = useNotifications({ page, pageSize }, true);
  const markAsReadMutation = useMarkAsRead();
  const markAllAsReadMutation = useMarkAllAsRead();

  const handleMarkAsRead = (id: string) => {
    markAsReadMutation.mutate(id);
  };

  const handleMarkAllAsRead = () => {
    markAllAsReadMutation.mutate();
  };

  const handlePageChange = (newPage: number) => {
    setSearchParams({ page: String(newPage) });
  };

  const handleNavigate = (id: string) => {
    navigate(`/tasks/${id}`);
  };

  return (
    <div className="px-6 py-4">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-2xl font-bold">Notifications</h2>
        <button
          className="btn btn-sm btn-link"
          onClick={handleMarkAllAsRead}
          disabled={markAllAsReadMutation.isPending}
        >
          {markAllAsReadMutation.isPending ? "Marking..." : "Mark all as read"}
        </button>
      </div>

      {isFetching && (
        <div className=" h-full flex flex-col items-center justify-center gap-4">
          <p className="text-2xl font-bold text-primary">Loading...</p>
          <progress className="progress progress-primary w-56"></progress>
        </div>
      )}

      {!isFetching && data?.items.length === 0 ? (
        <div className="text-center py-10 text-gray-500">
          You have no notifications.
        </div>
      ) : (
        <div className="space-y-3">
          {data?.items.map((n) => (
            <NotificationItem
              key={n.id}
              notification={n}
              onMarkAsRead={handleMarkAsRead}
              isLoading={markAsReadMutation.isPending}
              handleNavigate={handleNavigate}
            />
          ))}
        </div>
      )}

      {data && data.total > pageSize && (
        <div className="flex justify-center mt-6">
          <div className="join">
            <button
              className="join-item btn btn-sm"
              disabled={page <= 1}
              onClick={() => handlePageChange(page - 1)}
            >
              «
            </button>
            <button className="join-item btn btn-sm btn-disabled">
              Page {page}
            </button>
            <button
              className="join-item btn btn-sm"
              disabled={page * pageSize >= data.total}
              onClick={() => handlePageChange(page + 1)}
            >
              »
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

const NotificationItem = ({
  notification,
  onMarkAsRead,
  isLoading,
  handleNavigate,
}: {
  notification: NotificationDto;
  onMarkAsRead: (id: string) => void;
  handleNavigate: (id: string) => void;
  isLoading: boolean;
}) => {
  const isUnread = !notification.isRead;

  return (
    <div
      className={`p-4 rounded-lg border ${
        isUnread ? "bg-blue-50 border-blue-200" : "bg-base-100 border-base-200"
      }`}
      onClick={() => {
        if (notification.relatedTaskId)
          handleNavigate(notification.relatedTaskId);
        onMarkAsRead(notification.id);
      }}
    >
      <div className="flex justify-between">
        <div>
          <h3 className={`font-semibold ${isUnread ? "text-blue-700" : ""}`}>
            {notification.title}
          </h3>
          <p className="text-sm text-gray-600 mt-1">{notification.message}</p>
          <p className="text-xs text-gray-400 mt-2">
            {formatDistanceToNow(parseISO(notification.createdAt), {
              addSuffix: true,
            })}
          </p>
        </div>
        {!notification.isRead && !notification.relatedTaskId && (
          <button
            className="btn btn-xs btn-ghost text-blue-500"
            onClick={() => onMarkAsRead(notification.id)}
            disabled={isLoading}
          >
            Mark as read
          </button>
        )}
      </div>
    </div>
  );
};

export default NotificationsPage;

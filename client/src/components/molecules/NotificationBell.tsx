import React from "react";
import BellIcon from "../../assets/icons/bell.svg?react";
import { useUnreadNotificationsCount } from "../../app/hooks/useNotification";

const NotificationBellInner: React.FC<{ onClick?: () => void }> = ({
  onClick,
}) => {
  const { data: count = 0, isFetching } = useUnreadNotificationsCount();

  return (
    <button className="relative" onClick={onClick} aria-label="Notificaciones">
      <BellIcon className="w-6 h-6" />
      {count > 0 && (
        <span
          className="absolute -top-1 -right-1 indicator-item badge badge-xs badge-secondary"
          aria-hidden
        >
          {count > 99 ? "99+" : count}
        </span>
      )}
      {isFetching && (
        <span className="absolute -top-2 -right-2 text-xs text-gray-400">
          ...
        </span>
      )}
    </button>
  );
};

const NotificationBell = React.memo(NotificationBellInner);
NotificationBell.displayName = "NotificationBell";

export default NotificationBell;

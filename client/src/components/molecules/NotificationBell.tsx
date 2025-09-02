import BellIcon from "../../assets/icons/bell.svg?react";

const NotificationBell = () => {
  return (
    <div className="indicator">
      <BellIcon />
      <span className="badge badge-xs badge-primary indicator-item"></span>
    </div>
  );
};

export default NotificationBell;

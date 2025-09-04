import { useNavigate } from "react-router-dom";
import HomeIcon from "../../assets/icons/home.svg?react";
import AddIcon from "../../assets/icons/add.svg?react";
import TaskIcon from "../../assets/icons/task.svg?react";
import NotificationBell from "../molecules/NotificationBell";
import Avatar from "../atoms/Avatar";

const Footer = () => {
  const navigate = useNavigate();
  return (
    <div className="px-4 py-4 lg:hidden">
      <div className="w-full flex justify-between">
        <div className="navbar-center">
          <button className="text-xl" onClick={() => navigate("/")}>
            <HomeIcon />
          </button>
        </div>
        <div className="navbar-center">
          <button className="text-xl" onClick={() => navigate("/tasks")}>
            <TaskIcon />
          </button>
        </div>
        <div className="navbar-center">
          <button className="text-xl" onClick={() => navigate("/tasks/new")}>
            <AddIcon />
          </button>
        </div>
        <div className="navbar-center">
          <button
            className="text-xl"
            onClick={() => navigate("/notifications")}
          >
            <NotificationBell />
          </button>
        </div>
        <div className="navbar-center" onClick={() => navigate("/me/edit")}>
          <Avatar size="xs" rounded="full" />
        </div>
      </div>
    </div>
  );
};

export default Footer;

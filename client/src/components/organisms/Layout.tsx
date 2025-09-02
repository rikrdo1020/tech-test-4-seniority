import { Outlet, useMatch, useNavigate } from "react-router-dom";
import TopBar from "./TopBar/TopBar";
import Avatar from "../atoms/Avatar";
import NotificationBell from "../molecules/NotificationBell";
import HomeIcon from "../../assets/icons/home.svg?react";
import AddIcon from "../../assets/icons/add.svg?react";
import TaskIcon from "../../assets/icons/task.svg?react";

const Layout = () => {
  const navigate = useNavigate();

  const matchNew = useMatch({ path: "/tasks/new", end: true });
  const matchEdit = useMatch({ path: "/tasks/:id/edit", end: true });

  const hideTopBar = Boolean(matchNew || matchEdit);

  return (
    <div className="flex flex-col h-screen">
      {!hideTopBar && <TopBar />}

      <div className="flex-1 overflow-y-auto">
        <Outlet />
      </div>
      <div className="lg:hidden px-4 py-4">
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
            <button className="text-xl">
              <NotificationBell />
            </button>
          </div>
          <div className="navbar-center" onClick={() => navigate("/me/edit")}>
            <Avatar size="xs" rounded="full" />
          </div>
        </div>
      </div>
    </div>
  );
};

export default Layout;

import { useState } from "react";
import HamburgerIcon from "../../assets/icons/hamburger.svg?react";
import HomeIcon from "../../assets/icons/home.svg?react";
import AddIcon from "../../assets/icons/add_2.svg?react";
import TaskIcon from "../../assets/icons/task.svg?react";
import NotificationBell from "../molecules/NotificationBell";
import Avatar from "../atoms/Avatar";
import { useNavigate } from "react-router-dom";

const HamburgerMenu = () => {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);

  const closeDrawer = () => setOpen(false);
  const openDrawer = () => setOpen(true);

  const handleNavigation = (path: string) => {
    navigate(path);
    closeDrawer();
  };

  return (
    <>
      <input
        id="my-drawer-3"
        type="checkbox"
        className="drawer-toggle"
        checked={open}
        onChange={(e) => setOpen(e.target.checked)}
      />

      <div className="flex-none">
        <button
          type="button"
          aria-label="open sidebar"
          className="btn btn-square btn-ghost"
          onClick={openDrawer}
        >
          <HamburgerIcon />
        </button>
      </div>

      <div className="drawer-side z-40">
        <label
          htmlFor="my-drawer-3"
          aria-label="close sidebar"
          className="drawer-overlay"
          onClick={closeDrawer}
        />

        <ul className="menu bg-base-200 min-h-full w-80 p-4">
          <li>
            <button
              aria-label="Home"
              className="flex gap-2"
              onClick={() => handleNavigation("/")}
            >
              <HomeIcon />
              Home
            </button>
          </li>

          <li>
            <button
              className="flex gap-2"
              onClick={() => handleNavigation("/tasks")}
            >
              <TaskIcon />
              Tasks
            </button>
          </li>

          <li>
            <button
              className="flex gap-2"
              onClick={() => handleNavigation("/tasks/new")}
            >
              <AddIcon />
              Add new Task
            </button>
          </li>

          <li>
            <button
              className="flex gap-2"
              onClick={() => handleNavigation("/notifications")}
            >
              <NotificationBell />
              Notifications
            </button>
          </li>

          <li className="mt-auto">
            <button
              className="flex gap-2"
              onClick={() => handleNavigation("/me/edit")}
            >
              <Avatar size="xs" rounded="full" />
              Profile
            </button>
          </li>
        </ul>
      </div>
    </>
  );
};

export default HamburgerMenu;

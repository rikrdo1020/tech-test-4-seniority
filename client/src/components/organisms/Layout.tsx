import { Outlet, useMatch } from "react-router-dom";
import TopBar from "./TopBar/TopBar";
import Footer from "./Footer";

const Layout = () => {
  const matchNew = useMatch({ path: "/tasks/new", end: true });
  const matchEdit = useMatch({ path: "/tasks/:id/edit", end: true });

  const hideTopBar = Boolean(matchNew || matchEdit);

  return (
    <div className="flex flex-col h-screen items-center">
      <div className="flex flex-col h-full w-full max-w-[66rem]">
        {!hideTopBar && (
          <div className="lg:hidden">
            <TopBar />
          </div>
        )}
        <div className="hidden lg:block">
          <TopBar />
        </div>

        <div className="flex-1 overflow-y-auto">
          <Outlet />
        </div>
        <Footer />
      </div>
    </div>
  );
};

export default Layout;

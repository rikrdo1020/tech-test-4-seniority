import { useState, useEffect } from "react";
import { useNavigate, useLocation, useSearchParams } from "react-router-dom";
import HamburgerMenu from "../../molecules/HamburgerMenu";
import SearchIcon from "../../../assets/icons/search.svg?react";
import SearchInput from "../../atoms/SearchInput";

const TopBar = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();

  const pathName = location.pathname;

  const [showSearch, setShowSearch] = useState(false);
  const [query, setQuery] = useState("");
  const openSearch = searchParams.get("o") ?? "";

  useEffect(() => {
    if (Boolean(openSearch)) setShowSearch(true);
  }, [openSearch]);

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const q = params.get("search") || "";
    setQuery(q);
  }, [location.search]);

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const current = params.get("search") || "";

    if (query === current) return;

    if (query.trim()) params.set("search", query.trim());
    else params.delete("search");

    navigate(
      {
        pathname: location.pathname,
        search: params.toString(),
      },
      { replace: true }
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query, location.pathname, navigate]);

  const handleSearch = (value: string) => {
    const params = new URLSearchParams(location.search);
    if (value.trim()) params.set("search", value.trim());
    else params.delete("search");

    navigate(
      {
        pathname: location.pathname,
        search: params.toString(),
      },
      { replace: true }
    );

    setShowSearch(false);
  };

  return (
    <>
      {!showSearch ? (
        <div className="navbar p-6 flex justify-between items-center">
          <div className="hidden lg:block navbar-start">
            <HamburgerMenu />
          </div>
          <div className="navbar-center">
            <a className="btn btn-ghost text-xl" onClick={() => navigate("/")}>
              MyTasks
            </a>
          </div>
          <div className="navbar-end flex items-center gap-2">
            <button
              className="bg-transparent text-xl px-2"
              aria-label="Open search"
              onClick={() => {
                if (pathName === "/tasks") setShowSearch(true);
                else navigate("/tasks?o=true");
              }}
            >
              <SearchIcon className="w-6 h-6" />
            </button>
          </div>
        </div>
      ) : (
        <div className="flex items-center p-6 gap-2 w-full">
          <SearchInput
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onSearch={handleSearch}
            containerClassName="w-full max-w-xs"
            autoFocus
          />
          <button
            onClick={() => setShowSearch(false)}
            className="bg-transparent h-full px-2"
            aria-label="Cancel search"
          >
            Close
          </button>
        </div>
      )}
    </>
  );
};

export default TopBar;

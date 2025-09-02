import { useState } from "react";
import { useTasks } from "../../app/hooks/useTasks";
import TasksList from "../molecules/TasksList";
import FiltersPanel from "../organisms/FiltersPanel";
import FilterIcon from "../../assets/icons/filter.svg?react";
import { TASK_STATUS_OPTIONS, TaskStatus } from "../../app/types/Task";
import { useSearchParams } from "react-router-dom";
import { useDebounce } from "../../app/hooks/useDebounce";

const AllTasksPage = () => {
  const [searchParams] = useSearchParams();
  const search = searchParams.get("search") ?? "";

  const [filters, setFilters] = useState<{
    status?: TaskStatus;
    scope?: string;
  }>({
    status: undefined,
    scope: undefined,
  });
  const [showFilters, setShowFilters] = useState(false);

  const debouncedSearch = useDebounce(search, 400);

  const params = {
    search: debouncedSearch,
    status: filters.status,
    scope: filters.scope,
    page: 1,
    pageSize: 20,
  };

  const tasksQuery = useTasks(params);

  const handleApply = (newFilters: { status?: TaskStatus; scope?: string }) => {
    setFilters(newFilters);
  };

  return (
    <div className="px-6">
      <div className="flex justify-between">
        <div className="flex gap-2">
          {filters.status && (
            <div
              className="badge badge-outline badge-primary capitalize"
              onClick={() =>
                setFilters((prev) => ({ ...prev, status: undefined }))
              }
            >
              {TASK_STATUS_OPTIONS[Number(filters?.status)]?.label}
            </div>
          )}
          {filters.scope && (
            <div
              className="badge badge-outline badge-primary capitalize"
              onClick={() =>
                setFilters((prev) => ({ ...prev, scope: undefined }))
              }
            >
              {TASK_STATUS_OPTIONS[Number(filters?.scope)]?.label}
            </div>
          )}
        </div>
        <button
          onClick={() => setShowFilters(true)}
          className="btn btn-ghost btn-xs mb-4"
        >
          <FilterIcon />
        </button>
      </div>

      {showFilters && (
        <FiltersPanel
          initialFilters={filters}
          onApply={handleApply}
          onClose={() => setShowFilters(false)}
        />
      )}
      <TasksList
        tasks={tasksQuery.data?.items || []}
        isLoading={tasksQuery.isFetching}
      />
    </div>
  );
};

export default AllTasksPage;

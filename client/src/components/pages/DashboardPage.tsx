import { getGreeting } from "../../app/helpers/getGeeting";
import { useAuth } from "../../app/hooks/useAuth";
import { useDashboard } from "../../app/hooks/useDashboard";
import { useTasks } from "../../app/hooks/useTasks";
import TasksList from "../molecules/TasksList";
import TasksSummary from "../molecules/TasksSummary";
import PreviewGrid from "../organisms/PreviewGrid/PreviewGrid";

const DashboardPage = () => {
  /*todo
  - Presentation text calculate moment of the day and get user name and display it
  - Implement task list display
   - Add task creation functionality
   - Include task filtering options
  */
  const { user } = useAuth();
  const params = {
    search: undefined,
    status: undefined,
    scope: "all",
    page: 1,
    pageSize: 20,
  };

  const tasksQuery = useTasks(params);
  const dashboardQuery = useDashboard();

  if (dashboardQuery.isLoading || tasksQuery.isLoading)
    return (
      <div className=" h-full flex flex-col items-center justify-center gap-4">
        <p className="text-2xl font-bold text-primary">Loading...</p>
        <progress className="progress progress-primary w-56"></progress>
      </div>
    );
  return (
    <div className="flex flex-col">
      <div className="flex-1 flex flex-col gap-8 overflow-y-auto">
        <div className="flex flex-col px-6">
          <p className="text-base-content opacity-60">
            {getGreeting()}, {user!.name?.split(" ")[0]}!
          </p>{" "}
          <TasksSummary
            data={dashboardQuery.data}
            isLoading={dashboardQuery.isLoading}
            isError={dashboardQuery.isError}
          />
        </div>

        <div className="flex flex-col px-6 gap-4">
          <p className="text-xl font-semibold">Preview</p>
          <PreviewGrid {...dashboardQuery.data?.counts} />
        </div>

        <div className="flex flex-col px-6 gap-4 pb-6">
          <p className="text-xl font-semibold">Task List</p>
          <TasksList
            tasks={tasksQuery.data?.items || []}
            isLoading={tasksQuery.isLoading}
          />
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;

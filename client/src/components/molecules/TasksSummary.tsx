import type { DashboardData } from "../../app/types/Task";

const normalizePeriod = (raw: string | undefined) => {
  if (!raw) return "upcoming";
  if (raw.startsWith("for ")) return raw.slice(4);
  return raw;
};

const getCountForPeriod = (data: DashboardData) => {
  switch (data.relevantPeriod) {
    case "for today":
      return data.tasksDueToday?.length ?? 0;
    case "for this week":
      return data.tasksDueThisWeek?.length ?? 0;
    case "for this month":
      return data.tasksDueThisMonth?.length ?? 0;
    case "upcoming":
    default:
      return data.upcomingTasks?.length ?? data.counts?.total ?? 0;
  }
};

interface TasksSummaryProps {
  data?: DashboardData;
  isLoading: boolean;
  isError: boolean;
}

const TasksSummary = ({ data, isLoading, isError }: TasksSummaryProps) => {
  if (isLoading) return <div>Loading tasks summary...</div>;
  if (isError || !data) return <div>Error loading tasks summary.</div>;

  const periodLabel = normalizePeriod(data.relevantPeriod);
  const count = getCountForPeriod(data);
  const taskWord = count === 1 ? "task" : "tasks";

  return (
    <h2 className="text-2xl font-bold">
      {count === 0 ? (
        <>
          No {taskWord}{" "}
          {periodLabel === "upcoming" ? "scheduled" : `for ${periodLabel}`}
        </>
      ) : (
        <>
          You have{" "}
          <span className="text-primary">
            {count} {taskWord}
          </span>{" "}
          {periodLabel === "upcoming" ? "" : `for ${periodLabel}`}
        </>
      )}
    </h2>
  );
};

export default TasksSummary;

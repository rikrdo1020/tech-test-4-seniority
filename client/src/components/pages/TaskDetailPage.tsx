import { useMemo, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { format } from "date-fns";
import {
  useDeleteTask,
  useTask,
  useUpdateTaskStatus,
} from "../../app/hooks/useTasks";
import type { User } from "../../app/types/User";
import type { TaskStatus } from "../../app/types/Task";
import TaskOptionsModal from "../molecules/TaskOptionsModal";
import VerticalDotsIcon from "../../assets/icons/verticalDots.svg?react";

const TASK_STATUS_OPTIONS: Record<number, { label: string; color?: string }> = {
  0: { label: "Pending", color: "bg-warning" },
  1: { label: "In Progress", color: "bg-info" },
  2: { label: "Done", color: "bg-success" },
  3: { label: "Cancelled", color: "bg-error" },
};

const TaskDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [showOptions, setShowOptions] = useState(false);

  if (!id) {
    return (
      <div className="alert alert-error">
        <div>
          <span>Error loading task</span>
        </div>
      </div>
    );
  }

  const updateStatusMutation = useUpdateTaskStatus(id);
  const deleteTaskMutation = useDeleteTask(id);
  const handleChangeStatus = (newStatus: TaskStatus) => {
    updateStatusMutation.mutate(newStatus.toString());
  };

  const handleDeleteTask = () => {
    deleteTaskMutation.mutate();
  };
  const taskQuery = useTask(id);

  const task = taskQuery.data;

  const status = useMemo(() => {
    if (!task) return { label: "—", color: "badge-outline" };
    return (
      TASK_STATUS_OPTIONS[task.status] ?? {
        label: String(task.status),
        color: "badge-outline",
      }
    );
  }, [task]);

  return (
    <div className="pb-6 h-full max-w-3xl mx-auto flex flex-col">
      <div className="divider px-4 mt-0 opacity-35" />
      {taskQuery.isLoading && (
        <div className="flex justify-center py-12">
          <span className="loading loading-spinner loading-lg"></span>
        </div>
      )}

      {taskQuery.isError && (
        <div className="alert alert-error">
          <div>
            <span>Error loading task</span>
            <span className="block text-sm mt-1 text-gray-700">
              {(taskQuery.error as Error)?.message}
            </span>
          </div>
        </div>
      )}

      {task && (
        <div className="card bg-base-100 shadow-md flex-grow">
          <div className="card-body pt-0">
            <div className="flex items-start justify-between gap-4">
              <div>
                <div className="flex flex-col gap-2 items-left">
                  <h2 className="card-title text-xl">{task.title}</h2>

                  <span className={`badge ${status.color}`}>
                    {status.label}
                  </span>
                  {task.isOverdue && (
                    <span className="badge badge-error">Overdue</span>
                  )}
                  {task.dueDate ? (
                    <div className="text-sm text-gray-500">
                      Expires:{" "}
                      <span className="font-medium">
                        {format(task.dueDate, "dd MMM yyyy")}
                      </span>
                    </div>
                  ) : (
                    <div className="text-sm text-gray-400">
                      No expiration date
                    </div>
                  )}
                  <h2 className="text-md text-neutral-content opacity-35">
                    Description:
                  </h2>
                  <p className="text-xl text-neutral-content">
                    {task.description}
                  </p>
                </div>
              </div>

              <div className="text-right">
                <button className="shadow-none px-2">
                  <VerticalDotsIcon onClick={() => setShowOptions(true)} />
                </button>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-auto">
              <InfoCard title="Assigned to" user={task.assignedTo} />
              <InfoCard title="Created by" user={task.createdBy} />
            </div>

            {showOptions && (
              <TaskOptionsModal
                currentStatus={task.status}
                onUpdateStatus={handleChangeStatus}
                onUpdate={() => navigate(`edit`)}
                onDelete={handleDeleteTask}
                onClose={() => setShowOptions(false)}
              />
            )}
          </div>
        </div>
      )}
    </div>
  );
};

const InfoCard = ({ title, user }: { title: string; user?: User | null }) => {
  if (!user) {
    return (
      <div className="p-4 rounded-lg border bg-base-100">
        <div className="text-sm text-gray-500">{title}</div>
        <div className="mt-2 text-sm text-gray-400">Sin información</div>
      </div>
    );
  }

  return (
    <div className="p-4 rounded-lg border bg-base-100">
      <div className="text-sm text-gray-500">{title}</div>
      <div className="mt-2">
        <div className="font-medium">{user.name}</div>
        <div className="text-xs text-gray-500">{user.email}</div>
      </div>
    </div>
  );
};

export default TaskDetailPage;

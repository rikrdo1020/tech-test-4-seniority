import type { User } from "../../app/types/User";
import Avatar from "../atoms/Avatar";
import VerticalDotsIcon from "../../assets/icons/verticalDots.svg?react";
import { useMemo, useState } from "react";
import TaskOptionsModal from "./TaskOptionsModal";
import { useDeleteTask, useUpdateTaskStatus } from "../../app/hooks/useTasks";
import { TaskStatus } from "../../app/types/Task";
import { useNavigate } from "react-router-dom";

interface TaskCardProps {
  id: string;
  title: string;
  status: TaskStatus;
  dueDate: Date;
  createdBy: User;
  assignedTo: User | null;
}

const TASK_STATUS_OPTIONS_BADGE: Record<
  number,
  { label: string; color?: string }
> = {
  0: { label: "Pending", color: "bg-warning" },
  1: { label: "In Progress", color: "bg-info" },
  2: { label: "Done", color: "bg-success" },
  3: { label: "Cancelled", color: "bg-error" },
};

const TaskCard = ({
  id,
  title,
  createdBy,
  status,
  assignedTo,
}: TaskCardProps) => {
  const navigate = useNavigate();
  const [showOptions, setShowOptions] = useState(false);

  const updateStatusMutation = useUpdateTaskStatus(id);
  const deleteTaskMutation = useDeleteTask(id);
  const handleChangeStatus = (newStatus: TaskStatus) => {
    updateStatusMutation.mutate(newStatus.toString());
  };

  const handleDeleteTask = () => {
    deleteTaskMutation.mutate();
  };

  const statusBadge = useMemo(() => {
    return (
      TASK_STATUS_OPTIONS_BADGE[status] ?? {
        label: String(status),
        color: "badge-outline",
      }
    );
  }, []);

  return (
    <>
      <div
        className="card bg-base-300 text-neutral-content w-full cursor-pointer"
        onClick={() => navigate(`/tasks/${id}`)}
      >
        <div className="card-body items-left text-left text-base-content p-4 flex flex-row">
          <div className="flex-grow flex flex-col gap-2">
            <h2 className="card-title">{title}</h2>
            <div className="flex gap-2 items-start">
              <Avatar size="xs" rounded="full" />
              <div className="flex flex-col">
                <p className="opacity-50 text-xs">
                  Created by {createdBy.name}
                </p>
                <p className="opacity-50 text-xs">Due in 3 days</p>
                {assignedTo && (
                  <p className="opacity-50 text-xs ">
                    Assigned to by{" "}
                    <span className="text-warning">{assignedTo.name}</span>
                  </p>
                )}
                <span className={`badge ${statusBadge.color} mt-2`}>
                  {statusBadge.label}
                </span>
              </div>
            </div>
          </div>
          <div className="card-actions justify-end mt-4">
            <button
              type="button"
              aria-label="Open options"
              className="btn bg-transparent shadow-none px-2"
              onClick={(e) => {
                e.stopPropagation();
                setShowOptions(true);
              }}
            >
              <VerticalDotsIcon />
            </button>
          </div>
        </div>
      </div>
      {showOptions && (
        <TaskOptionsModal
          currentStatus={status}
          onUpdateStatus={handleChangeStatus}
          onUpdate={() => navigate(`tasks/${id}/edit`)}
          onDelete={handleDeleteTask}
          onClose={() => setShowOptions(false)}
        />
      )}
    </>
  );
};

export default TaskCard;

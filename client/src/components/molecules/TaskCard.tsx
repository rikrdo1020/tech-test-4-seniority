import type { User } from "../../app/types/User";
import Avatar from "../atoms/Avatar";
import VerticalDotsIcon from "../../assets/icons/verticalDots.svg?react";
import { useState } from "react";
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
}
const TaskCard = ({ id, title, createdBy, status }: TaskCardProps) => {
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

  return (
    <>
      <div className="card bg-base-300 text-neutral-content w-full">
        <div className="card-body items-left text-left text-base-content p-4 flex flex-row">
          <div className="flex-grow flex flex-col gap-2">
            <h2 className="card-title">{title}</h2>
            <div className="flex gap-2 items-end">
              <Avatar size="xs" rounded="full" />
              <div>
                <p className="opacity-50 text-xs">
                  Created by {createdBy.name}
                </p>
                <p className="opacity-50 text-xs">Due in 3 days</p>
              </div>
            </div>
          </div>
          <div className="card-actions justify-end mt-4">
            <button className="btn bg-transparent shadow-none px-2">
              <VerticalDotsIcon onClick={() => setShowOptions(true)} />
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

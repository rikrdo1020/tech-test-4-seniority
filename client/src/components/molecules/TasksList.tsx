import React from "react";
import type { Task } from "../../app/types/Task";
import TaskCard from "./TaskCard";

interface TasksListProps {
  tasks?: Task[] | null;
  isLoading: boolean;
}

const TasksList: React.FC<TasksListProps> = ({ tasks, isLoading }) => {
  if (isLoading) {
    return <div className="text-center py-6">Loading tasks...</div>;
  }

  if (!tasks || tasks.length === 0) {
    return (
      <div className="text-center py-10 text-gray-500">
        <p className="text-lg">No tasks to display.</p>
        <p className="text-sm mt-2">
          Try adjusting your filters or add a new task.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {tasks.map((item) => (
        <TaskCard
          key={item.id}
          title={item.title}
          createdBy={item.createdBy}
          dueDate={item.dueDate}
          id={item.id}
          status={item.status}
        />
      ))}
    </div>
  );
};

export default TasksList;

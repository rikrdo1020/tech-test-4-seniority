import type { User } from "./User";

export interface Task {
  id: string;
  title: string;
  description: string;
  dueDate: Date;
  status: TaskStatus;
  createdBy: User;
  assignedTo: User | null;
}

export const TaskStatus = {
  Pending: 0,
  InProgress: 1,
  Done: 2,
} as const;

export type TaskStatus = (typeof TaskStatus)[keyof typeof TaskStatus];
export const TASK_STATUS_OPTIONS = [
  { value: TaskStatus.Pending, label: "Pending" },
  { value: TaskStatus.InProgress, label: "In Progress" },
  { value: TaskStatus.Done, label: "Done" },
] as const;
export type TaskStatusOption = (typeof TASK_STATUS_OPTIONS)[number];
export const SCOPE_OPTIONS = ["assigned", "created", "all"];

export interface Counts {
  pending: number;
  inProgress: number;
  done: number;
  total: number;
}

export interface DashboardData {
  greeting: string;
  relevantPeriod: string;
  tasksDueToday: Task[];
  tasksDueThisWeek: Task[];
  tasksDueThisMonth: Task[];
  upcomingTasks: Task[];
  counts: Counts;
}

export interface PaginatedTasks {
  items: Task[];
  page: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
}

export interface CreateTaskDto {
  title: string;
  description?: string | null;
  dueDate?: string | null;
  assignedToExternalId?: string | null;
}

export interface UpdateTaskDto {
  title: string;
  description?: string | null;
  dueDate?: string | null;
  itemStatus: TaskStatus;
  assignedToExternalId?: string | null;
}

import {
  keepPreviousData,
  useMutation,
  useQuery,
  type UseMutationOptions,
  type UseQueryOptions,
  type UseQueryResult,
} from "@tanstack/react-query";
import { apiFetch } from "../services/apiClient";
import type {
  CreateTaskDto,
  PaginatedTasks,
  Task,
  TaskStatus,
  UpdateTaskDto,
} from "../types/Task";
import { queryClient } from "../../main";

export type UseTasksParams = {
  search?: string;
  status?: TaskStatus;
  dueDateFrom?: string;
  dueDateTo?: string;
  scope?: "assigned" | "created" | string;
  page?: number;
  pageSize?: number;
};

export const fetchTask = async (id: string, signal?: AbortSignal) =>
  apiFetch(`/tasks/${id}`, { method: "GET" }, signal);

export const useTask = (id?: string): UseQueryResult<Task, Error> => {
  return useQuery<Task, Error>({
    queryKey: ["task", id],
    queryFn: ({ signal }) => fetchTask(id!, signal),
    enabled: Boolean(id),
    staleTime: 1000 * 60 * 0.5,
  });
};

const toQueryString = (params: UseTasksParams) => {
  const qs = new URLSearchParams();
  if (params.search) qs.set("search", params.search);
  if (params.status !== undefined && params.status !== null)
    qs.set("status", String(params.status));
  if (params.dueDateFrom) qs.set("dueDateFrom", params.dueDateFrom);
  if (params.dueDateTo) qs.set("dueDateTo", params.dueDateTo);
  if (params.scope) qs.set("scope", params.scope);
  if (params.page !== undefined && params.page !== null)
    qs.set("page", String(params.page));
  if (params.pageSize !== undefined && params.pageSize !== null)
    qs.set("pageSize", String(params.pageSize));
  return qs.toString();
};

export const fetchTasks = async (
  params: UseTasksParams,
  signal?: AbortSignal
): Promise<PaginatedTasks> => {
  const qs = toQueryString(params);
  const path = `/tasks${qs ? `?${qs}` : ""}`;
  return apiFetch(path, { method: "GET" }, signal);
};

export const useTasks = (
  params: UseTasksParams = {},
  options?: UseQueryOptions<PaginatedTasks, Error>
) => {
  const {
    search = "",
    status = undefined,
    dueDateFrom = undefined,
    dueDateTo = undefined,
    scope = undefined,
    page = 1,
    pageSize = 20,
  } = params;

  return useQuery<PaginatedTasks, Error>({
    queryKey: [
      "tasks",
      search,
      status,
      dueDateFrom,
      dueDateTo,
      scope,
      page,
      pageSize,
    ],
    queryFn: ({ signal }) =>
      fetchTasks(
        { search, status, dueDateFrom, dueDateTo, scope, page, pageSize },
        signal
      ),
    staleTime: 1000 * 60 * 0.5,
    placeholderData: keepPreviousData,
    ...options,
  });
};

export const createTaskRequest = async (payload: CreateTaskDto) => {
  return apiFetch("/tasks", {
    method: "POST",
    body: JSON.stringify(payload),
    headers: {
      "Content-Type": "application/json",
    },
  });
};

export const useCreateTask = (
  options?: UseMutationOptions<Task, Error, CreateTaskDto>
) => {
  return useMutation<Task, Error, CreateTaskDto>({
    mutationFn: (payload: CreateTaskDto) => createTaskRequest(payload),
    onSuccess: (...args) => {
      queryClient.invalidateQueries({ queryKey: ["tasks"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      options?.onSuccess?.(...args);
    },
    ...options,
  });
};

export async function updateTask(
  id: string,
  dto: UpdateTaskDto,
  signal?: AbortSignal
) {
  return apiFetch(
    `/tasks/${id}`,
    {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(dto),
    },
    signal
  );
}

export const useUpdateTask = (options?: {
  onSuccess?: () => void;
  onError?: (err: unknown) => void;
}) => {
  return useMutation({
    mutationFn: ({
      id,
      dto,
      signal,
    }: {
      id: string;
      dto: UpdateTaskDto;
      signal?: AbortSignal;
    }) => updateTask(id, dto, signal),
    onSuccess: (variables) => {
      queryClient.invalidateQueries({ queryKey: ["task", variables.id] });
      queryClient.invalidateQueries({ queryKey: ["tasks"] });
      options?.onSuccess?.();
    },
    onError: (err) => options?.onError?.(err),
  });
};

export async function updateTaskStatus(
  id: string,
  status: string,
  signal?: AbortSignal
) {
  return apiFetch(
    `/tasks/${id}/status`,
    {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ status }),
    },
    signal
  );
}

export const useUpdateTaskStatus = (
  taskId: string,
  options?: {
    onSuccess?: () => void;
    onError?: (err: unknown) => void;
  }
) => {
  return useMutation({
    mutationFn: (status: string) => updateTaskStatus(taskId, status),
    onMutate: async (newStatus) => {
      await queryClient.cancelQueries({ queryKey: ["task", taskId] });

      const previous = queryClient.getQueryData(["task", taskId]);

      if (previous) {
        queryClient.setQueryData(["task", taskId], {
          ...previous,
          itemStatus: newStatus,
        });
      }

      queryClient.setQueryData(["tasks"], (old: any[] | undefined) =>
        old?.map((t) => (t.id === taskId ? { ...t, itemStatus: newStatus } : t))
      );

      return { previous };
    },
    onError: (err, context: any) => {
      if (context?.previous) {
        queryClient.setQueryData(["task", taskId], context.previous);
      }
      options?.onError?.(err);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ["task", taskId] });
      queryClient.invalidateQueries({ queryKey: ["tasks"] });
      options?.onSuccess?.();
    },
  });
};

export const deleteTaskRequest = async (id: string, signal?: AbortSignal) => {
  return apiFetch(
    `/tasks/${id}`,
    {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
      },
    },
    signal
  );
};

export const useDeleteTask = (
  taskId: string,
  options?: UseMutationOptions<unknown, Error, void>
) => {
  const userOnSuccess = options?.onSuccess;

  const { onSuccess: _omitOnSuccess, ...restOptions } = options || {};

  return useMutation({
    mutationFn: () => deleteTaskRequest(taskId),
    ...restOptions,
    onSuccess: (data, variables, context) => {
      queryClient.invalidateQueries({ queryKey: ["tasks"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });

      userOnSuccess?.(data, variables, context);
    },
  });
};

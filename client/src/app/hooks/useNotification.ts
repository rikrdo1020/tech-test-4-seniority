import {
  useQuery,
  useMutation,
  type UseQueryOptions,
  type UseMutationOptions,
  keepPreviousData,
} from "@tanstack/react-query";
import { apiFetch } from "../services/apiClient";
import { queryClient } from "../../main";
import type {
  CreateNotificationDto,
  NotificationDto,
  PagedResult,
  UseNotificationsParams,
} from "../types/Notification";

const toQueryString = (params: UseNotificationsParams) => {
  const qs = new URLSearchParams();
  if (params.page) qs.set("page", String(params.page));
  if (params.pageSize) qs.set("pageSize", String(params.pageSize));
  return qs.toString();
};

export const fetchNotifications = async (
  params: UseNotificationsParams = {},
  signal?: AbortSignal
): Promise<PagedResult<NotificationDto>> => {
  const qs = toQueryString(params);
  const path = `/notifications${qs ? `?${qs}` : ""}`;
  return apiFetch(path, { method: "GET" }, signal);
};

export const useNotifications = (
  params: UseNotificationsParams = { page: 1, pageSize: 20 },
  isActive: boolean = false,
  options?: UseQueryOptions<PagedResult<NotificationDto>, Error>
) => {
  return useQuery<PagedResult<NotificationDto>, Error>({
    queryKey: ["notifications", params],
    queryFn: ({ signal }) => fetchNotifications(params, signal),
    staleTime: 1000 * 60 * 0.5,
    placeholderData: keepPreviousData,
    refetchInterval: isActive ? 1000 * 60 : false,
    ...options,
  });
};

export const createNotificationRequest = async (
  payload: CreateNotificationDto
): Promise<NotificationDto> => {
  return apiFetch("/notifications", {
    method: "POST",
    body: JSON.stringify(payload),
    headers: { "Content-Type": "application/json" },
  });
};

export const useCreateNotification = (
  options?: UseMutationOptions<NotificationDto, Error, CreateNotificationDto>
) => {
  return useMutation<NotificationDto, Error, CreateNotificationDto>({
    mutationFn: (payload) => createNotificationRequest(payload),
    onSuccess: (data, variables, context) => {
      queryClient.invalidateQueries({
        queryKey: ["notifications"],
        exact: false,
      });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      options?.onSuccess?.(data, variables, context);
    },
    ...options,
  });
};

export const markAsReadRequest = async (id: string) => {
  return apiFetch(`/notifications/${id}/read`, {
    method: "POST",
  });
};

export const useMarkAsRead = (
  options?: UseMutationOptions<void, Error, string>
) => {
  return useMutation<void, Error, string>({
    mutationFn: (id) => markAsReadRequest(id),
    onSuccess: (data, id, ctx) => {
      queryClient.invalidateQueries({
        queryKey: ["notifications"],
        exact: false,
      });
      queryClient.invalidateQueries({
        queryKey: ["notifications-unread-count"],
      });
      options?.onSuccess?.(data, id, ctx);
    },
    ...options,
  });
};

export const markAllAsReadRequest = async () => {
  return apiFetch("/notifications/readAll", {
    method: "POST",
  });
};

export const useMarkAllAsRead = (
  options?: UseMutationOptions<void, Error, void>
) => {
  return useMutation<void, Error, void>({
    mutationFn: () => markAllAsReadRequest(),
    onSuccess: (data, vars, ctx) => {
      queryClient.invalidateQueries({
        queryKey: ["notifications"],
        exact: false,
      });
      queryClient.invalidateQueries({
        queryKey: ["notifications-unread-count"],
      });
      options?.onSuccess?.(data, vars, ctx);
    },
    ...options,
  });
};

const fetchUnreadCount = async (signal?: AbortSignal): Promise<number> => {
  const res = await apiFetch(
    "/notifications/unread-count",
    { method: "GET" },
    signal
  );
  return res.count;
};

export const useUnreadNotificationsCount = () => {
  return useQuery<number, Error>({
    queryKey: ["notifications-unread-count"],
    queryFn: ({ signal }) => fetchUnreadCount(signal),
    staleTime: 1000 * 30,
    refetchInterval: 1000 * 30,
    refetchIntervalInBackground: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: true,
    refetchOnMount: false,
    retry: 0,
  });
};

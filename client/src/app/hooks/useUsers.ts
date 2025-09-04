import {
  useMutation,
  useQuery,
  type UseMutationOptions,
  type UseQueryOptions,
  type UseQueryResult,
} from "@tanstack/react-query";
import type {
  PaginatedUsers,
  UpdateUserDto,
  User,
  UseUsersParams,
} from "../types/User";
import { apiFetch } from "../services/apiClient";
import { queryClient } from "../../main";

export const fetchUser = async (id: string, signal?: AbortSignal) =>
  apiFetch(`/users/${id}`, { method: "GET" }, signal);

export const useUser = (id?: string): UseQueryResult<User, Error> => {
  return useQuery<User, Error>({
    queryKey: ["task", id],
    queryFn: ({ signal }) => fetchUser(id!, signal),
    enabled: Boolean(id),
    staleTime: 1000 * 60 * 0.5,
  });
};

const toQueryString = (params: UseUsersParams) => {
  const qs = new URLSearchParams();
  if (params.search) qs.set("search", params.search);
  if (params.page) qs.set("page", String(params.page ?? 1));
  if (params.pageSize) qs.set("pageSize", String(params.pageSize ?? 20));
  const s = qs.toString();
  return s ? `?${s}` : "";
};

export const fetchUsers = async (
  params: UseUsersParams,
  signal?: AbortSignal
): Promise<PaginatedUsers> => {
  const qs = toQueryString(params);
  return apiFetch(`/users${qs}`, { method: "GET" }, signal);
};

type QueryKey = readonly ["users", UseUsersParams];

export const useUsers = (
  params: UseUsersParams = {},
  options?: Omit<
    UseQueryOptions<PaginatedUsers, Error, PaginatedUsers, QueryKey>,
    "queryKey" | "queryFn"
  >
) => {
  return useQuery<PaginatedUsers, Error, PaginatedUsers, QueryKey>({
    queryKey: ["users", params],
    queryFn: ({ signal }) => fetchUsers(params, signal),
    staleTime: 1000 * 30,
    retry: 1,
    ...options,
  });
};

export const updateUserRequest = async (
  dto: UpdateUserDto | Partial<User>,
  signal?: AbortSignal
): Promise<User> => {
  return apiFetch(
    `/users/me`,
    {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(dto),
    },
    signal
  );
};

export const useUpdateUser = (
  userId: string,
  options?: UseMutationOptions<User, Error, UpdateUserDto | Partial<User>>
) => {
  const userOnSuccess = options?.onSuccess;
  const { onSuccess: _omit, ...restOptions } = options || {};

  return useMutation<User, Error, UpdateUserDto | Partial<User>>({
    mutationFn: (dto) => updateUserRequest(dto),
    ...restOptions,
    onMutate: async (newDto) => {
      await queryClient.cancelQueries({ queryKey: ["user", userId] });
      await queryClient.cancelQueries({ queryKey: ["users"] });

      const previousUser = queryClient.getQueryData<User>(["user", userId]);
      const previousUsers = queryClient.getQueryData<User[]>(["users"]);

      if (previousUser) {
        queryClient.setQueryData<User>(["user", userId], {
          ...previousUser,
          ...newDto,
        } as User);
      }

      if (previousUsers) {
        queryClient.setQueryData<User[]>(
          ["users"],
          previousUsers.map((u) =>
            u.id === userId ? { ...u, ...(newDto as any) } : u
          )
        );
      }

      return { previousUser, previousUsers };
    },
    onError: (err, variables, context: any) => {
      // rollback
      if (context?.previousUser) {
        queryClient.setQueryData(["user", userId], context.previousUser);
      }
      if (context?.previousUsers) {
        queryClient.setQueryData(["users"], context.previousUsers);
      }
      options?.onError?.(err, variables, context);
    },
    onSettled: (data, _, variables, context) => {
      // invalidate to ensure fresh data
      queryClient.invalidateQueries({ queryKey: ["user", userId] });
      queryClient.invalidateQueries({ queryKey: ["users"] });

      if (data) userOnSuccess?.(data, variables, context);
    },
  });
};

export interface User {
  id: string;
  externalId?: string | null;
  name: string;
  email?: string | null;
}
export interface PaginatedUsers {
  items: User[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export type UseUsersParams = {
  search?: string | null;
  page?: number;
  pageSize?: number;
};

export interface UpdateUserDto {
  name: string;
  email?: string | null;
}

export const NotificationType = {
  Generic: 0,
  TaskAssigned: 1,
} as const;

export type UnreadCountResponse = {
  count: number;
};

export type NotificationType =
  (typeof NotificationType)[keyof typeof NotificationType];

export type NotificationDto = {
  id: string;
  recipientUserId: string;
  relatedTaskId?: string | null;
  title: string;
  message?: string | null;
  type: NotificationType;
  isRead: boolean;
  createdAt: string;
  readAt?: string | null;
};

export type CreateNotificationDto = {
  recipientUserId: string;
  title: string;
  message?: string | null;
  type?: NotificationType;
  relatedTaskId?: string | null;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

export type UseNotificationsParams = {
  page?: number;
  pageSize?: number;
};

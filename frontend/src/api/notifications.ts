const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5144";

export type AppNotification = {
  id: string;
  type:
    | "Info"
    | "Success"
    | "Warning"
    | "Error";
  title: string;
  message: string;
  actionUrl: string | null;
  isRead: boolean;
  createdAtUtc: string;
};

export async function getNotifications(
  token: string
): Promise<AppNotification[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/notifications`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (!response.ok) {
    throw new Error(
      "Unable to load notifications."
    );
  }

  return (
    await response.json()
  ) as AppNotification[];
}

export async function markNotificationAsRead(
  token: string,
  notificationId: string
): Promise<void> {
  const response = await fetch(
    `${API_BASE_URL}/api/notifications/${notificationId}/read`,
    {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (response.status === 404) {
    throw new Error(
      "Notification not found."
    );
  }

  if (!response.ok) {
    throw new Error(
      "Unable to update notification."
    );
  }
}

export async function markAllNotificationsAsRead(
  token: string
): Promise<void> {
  const response = await fetch(
    `${API_BASE_URL}/api/notifications/read-all`,
    {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (!response.ok) {
    throw new Error(
      "Unable to update notifications."
    );
  }
}
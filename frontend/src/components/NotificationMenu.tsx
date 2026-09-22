import {
  useEffect,
  useRef,
  useState,
} from "react";

import {
  Bell,
  CheckCheck,
  CheckCircle2,
  CircleAlert,
  Info,
  LoaderCircle,
  TriangleAlert,
} from "lucide-react";

import {
  getNotifications,
  markAllNotificationsAsRead,
  markNotificationAsRead,
  type AppNotification,
} from "../api/notifications";

type NotificationMenuProps = {
  token: string;
  onSessionExpired: () => void;
  onOpenProject: (
    projectId: string
  ) => void;
};

export function NotificationMenu({
  token,
  onSessionExpired,
  onOpenProject,
}: NotificationMenuProps) {
  const [notifications, setNotifications] =
    useState<AppNotification[]>([]);

  const [isOpen, setIsOpen] =
    useState(false);

  const [isLoading, setIsLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(null);

  const menuRef =
    useRef<HTMLDivElement>(null);

  const unreadCount =
    notifications.filter(
      (notification) =>
        !notification.isRead
    ).length;

  useEffect(() => {
    let cancelled = false;

    async function loadNotifications() {
      try {
        const results =
          await getNotifications(token);

        if (!cancelled) {
          setNotifications(results);
          setError(null);
        }
      } catch (exception) {
        if (cancelled) {
          return;
        }

        const message =
          exception instanceof Error
            ? exception.message
            : "Unable to load notifications.";

        if (message === "SESSION_EXPIRED") {
          onSessionExpired();
          return;
        }

        setError(message);
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadNotifications();

    const intervalId =
      window.setInterval(
        () => {
          void loadNotifications();
        },
        60000
      );

    return () => {
      cancelled = true;

      window.clearInterval(
        intervalId
      );
    };
  }, [token, onSessionExpired]);

  useEffect(() => {
    function handleOutsideClick(
      event: MouseEvent
    ) {
      if (
        menuRef.current &&
        !menuRef.current.contains(
          event.target as Node
        )
      ) {
        setIsOpen(false);
      }
    }

    document.addEventListener(
      "mousedown",
      handleOutsideClick
    );

    return () => {
      document.removeEventListener(
        "mousedown",
        handleOutsideClick
      );
    };
  }, []);

  async function handleMarkAsRead(
    notification: AppNotification
  ) {
    if (notification.isRead) {
      return;
    }

    try {
      await markNotificationAsRead(
        token,
        notification.id
      );

      setNotifications((current) =>
        current.map((item) =>
          item.id === notification.id
            ? {
                ...item,
                isRead: true,
              }
            : item
        )
      );
    } catch (exception) {
      handleError(exception);
    }
  }

  async function handleNotificationClick(
    notification: AppNotification
  ) {
    await handleMarkAsRead(
      notification
    );

    const projectPrefix =
      "project:";

    if (
      notification.actionUrl?.startsWith(
        projectPrefix
      )
    ) {
      const projectId =
        notification.actionUrl.slice(
          projectPrefix.length
        );

      if (projectId) {
        setIsOpen(false);
        onOpenProject(projectId);
      }
    }
  }

  async function handleMarkAllAsRead() {
    try {
      await markAllNotificationsAsRead(
        token
      );

      setNotifications((current) =>
        current.map(
          (notification) => ({
            ...notification,
            isRead: true,
          })
        )
      );
    } catch (exception) {
      handleError(exception);
    }
  }

  function handleError(
    exception: unknown
  ) {
    const message =
      exception instanceof Error
        ? exception.message
        : "Unable to update notifications.";

    if (message === "SESSION_EXPIRED") {
      onSessionExpired();
      return;
    }

    setError(message);
  }

  return (
  <>
    {isOpen && (
      <div
        className="notification-overlay"
        onClick={() => setIsOpen(false)}
        aria-hidden="true"
      />
    )}

    <div
      className={`notification-menu ${
        isOpen ? "open" : ""
      }`}
      ref={menuRef}
    >
      <button
        className="notification-button"
        type="button"
        aria-label="Notifications"
        aria-expanded={isOpen}
        onClick={() =>
          setIsOpen(
            (current) => !current
          )
        }
      >
        <Bell size={20} />

        {unreadCount > 0 && (
          <span className="notification-count">
            {unreadCount > 9
              ? "9+"
              : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <section className="notification-dropdown">
          <header className="notification-header">
            <div>
              <h2>Notifications</h2>

              <p>
                {unreadCount === 0
                  ? "You’re all caught up."
                  : `${unreadCount} unread notification${
                      unreadCount === 1
                        ? ""
                        : "s"
                    }`}
              </p>
            </div>

            {unreadCount > 0 && (
              <button
                type="button"
                onClick={() =>
                  void handleMarkAllAsRead()
                }
              >
                <CheckCheck size={16} />
                Mark all read
              </button>
            )}
          </header>

          {isLoading ? (
            <div className="notification-state">
              <LoaderCircle
                className="button-spinner"
                size={22}
              />

              <p>
                Loading notifications...
              </p>
            </div>
          ) : error ? (
            <div className="notification-state error">
              <CircleAlert size={23} />
              <p>{error}</p>
            </div>
          ) : notifications.length ===
            0 ? (
            <div className="notification-state">
              <Bell size={25} />

              <strong>
                No notifications yet
              </strong>

              <p>
                Document and deadline
                updates will appear here.
              </p>
            </div>
          ) : (
            <div className="notification-list">
              {notifications.map(
                (notification) => (
                  <button
                    className={`notification-item ${
                      notification.isRead
                        ? ""
                        : "unread"
                    }`}
                    type="button"
                    key={notification.id}
                    onClick={() =>
                      void handleNotificationClick(
                        notification
                      )
                    }
                  >
                    <span
                      className={`notification-type-icon ${notification.type.toLowerCase()}`}
                    >
                      <NotificationIcon
                        type={
                          notification.type
                        }
                      />
                    </span>

                    <span className="notification-content">
                      <strong>
                        {notification.title}
                      </strong>

                      <span>
                        {notification.message}
                      </span>

                      <time>
                        {formatNotificationTime(
                          notification.createdAtUtc
                        )}
                      </time>
                    </span>

                    {!notification.isRead && (
                      <span className="unread-dot" />
                    )}
                  </button>
                )
              )}
            </div>
          )}
        </section>
      )}
    </div>
    </>
  );
}

function NotificationIcon({
  type,
}: {
  type: AppNotification["type"];
}) {
  if (type === "Success") {
    return (
      <CheckCircle2 size={18} />
    );
  }

  if (type === "Warning") {
    return (
      <TriangleAlert size={18} />
    );
  }

  if (type === "Error") {
    return (
      <CircleAlert size={18} />
    );
  }

  return <Info size={18} />;
}

function formatNotificationTime(
  value: string
) {
  const date = new Date(value);

  const differenceInSeconds =
    Math.max(
      0,
      Math.floor(
        (Date.now() -
          date.getTime()) /
          1000
      )
    );

  if (differenceInSeconds < 60) {
    return "Just now";
  }

  const minutes =
    Math.floor(
      differenceInSeconds / 60
    );

  if (minutes < 60) {
    return `${minutes}m ago`;
  }

  const hours =
    Math.floor(minutes / 60);

  if (hours < 24) {
    return `${hours}h ago`;
  }

  const days =
    Math.floor(hours / 24);

  if (days < 7) {
    return `${days}d ago`;
  }

  return new Intl.DateTimeFormat(
    "en",
    {
      day: "numeric",
      month: "short",
      year: "numeric",
    }
  ).format(date);
}
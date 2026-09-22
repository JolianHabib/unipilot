import {
  ArrowRightLeft,
  CheckCircle2,
  CircleAlert,
  FileText,
  History,
  ListPlus,
  Pencil,
  RotateCcw,
  Sparkles,
  Trash2,
} from "lucide-react";
import {
  useEffect,
  useState,
  type ReactNode,
} from "react";

import {
  getProjectActivities,
  type ProjectActivity,
  type ProjectActivityType,
} from "../api/activities";

type ProjectActivityPanelProps = {
  token: string;
  projectId: string;
  onSessionExpired: () => void;
};

export function ProjectActivityPanel({
  token,
  projectId,
  onSessionExpired,
}: ProjectActivityPanelProps) {
  const [activities, setActivities] =
    useState<ProjectActivity[]>([]);

  const [isLoading, setIsLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadActivities() {
      setIsLoading(true);
      setError(null);

      try {
        const results =
          await getProjectActivities(
            token,
            projectId
          );

        if (!cancelled) {
          setActivities(results);
        }
      } catch (exception) {
        if (cancelled) {
          return;
        }

        const message =
          exception instanceof Error
            ? exception.message
            : "Unable to load project activity.";

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

    void loadActivities();

    return () => {
      cancelled = true;
    };
  }, [
    token,
    projectId,
    onSessionExpired,
  ]);

  if (isLoading) {
    return (
      <div className="activity-state">
        <div className="loading-spinner" />
        <p>Loading project activity...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="activity-state error">
        <CircleAlert size={24} />
        <h3>Unable to load activity</h3>
        <p>{error}</p>
      </div>
    );
  }

  if (activities.length === 0) {
    return (
      <div className="activity-state">
        <History size={28} />
        <h3>No activity yet</h3>
        <p>
          Project updates will appear here as
          you upload documents and manage tasks.
        </p>
      </div>
    );
  }

  return (
    <section className="project-activity-panel">
      <div className="activity-panel-heading">
        <div>
          <h2>Activity history</h2>
          <p>
            Recent document, requirement, and
            task updates.
          </p>
        </div>

        <span>
          {activities.length} event
          {activities.length === 1 ? "" : "s"}
        </span>
      </div>

      <div className="activity-timeline">
        {activities.map((activity) => (
          <article
            className="activity-entry"
            key={activity.id}
          >
            <div
              className={`activity-entry-icon ${getActivityTone(
                activity.type
              )}`}
            >
              {getActivityIcon(activity.type)}
            </div>

            <div className="activity-entry-content">
              <div className="activity-entry-title">
                <h3>{activity.title}</h3>
                <time
                  dateTime={activity.createdAtUtc}
                  title={formatFullDate(
                    activity.createdAtUtc
                  )}
                >
                  {formatRelativeTime(
                    activity.createdAtUtc
                  )}
                </time>
              </div>

              {activity.description && (
                <p>{activity.description}</p>
              )}
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

function getActivityIcon(
  type: ProjectActivityType
): ReactNode {
  switch (type) {
    case "DocumentUploaded":
    case "DocumentProcessed":
      return <FileText size={19} />;

    case "DocumentProcessingFailed":
      return <CircleAlert size={19} />;

    case "RequirementsExtracted":
      return <Sparkles size={19} />;

    case "RequirementCompleted":
      return <CheckCircle2 size={19} />;

    case "RequirementReopened":
      return <RotateCcw size={19} />;

    case "TaskCreated":
      return <ListPlus size={19} />;

    case "TaskUpdated":
      return <Pencil size={19} />;

    case "TaskMoved":
      return <ArrowRightLeft size={19} />;

    case "TaskDeleted":
      return <Trash2 size={19} />;

    default:
      return <History size={19} />;
  }
}

function getActivityTone(
  type: ProjectActivityType
) {
  if (
    type === "DocumentProcessingFailed" ||
    type === "TaskDeleted"
  ) {
    return "danger";
  }

  if (
    type === "DocumentProcessed" ||
    type === "RequirementCompleted"
  ) {
    return "success";
  }

  if (
    type === "RequirementsExtracted" ||
    type === "TaskCreated"
  ) {
    return "accent";
  }

  return "neutral";
}

function formatRelativeTime(value: string) {
  const timestamp = new Date(value).getTime();

  if (Number.isNaN(timestamp)) {
    return "Unknown time";
  }

  const seconds = Math.max(
    0,
    Math.floor((Date.now() - timestamp) / 1000)
  );

  if (seconds < 60) {
    return "Just now";
  }

  const minutes = Math.floor(seconds / 60);

  if (minutes < 60) {
    return `${minutes}m ago`;
  }

  const hours = Math.floor(minutes / 60);

  if (hours < 24) {
    return `${hours}h ago`;
  }

  const days = Math.floor(hours / 24);

  if (days < 7) {
    return `${days}d ago`;
  }

  return formatFullDate(value);
}

function formatFullDate(value: string) {
  return new Intl.DateTimeFormat("en", {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(value));
}

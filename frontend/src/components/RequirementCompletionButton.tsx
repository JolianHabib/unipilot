import {
  useState,
} from "react";

import {
  CheckCircle2,
  Circle,
  LoaderCircle,
} from "lucide-react";

import {
  setRequirementCompletion,
  type ProjectRequirement,
} from "../api/auth";

type RequirementCompletionButtonProps = {
  token: string;
  requirement: ProjectRequirement;
  onUpdated: (
    requirement: ProjectRequirement
  ) => void;
  onSessionExpired: () => void;
};

export function RequirementCompletionButton({
  token,
  requirement,
  onUpdated,
  onSessionExpired,
}: RequirementCompletionButtonProps) {
  const [isUpdating, setIsUpdating] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  async function handleToggle() {
    if (isUpdating) {
      return;
    }

    setIsUpdating(true);
    setError(null);

    try {
      const updated =
        await setRequirementCompletion(
          token,
          requirement.id,
          !requirement.isCompleted
        );

      onUpdated(updated);
    } catch (exception) {
      const message =
        exception instanceof Error
          ? exception.message
          : "Unable to update the requirement.";

      if (message === "SESSION_EXPIRED") {
        onSessionExpired();
        return;
      }

      setError(message);
    } finally {
      setIsUpdating(false);
    }
  }

  return (
    <div className="requirement-completion-control">
      <button
        className={`requirement-check requirement-check-button ${
          requirement.isCompleted
            ? "completed"
            : ""
        }`}
        type="button"
        disabled={isUpdating}
        aria-pressed={
          requirement.isCompleted
        }
        aria-label={
          requirement.isCompleted
            ? `Mark ${requirement.title} as incomplete`
            : `Mark ${requirement.title} as completed`
        }
        title={
          requirement.isCompleted
            ? "Mark as incomplete"
            : "Mark as completed"
        }
        onClick={() =>
          void handleToggle()
        }
      >
        {isUpdating ? (
          <LoaderCircle
            className="button-spinner"
            size={21}
          />
        ) : requirement.isCompleted ? (
          <CheckCircle2 size={21} />
        ) : (
          <Circle size={21} />
        )}
      </button>

      {error && (
        <small
          className="requirement-completion-error"
          role="alert"
        >
          {error}
        </small>
      )}
    </div>
  );
}

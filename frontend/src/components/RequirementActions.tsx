import {
  LoaderCircle,
  Pencil,
  Trash2,
} from "lucide-react";
import {
  useState,
} from "react";

import type {
  ProjectRequirement,
} from "../api/auth";
import {
  deleteRequirement,
} from "../api/requirements";
import {
  RequirementModal,
} from "./RequirementModal";

type RequirementActionsProps = {
  token: string;
  requirement: ProjectRequirement;
  onUpdated: (
    requirement: ProjectRequirement
  ) => void;
  onDeleted: (
    requirementId: string
  ) => void;
  onSessionExpired: () => void;
};

export function RequirementActions({
  token,
  requirement,
  onUpdated,
  onDeleted,
  onSessionExpired,
}: RequirementActionsProps) {
  const [isEditing, setIsEditing] =
    useState(false);
  const [isDeleting, setIsDeleting] =
    useState(false);
  const [error, setError] =
    useState<string | null>(null);

  async function handleDelete() {
    const confirmed = window.confirm(
      `Delete “${requirement.title}”?\n\n` +
        "This action cannot be undone."
    );

    if (!confirmed) {
      return;
    }

    setIsDeleting(true);
    setError(null);

    try {
      await deleteRequirement(
        token,
        requirement.id
      );

      onDeleted(requirement.id);
    } catch (exception) {
      const message =
        exception instanceof Error
          ? exception.message
          : "Unable to delete the requirement.";

      if (message === "SESSION_EXPIRED") {
        onSessionExpired();
        return;
      }

      setError(message);
    } finally {
      setIsDeleting(false);
    }
  }

  return (
    <>
      <div className="requirement-actions">
        <button
          className="requirement-action-button"
          type="button"
          onClick={() => {
            setError(null);
            setIsEditing(true);
          }}
          disabled={isDeleting}
          aria-label={`Edit ${requirement.title}`}
          title="Edit requirement"
        >
          <Pencil size={17} />
        </button>

        <button
          className="requirement-action-button delete"
          type="button"
          onClick={() => void handleDelete()}
          disabled={isDeleting}
          aria-label={`Delete ${requirement.title}`}
          title="Delete requirement"
        >
          {isDeleting ? (
            <LoaderCircle
              className="button-spinner"
              size={17}
            />
          ) : (
            <Trash2 size={17} />
          )}
        </button>

        {error && (
          <span
            className="requirement-action-error"
            role="alert"
          >
            {error}
          </span>
        )}
      </div>

      <RequirementModal
        token={token}
        requirement={
          isEditing ? requirement : null
        }
        onClose={() =>
          setIsEditing(false)
        }
        onUpdated={onUpdated}
        onSessionExpired={onSessionExpired}
      />
    </>
  );
}

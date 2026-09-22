import {
  useEffect,
  useState,
  type FormEvent,
} from "react";
import {
  LoaderCircle,
  Pencil,
  X,
} from "lucide-react";

import type {
  ProjectRequirement,
} from "../api/auth";
import {
  updateRequirement,
  type RequirementPriority,
  type RequirementType,
  type UpdateRequirementInput,
} from "../api/requirements";

type RequirementModalProps = {
  token: string;
  requirement: ProjectRequirement | null;
  onClose: () => void;
  onUpdated: (
    requirement: ProjectRequirement
  ) => void;
  onSessionExpired: () => void;
};

export function RequirementModal({
  token,
  requirement,
  onClose,
  onUpdated,
  onSessionExpired,
}: RequirementModalProps) {
  const [title, setTitle] = useState("");
  const [description, setDescription] =
    useState("");
  const [type, setType] =
    useState<RequirementType>("Functional");
  const [priority, setPriority] =
    useState<RequirementPriority>("Medium");
  const [isSubmitting, setIsSubmitting] =
    useState(false);
  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    if (!requirement) {
      return;
    }

    setTitle(requirement.title);
    setDescription(requirement.description);
    setType(normalizeType(requirement.type));
    setPriority(
      normalizePriority(requirement.priority)
    );
    setError(null);
    setIsSubmitting(false);
  }, [requirement]);

  useEffect(() => {
    if (!requirement) {
      return;
    }

    function handleEscape(event: KeyboardEvent) {
      if (
        event.key === "Escape" &&
        !isSubmitting
      ) {
        onClose();
      }
    }

    document.addEventListener(
      "keydown",
      handleEscape
    );

    return () => {
      document.removeEventListener(
        "keydown",
        handleEscape
      );
    };
  }, [requirement, isSubmitting, onClose]);

  if (!requirement) {
    return null;
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    if (!requirement) {
      return;
    }

    const trimmedTitle = title.trim();
    const trimmedDescription =
      description.trim();

    if (!trimmedTitle) {
      setError("Requirement title is required.");
      return;
    }

    if (!trimmedDescription) {
      setError(
        "Requirement description is required."
      );
      return;
    }

    const input: UpdateRequirementInput = {
      title: trimmedTitle,
      description: trimmedDescription,
      type,
      priority,
    };

    setIsSubmitting(true);
    setError(null);

    try {
      const updated = await updateRequirement(
        token,
        requirement.id,
        input
      );

      onUpdated(updated);
      onClose();
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
      setIsSubmitting(false);
    }
  }

  return (
    <div
      className="modal-backdrop"
      onMouseDown={(event) => {
        if (
          event.target === event.currentTarget &&
          !isSubmitting
        ) {
          onClose();
        }
      }}
    >
      <section
        className="course-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="requirement-modal-title"
      >
        <header className="course-modal-header">
          <div className="modal-heading-icon">
            <Pencil size={21} />
          </div>

          <div>
            <h2 id="requirement-modal-title">
              Edit requirement
            </h2>
            <p>
              Update the AI-extracted requirement.
            </p>
          </div>

          <button
            className="modal-close-button"
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label="Close"
          >
            <X size={20} />
          </button>
        </header>

        <form
          className="course-modal-form"
          onSubmit={handleSubmit}
        >
          <div className="form-field">
            <label htmlFor="requirement-title">
              Title
            </label>
            <input
              id="requirement-title"
              value={title}
              onChange={(event) =>
                setTitle(event.target.value)
              }
              maxLength={250}
              disabled={isSubmitting}
              autoFocus
              required
            />
          </div>

          <div className="form-field">
            <label htmlFor="requirement-description">
              Description
            </label>
            <textarea
              id="requirement-description"
              value={description}
              onChange={(event) =>
                setDescription(event.target.value)
              }
              rows={5}
              disabled={isSubmitting}
              required
            />
          </div>

          <div className="requirement-modal-grid">
            <div className="form-field">
              <label htmlFor="requirement-type">
                Type
              </label>
              <select
                id="requirement-type"
                value={type}
                onChange={(event) =>
                  setType(
                    event.target.value as RequirementType
                  )
                }
                disabled={isSubmitting}
              >
                <option value="Functional">
                  Functional
                </option>
                <option value="NonFunctional">
                  Non-functional
                </option>
                <option value="Constraint">
                  Constraint
                </option>
              </select>
            </div>

            <div className="form-field">
              <label htmlFor="requirement-priority">
                Priority
              </label>
              <select
                id="requirement-priority"
                value={priority}
                onChange={(event) =>
                  setPriority(
                    event.target.value as RequirementPriority
                  )
                }
                disabled={isSubmitting}
              >
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
                <option value="Critical">
                  Critical
                </option>
              </select>
            </div>
          </div>

          {error && (
            <p className="modal-error" role="alert">
              {error}
            </p>
          )}

          <footer className="course-modal-actions">
            <button
              className="cancel-button"
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </button>

            <button
              className="create-button"
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <LoaderCircle
                    className="button-spinner"
                    size={17}
                  />
                  Saving...
                </>
              ) : (
                "Save changes"
              )}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}

function normalizeType(
  value: string
): RequirementType {
  if (value === "NonFunctional") {
    return "NonFunctional";
  }

  if (value === "Constraint") {
    return "Constraint";
  }

  return "Functional";
}

function normalizePriority(
  value: string
): RequirementPriority {
  if (
    value === "Low" ||
    value === "High" ||
    value === "Critical"
  ) {
    return value;
  }

  return "Medium";
}

import {
  useEffect,
  useState,
  type FormEvent,
} from "react";

import {
  FolderKanban,
  LoaderCircle,
  X,
} from "lucide-react";

import type {
  AcademicProject,
  SaveProjectInput,
} from "../api/auth";

type ProjectModalProps = {
  isOpen: boolean;
  project?: AcademicProject | null;
  onClose: () => void;
  onSave: (
    input: SaveProjectInput
  ) => Promise<void>;
};

export function ProjectModal({
  isOpen,
  project = null,
  onClose,
  onSave,
}: ProjectModalProps) {
  const [title, setTitle] =
    useState("");

  const [description, setDescription] =
    useState("");

  const [dueDate, setDueDate] =
    useState("");

  const [isSubmitting, setIsSubmitting] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  const isEditing = project !== null;

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setTitle(project?.title ?? "");

    setDescription(
      project?.description ?? ""
    );

    setDueDate(
      project?.dueDateUtc
        ? project.dueDateUtc.slice(0, 10)
        : ""
    );

    setError(null);
    setIsSubmitting(false);
  }, [isOpen, project]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    function handleEscape(
      event: KeyboardEvent
    ) {
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
  }, [isOpen, isSubmitting, onClose]);

  if (!isOpen) {
    return null;
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    const trimmedTitle = title.trim();

    if (!trimmedTitle) {
      setError(
        "Project title is required."
      );
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      await onSave({
        title: trimmedTitle,
        description:
          description.trim() || null,
        dueDateUtc: dueDate
          ? `${dueDate}T00:00:00.000Z`
          : null,
      });

      onClose();
    } catch (submitError) {
      setError(
        submitError instanceof Error
          ? submitError.message
          : isEditing
            ? "Unable to update the project."
            : "Unable to create the project."
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div
      className="modal-backdrop"
      onMouseDown={(event) => {
        if (
          event.target ===
            event.currentTarget &&
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
        aria-labelledby="project-modal-title"
      >
        <header className="course-modal-header">
          <div className="modal-heading-icon">
            <FolderKanban size={22} />
          </div>

          <div>
            <h2 id="project-modal-title">
              {isEditing
                ? "Edit project"
                : "Create a new project"}
            </h2>

            <p>
              {isEditing
                ? "Update the project information."
                : "Add a project to this course."}
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
            <label htmlFor="project-title">
              Project title
            </label>

            <input
              id="project-title"
              value={title}
              onChange={(event) =>
                setTitle(event.target.value)
              }
              placeholder="Example: Secure Web Application"
              maxLength={200}
              disabled={isSubmitting}
              autoFocus
              required
            />
          </div>

          <div className="form-field">
            <label htmlFor="project-description">
              Description
              <span>Optional</span>
            </label>

            <textarea
              id="project-description"
              value={description}
              onChange={(event) =>
                setDescription(
                  event.target.value
                )
              }
              placeholder="What is this project about?"
              maxLength={2000}
              rows={4}
              disabled={isSubmitting}
            />
          </div>

          <div className="form-field">
            <label htmlFor="project-due-date">
              Due date
              <span>Optional</span>
            </label>

            <input
              id="project-due-date"
              type="date"
              value={dueDate}
              onChange={(event) =>
                setDueDate(
                  event.target.value
                )
              }
              disabled={isSubmitting}
            />
          </div>

          {error && (
            <p
              className="modal-error"
              role="alert"
            >
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

                  {isEditing
                    ? "Saving..."
                    : "Creating..."}
                </>
              ) : isEditing ? (
                "Save changes"
              ) : (
                "Create project"
              )}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
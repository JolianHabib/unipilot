import {
  useState,
  type FormEvent,
} from "react";
import {
  ListPlus,
  LoaderCircle,
  Plus,
  X,
} from "lucide-react";

import type {
  ProjectRequirement,
} from "../api/auth";
import {
  createRequirement,
  type RequirementPriority,
  type RequirementType,
} from "../api/requirements";

type CreateRequirementButtonProps = {
  token: string;
  projectId: string;
  onCreated: (
    requirement: ProjectRequirement
  ) => void;
  onSessionExpired: () => void;
};

export function CreateRequirementButton({
  token,
  projectId,
  onCreated,
  onSessionExpired,
}: CreateRequirementButtonProps) {
  const [isOpen, setIsOpen] = useState(false);
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

  function openModal() {
    setTitle("");
    setDescription("");
    setType("Functional");
    setPriority("Medium");
    setError(null);
    setIsOpen(true);
  }

  function closeModal() {
    if (!isSubmitting) {
      setIsOpen(false);
    }
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

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

    setIsSubmitting(true);
    setError(null);

    try {
      const created = await createRequirement(
        token,
        projectId,
        {
          title: trimmedTitle,
          description: trimmedDescription,
          type,
          priority,
        }
      );

      onCreated(created);
      setIsOpen(false);
    } catch (exception) {
      const message =
        exception instanceof Error
          ? exception.message
          : "Unable to create the requirement.";

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
    <>
      <button
        className="add-requirement-button"
        type="button"
        onClick={openModal}
      >
        <Plus size={17} />
        Add requirement
      </button>

      {isOpen && (
        <div
          className="modal-backdrop"
          onMouseDown={(event) => {
            if (
              event.target === event.currentTarget
            ) {
              closeModal();
            }
          }}
        >
          <section
            className="course-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="create-requirement-title"
          >
            <header className="course-modal-header">
              <div className="modal-heading-icon">
                <ListPlus size={21} />
              </div>

              <div>
                <h2 id="create-requirement-title">
                  Add requirement
                </h2>
                <p>
                  Create a project requirement manually.
                </p>
              </div>

              <button
                className="modal-close-button"
                type="button"
                onClick={closeModal}
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
                <label htmlFor="new-requirement-title">
                  Title
                </label>
                <input
                  id="new-requirement-title"
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
                <label htmlFor="new-requirement-description">
                  Description
                </label>
                <textarea
                  id="new-requirement-description"
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
                  <label htmlFor="new-requirement-type">
                    Type
                  </label>
                  <select
                    id="new-requirement-type"
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
                  <label htmlFor="new-requirement-priority">
                    Priority
                  </label>
                  <select
                    id="new-requirement-priority"
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
                  onClick={closeModal}
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
                      Creating...
                    </>
                  ) : (
                    "Create requirement"
                  )}
                </button>
              </footer>
            </form>
          </section>
        </div>
      )}
    </>
  );
}

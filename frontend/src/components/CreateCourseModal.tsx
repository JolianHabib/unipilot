import {
  useEffect,
  useState,
  type FormEvent,
} from "react";

import {
  BookOpen,
  LoaderCircle,
  X,
} from "lucide-react";

import type {
  CreateCourseInput,
} from "../api/auth";

type CreateCourseModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onCreate: (
    input: CreateCourseInput
  ) => Promise<void>;
};

export function CreateCourseModal({
  isOpen,
  onClose,
  onCreate,
}: CreateCourseModalProps) {
  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [description, setDescription] =
    useState("");

  const [isSubmitting, setIsSubmitting] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setName("");
    setCode("");
    setDescription("");
    setError(null);
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    function handleEscape(event: KeyboardEvent) {
      if (event.key === "Escape") {
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
  }, [isOpen, onClose]);

  if (!isOpen) {
    return null;
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    const trimmedName = name.trim();

    if (!trimmedName) {
      setError("Course name is required.");
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      await onCreate({
        name: trimmedName,
        code: code.trim() || null,
        description:
          description.trim() || null,
      });

      onClose();
    } catch (submitError) {
      setError(
        submitError instanceof Error
          ? submitError.message
          : "Unable to create the course."
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div
      className="modal-backdrop"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
    >
      <section
        className="course-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="create-course-title"
      >
        <header className="course-modal-header">
          <div className="modal-heading-icon">
            <BookOpen size={22} />
          </div>

          <div>
            <h2 id="create-course-title">
              Create a new course
            </h2>

            <p>
              Add a course to organize your
              academic projects.
            </p>
          </div>

          <button
            className="modal-close-button"
            onClick={onClose}
            type="button"
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
            <label htmlFor="course-name">
              Course name
            </label>

            <input
              id="course-name"
              value={name}
              onChange={(event) =>
                setName(event.target.value)
              }
              placeholder="Example: Software Engineering"
              maxLength={150}
              autoFocus
            />
          </div>

          <div className="form-field">
            <label htmlFor="course-code">
              Course code
              <span>Optional</span>
            </label>

            <input
              id="course-code"
              value={code}
              onChange={(event) =>
                setCode(event.target.value)
              }
              placeholder="Example: SE-2026"
              maxLength={50}
            />
          </div>

          <div className="form-field">
            <label htmlFor="course-description">
              Description
              <span>Optional</span>
            </label>

            <textarea
              id="course-description"
              value={description}
              onChange={(event) =>
                setDescription(event.target.value)
              }
              placeholder="What is this course about?"
              maxLength={1000}
              rows={4}
            />
          </div>

          {error && (
            <p className="modal-error">
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
                  Creating...
                </>
              ) : (
                "Create course"
              )}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
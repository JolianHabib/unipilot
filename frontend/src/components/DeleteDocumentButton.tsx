import { useState } from "react";

import {
  LoaderCircle,
  Trash2,
  X,
} from "lucide-react";

import {
  deleteProjectDocument,
  type ProjectDocument,
} from "../api/auth";

type DeleteDocumentButtonProps = {
  token: string;
  projectId: string;
  document: ProjectDocument;
  onDeleted: (
    documentId: string
  ) => void;
  onSessionExpired: () => void;
};

export function DeleteDocumentButton({
  token,
  projectId,
  document,
  onDeleted,
  onSessionExpired,
}: DeleteDocumentButtonProps) {
  const [isOpen, setIsOpen] =
    useState(false);

  const [isDeleting, setIsDeleting] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  function closeModal() {
    if (isDeleting) {
      return;
    }

    setIsOpen(false);
    setError(null);
  }

  async function handleDelete() {
    setIsDeleting(true);
    setError(null);

    try {
      await deleteProjectDocument(
        token,
        projectId,
        document.id
      );

      onDeleted(document.id);
      setIsOpen(false);
    } catch (deleteError) {
      const message =
        deleteError instanceof Error
          ? deleteError.message
          : "Unable to delete the document.";

      if (
        message === "SESSION_EXPIRED"
      ) {
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
      <button
        className="document-delete-button"
        type="button"
        onClick={() => {
          setError(null);
          setIsOpen(true);
        }}
        aria-label={`Delete ${document.originalFileName}`}
        title="Delete PDF"
      >
        <Trash2 size={17} />
      </button>

      {isOpen && (
        <div
          className="modal-backdrop"
          onMouseDown={(event) => {
            if (
              event.target ===
              event.currentTarget
            ) {
              closeModal();
            }
          }}
        >
          <section
            className="delete-course-modal"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="delete-document-title"
          >
            <button
              className="modal-close-button delete-modal-close"
              type="button"
              onClick={closeModal}
              disabled={isDeleting}
              aria-label="Close"
            >
              <X size={20} />
            </button>

            <div className="delete-modal-icon">
              <Trash2 size={25} />
            </div>

            <h2 id="delete-document-title">
              Delete PDF?
            </h2>

            <p>
              <strong>
                {document.originalFileName}
              </strong>{" "}
              and its extracted pages will be
              permanently deleted. Existing
              requirements and tasks will be
              preserved.
            </p>

            {error && (
              <p
                className="modal-error"
                role="alert"
              >
                {error}
              </p>
            )}

            <div className="delete-modal-actions">
              <button
                className="cancel-button"
                type="button"
                onClick={closeModal}
                disabled={isDeleting}
              >
                Cancel
              </button>

              <button
                className="confirm-delete-button"
                type="button"
                onClick={() =>
                  void handleDelete()
                }
                disabled={isDeleting}
              >
                {isDeleting ? (
                  <>
                    <LoaderCircle
                      className="button-spinner"
                      size={17}
                    />
                    Deleting...
                  </>
                ) : (
                  <>
                    <Trash2 size={17} />
                    Delete PDF
                  </>
                )}
              </button>
            </div>
          </section>
        </div>
      )}
    </>
  );
}
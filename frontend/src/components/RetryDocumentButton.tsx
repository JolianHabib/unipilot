import {
  useState,
} from "react";

import {
  LoaderCircle,
  RotateCcw,
} from "lucide-react";

import {
  retryProjectDocumentProcessing,
} from "../api/documents";

import type {
  ProjectDocument,
} from "../api/auth";

type RetryDocumentButtonProps = {
  token: string;
  projectId: string;
  document: ProjectDocument;
  onRetried: (
    document: ProjectDocument
  ) => void;
  onSessionExpired: () => void;
};

export function RetryDocumentButton({
  token,
  projectId,
  document,
  onRetried,
  onSessionExpired,
}: RetryDocumentButtonProps) {
  const [isRetrying, setIsRetrying] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  async function handleRetry() {
    setIsRetrying(true);
    setError(null);

    try {
      const updatedDocument =
        await retryProjectDocumentProcessing(
          token,
          projectId,
          document.id
        );

      onRetried(updatedDocument);
    } catch (exception) {
      const message =
        exception instanceof Error
          ? exception.message
          : "Unable to retry processing.";

      if (message === "SESSION_EXPIRED") {
        onSessionExpired();
        return;
      }

      setError(message);
    } finally {
      setIsRetrying(false);
    }
  }

  return (
    <div className="retry-document-control">
      <button
        className="retry-document-button"
        type="button"
        disabled={isRetrying}
        onClick={() =>
          void handleRetry()
        }
      >
        {isRetrying ? (
          <LoaderCircle
            className="button-spinner"
            size={16}
          />
        ) : (
          <RotateCcw size={16} />
        )}

        {isRetrying
          ? "Retrying..."
          : "Retry processing"}
      </button>

      {error && (
        <small
          className="retry-document-error"
          role="alert"
        >
          {error}
        </small>
      )}
    </div>
  );
}
import {
  useState,
} from "react";

import {
  ExternalLink,
  LoaderCircle,
} from "lucide-react";

import {
  getProjectDocumentFile,
} from "../api/documents";

type RequirementSourceButtonProps = {
  token: string;
  projectId: string;
  documentId: string;
  pageNumber: number;
  onSessionExpired: () => void;
};

export function RequirementSourceButton({
  token,
  projectId,
  documentId,
  pageNumber,
  onSessionExpired,
}: RequirementSourceButtonProps) {
  const [isLoading, setIsLoading] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  async function handleOpenSource() {
    const openedWindow = window.open(
      "about:blank",
      "_blank"
    );

    if (!openedWindow) {
      setError(
        "Your browser blocked the PDF window."
      );
      return;
    }

    openedWindow.opener = null;
    openedWindow.document.title =
      "Loading PDF...";

    setIsLoading(true);
    setError(null);

    try {
      const file =
        await getProjectDocumentFile(
          token,
          projectId,
          documentId
        );

      const fileUrl =
        URL.createObjectURL(file);

      openedWindow.location.href =
        `${fileUrl}#page=${pageNumber}`;

      window.setTimeout(() => {
        URL.revokeObjectURL(fileUrl);
      }, 60_000);
    } catch (exception) {
      openedWindow.close();

      const message =
        exception instanceof Error
          ? exception.message
          : "Unable to open the requirement source.";

      if (message === "SESSION_EXPIRED") {
        onSessionExpired();
        return;
      }

      setError(message);
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="requirement-source-control">
      <button
        className="requirement-source-button"
        type="button"
        disabled={isLoading}
        onClick={() =>
          void handleOpenSource()
        }
        aria-label={`Open requirement source on page ${pageNumber}`}
      >
        {isLoading ? (
          <LoaderCircle
            className="button-spinner"
            size={15}
          />
        ) : (
          <ExternalLink size={15} />
        )}

        <span>
          {isLoading
            ? "Opening source..."
            : `View source · Page ${pageNumber}`}
        </span>
      </button>

      {error && (
        <small
          className="requirement-source-error"
          role="alert"
        >
          {error}
        </small>
      )}
    </div>
  );
}

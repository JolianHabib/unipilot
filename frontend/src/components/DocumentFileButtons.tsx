import {
  useState,
} from "react";

import {
  Download,
  ExternalLink,
  LoaderCircle,
} from "lucide-react";

import {
  getProjectDocumentFile,
} from "../api/documents";

type DocumentFileButtonsProps = {
  token: string;
  projectId: string;
  documentId: string;
  fileName: string;
  onSessionExpired: () => void;
};

type FileAction =
  | "view"
  | "download"
  | null;

export function DocumentFileButtons({
  token,
  projectId,
  documentId,
  fileName,
  onSessionExpired,
}: DocumentFileButtonsProps) {
  const [fileAction, setFileAction] =
    useState<FileAction>(null);

  const [error, setError] =
    useState<string | null>(null);

  async function loadFile(
    action: Exclude<FileAction, null>
  ) {
    setFileAction(action);
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

      if (action === "view") {
        const openedWindow =
          window.open(
            fileUrl,
            "_blank",
            "noopener,noreferrer"
          );

        if (!openedWindow) {
          URL.revokeObjectURL(fileUrl);

          throw new Error(
            "Your browser blocked the PDF window."
          );
        }

        window.setTimeout(() => {
          URL.revokeObjectURL(fileUrl);
        }, 60_000);

        return;
      }

      const link =
        document.createElement("a");

      link.href = fileUrl;
      link.download = fileName;

      document.body.appendChild(link);
      link.click();
      link.remove();

      URL.revokeObjectURL(fileUrl);
    } catch (exception) {
      const message =
        exception instanceof Error
          ? exception.message
          : "Unable to open the document.";

      if (message === "SESSION_EXPIRED") {
        onSessionExpired();
        return;
      }

      setError(message);
    } finally {
      setFileAction(null);
    }
  }

  return (
    <div className="document-file-controls">
      <button
        className="document-file-button"
        type="button"
        disabled={fileAction !== null}
        onClick={() =>
          void loadFile("view")
        }
        title="View PDF"
        aria-label={`View ${fileName}`}
      >
        {fileAction === "view" ? (
          <LoaderCircle
            className="button-spinner"
            size={17}
          />
        ) : (
          <ExternalLink size={17} />
        )}

        <span>View PDF</span>
      </button>

      <button
        className="document-file-button icon-only"
        type="button"
        disabled={fileAction !== null}
        onClick={() =>
          void loadFile("download")
        }
        title="Download PDF"
        aria-label={`Download ${fileName}`}
      >
        {fileAction === "download" ? (
          <LoaderCircle
            className="button-spinner"
            size={17}
          />
        ) : (
          <Download size={17} />
        )}
      </button>

      {error && (
        <small
          className="document-file-error"
          role="alert"
        >
          {error}
        </small>
      )}
    </div>
  );
}
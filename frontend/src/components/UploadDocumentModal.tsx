import {
  useEffect,
  useRef,
  useState,
  type ChangeEvent,
  type FormEvent,
} from "react";

import {
  FileText,
  LoaderCircle,
  Upload,
  X,
} from "lucide-react";

type UploadDocumentModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onUpload: (file: File) => Promise<void>;
};

export function UploadDocumentModal({
  isOpen,
  onClose,
  onUpload,
}: UploadDocumentModalProps) {
  const inputRef =
    useRef<HTMLInputElement | null>(null);

  const [file, setFile] =
    useState<File | null>(null);

  const [isUploading, setIsUploading] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setFile(null);
    setError(null);

    if (inputRef.current) {
      inputRef.current.value = "";
    }
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    function handleEscape(event: KeyboardEvent) {
      if (
        event.key === "Escape" &&
        !isUploading
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
  }, [isOpen, isUploading, onClose]);

  if (!isOpen) {
    return null;
  }

  function handleFileChange(
    event: ChangeEvent<HTMLInputElement>
  ) {
    const selectedFile =
      event.target.files?.[0] ?? null;

    setError(null);

    if (!selectedFile) {
      setFile(null);
      return;
    }

    const isPdf =
      selectedFile.type === "application/pdf" ||
      selectedFile.name
        .toLowerCase()
        .endsWith(".pdf");

    if (!isPdf) {
      setFile(null);
      setError("Only PDF files are allowed.");
      event.target.value = "";
      return;
    }

    setFile(selectedFile);
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    if (!file) {
      setError("Please select a PDF file.");
      return;
    }

    setIsUploading(true);
    setError(null);

    try {
      await onUpload(file);
      onClose();
    } catch (uploadError) {
      setError(
        uploadError instanceof Error
          ? uploadError.message
          : "Unable to upload the PDF."
      );
    } finally {
      setIsUploading(false);
    }
  }

  function formatFileSize(bytes: number) {
    return `${(
      bytes /
      (1024 * 1024)
    ).toFixed(2)} MB`;
  }

  return (
    <div
      className="modal-backdrop"
      onMouseDown={(event) => {
        if (
          event.target === event.currentTarget &&
          !isUploading
        ) {
          onClose();
        }
      }}
    >
      <section
        className="course-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="upload-document-title"
      >
        <header className="course-modal-header">
          <div className="modal-heading-icon">
            <Upload size={22} />
          </div>

          <div>
            <h2 id="upload-document-title">
              Upload project document
            </h2>

            <p>
              Upload a PDF to extract its pages and
              analyze requirements.
            </p>
          </div>

          <button
            className="modal-close-button"
            type="button"
            onClick={onClose}
            disabled={isUploading}
            aria-label="Close"
          >
            <X size={20} />
          </button>
        </header>

        <form
          className="course-modal-form"
          onSubmit={handleSubmit}
        >
          <label
            className={`pdf-drop-zone ${
              file ? "has-file" : ""
            }`}
            htmlFor="project-pdf"
          >
            <input
              ref={inputRef}
              id="project-pdf"
              type="file"
              accept=".pdf,application/pdf"
              onChange={handleFileChange}
              disabled={isUploading}
            />

            {file ? (
              <>
                <div className="selected-pdf-icon">
                  <FileText size={27} />
                </div>

                <strong>{file.name}</strong>

                <span>
                  {formatFileSize(file.size)}
                </span>

                <small>
                  Click to select another PDF
                </small>
              </>
            ) : (
              <>
                <div className="upload-zone-icon">
                  <Upload size={27} />
                </div>

                <strong>Select a PDF file</strong>

                <span>
                  Click here to browse your files
                </span>
              </>
            )}
          </label>

          <div className="document-type-note">
            <span>Document type</span>
            <strong>Specification</strong>
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
              disabled={isUploading}
            >
              Cancel
            </button>

            <button
              className="create-button"
              type="submit"
              disabled={isUploading || !file}
            >
              {isUploading ? (
                <>
                  <LoaderCircle
                    className="button-spinner"
                    size={17}
                  />
                  Processing PDF...
                </>
              ) : (
                <>
                  <Upload size={17} />
                  Upload PDF
                </>
              )}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
import type {
  ProjectDocument,
} from "./auth";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5144";

export async function getProjectDocumentFile(
  token: string,
  projectId: string,
  documentId: string
): Promise<Blob> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/documents/${documentId}/file`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (response.status === 404) {
    throw new Error(
      "Document not found."
    );
  }

  if (!response.ok) {
    throw new Error(
      "Unable to open the document."
    );
  }

  return await response.blob();
}
export async function retryProjectDocumentProcessing(
  token: string,
  projectId: string,
  documentId: string
): Promise<ProjectDocument> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/documents/${documentId}/retry`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (response.ok) {
    return (
      await response.json()
    ) as ProjectDocument;
  }

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  let serverMessage: string | null = null;

  try {
    const body =
      (await response.json()) as {
        message?: string;
      };

    serverMessage =
      body.message ?? null;
  } catch {
    serverMessage = null;
  }

  if (response.status === 404) {
    throw new Error(
      serverMessage ??
        "Document not found."
    );
  }

  if (response.status === 409) {
    throw new Error(
      serverMessage ??
        "This document cannot be retried."
    );
  }

  if (response.status === 422) {
    throw new Error(
      serverMessage ??
        "Document processing failed again."
    );
  }

  throw new Error(
    serverMessage ??
      "Unable to retry document processing."
  );
}
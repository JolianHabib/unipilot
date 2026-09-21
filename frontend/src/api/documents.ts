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
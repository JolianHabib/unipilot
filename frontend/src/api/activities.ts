const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5144";

export type ProjectActivityType =
  | "ProjectCreated"
  | "ProjectStatusChanged"
  | "DocumentUploaded"
  | "DocumentProcessed"
  | "DocumentProcessingFailed"
  | "RequirementsExtracted"
  | "RequirementCreated"
  | "RequirementCompleted"
  | "RequirementReopened"
  | "RequirementUpdated"
  | "RequirementDeleted"
  | "TaskCreated"
  | "TaskUpdated"
  | "TaskMoved"
  | "TaskDeleted";

export type ProjectActivity = {
  id: string;
  academicProjectId: string;
  type: ProjectActivityType;
  title: string;
  description: string | null;
  createdAtUtc: string;
};

type ApiError = {
  message?: string;
};

async function readError(
  response: Response,
  fallback: string
): Promise<Error> {
  if (response.status === 401) {
    return new Error("SESSION_EXPIRED");
  }

  try {
    const body =
      (await response.json()) as ApiError;

    return new Error(body.message ?? fallback);
  } catch {
    return new Error(fallback);
  }
}

export async function getProjectActivities(
  token: string,
  projectId: string
): Promise<ProjectActivity[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/activities`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (!response.ok) {
    throw await readError(
      response,
      "Unable to load project activity."
    );
  }

  return (
    await response.json()
  ) as ProjectActivity[];
}

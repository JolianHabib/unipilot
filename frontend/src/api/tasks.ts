const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5144";

export type ProjectTaskStatus =
  | "ToDo"
  | "InProgress"
  | "Done";

export type ProjectTaskPriority =
  | "Low"
  | "Medium"
  | "High";

export type ProjectTask = {
  id: string;
  academicProjectId: string;
  projectRequirementId: string | null;
  title: string;
  description: string | null;
  status: ProjectTaskStatus;
  priority: ProjectTaskPriority;
  dueDateUtc: string | null;
  position: number;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type CreateProjectTaskInput = {
  projectRequirementId?: string | null;
  title: string;
  description?: string | null;
  priority: ProjectTaskPriority;
  dueDateUtc?: string | null;
};

export type UpdateProjectTaskInput =
  CreateProjectTaskInput;

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

export async function getProjectTasks(
  token: string,
  projectId: string
): Promise<ProjectTask[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/tasks`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (!response.ok) {
    throw await readError(
      response,
      "Unable to load project tasks."
    );
  }

  return (await response.json()) as ProjectTask[];
}

export async function createProjectTask(
  token: string,
  projectId: string,
  input: CreateProjectTaskInput
): Promise<ProjectTask> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/tasks`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(input),
    }
  );

  if (!response.ok) {
    throw await readError(
      response,
      "Unable to create the task."
    );
  }

  return (await response.json()) as ProjectTask;
}

export async function updateProjectTask(
  token: string,
  projectId: string,
  taskId: string,
  input: UpdateProjectTaskInput
): Promise<ProjectTask> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/tasks/${taskId}`,
    {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(input),
    }
  );

  if (!response.ok) {
    throw await readError(
      response,
      "Unable to update the task."
    );
  }

  return (await response.json()) as ProjectTask;
}

export async function moveProjectTask(
  token: string,
  projectId: string,
  taskId: string,
  status: ProjectTaskStatus,
  position: number
): Promise<ProjectTask> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/tasks/${taskId}/move`,
    {
      method: "PATCH",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        status,
        position,
      }),
    }
  );

  if (!response.ok) {
    throw await readError(
      response,
      "Unable to move the task."
    );
  }

  return (await response.json()) as ProjectTask;
}

export async function deleteProjectTask(
  token: string,
  projectId: string,
  taskId: string
): Promise<void> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/tasks/${taskId}`,
    {
      method: "DELETE",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (!response.ok) {
    throw await readError(
      response,
      "Unable to delete the task."
    );
  }
}
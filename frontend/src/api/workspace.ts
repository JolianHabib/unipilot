const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5144";

import type {
  AcademicProject,
  ProjectRequirement,
} from "./auth";

export async function getWorkspaceProjects(
  token: string
): Promise<AcademicProject[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (!response.ok) {
    throw new Error("Unable to load projects.");
  }

  return (await response.json()) as AcademicProject[];
}

export async function getWorkspaceRequirements(
  token: string
): Promise<ProjectRequirement[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/requirements`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (!response.ok) {
    throw new Error("Unable to load requirements.");
  }

  return (await response.json()) as ProjectRequirement[];
}

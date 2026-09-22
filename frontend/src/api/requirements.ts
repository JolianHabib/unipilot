import type {
  ProjectRequirement,
} from "./auth";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5144";

export type RequirementType =
  | "Functional"
  | "NonFunctional"
  | "Constraint";

export type RequirementPriority =
  | "Low"
  | "Medium"
  | "High"
  | "Critical";

export type RequirementInput = {
  title: string;
  description: string;
  type: RequirementType;
  priority: RequirementPriority;
};

export type UpdateRequirementInput =
  RequirementInput;

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

    return new Error(
      body.message ?? fallback
    );
  } catch {
    return new Error(fallback);
  }
}

export async function createRequirement(
  token: string,
  projectId: string,
  input: RequirementInput
): Promise<ProjectRequirement> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/requirements`,
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
      "Unable to create the requirement."
    );
  }

  return (
    await response.json()
  ) as ProjectRequirement;
}

export async function updateRequirement(
  token: string,
  requirementId: string,
  input: UpdateRequirementInput
): Promise<ProjectRequirement> {
  const response = await fetch(
    `${API_BASE_URL}/api/requirements/${requirementId}`,
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
      "Unable to update the requirement."
    );
  }

  return (
    await response.json()
  ) as ProjectRequirement;
}

export async function deleteRequirement(
  token: string,
  requirementId: string
): Promise<void> {
  const response = await fetch(
    `${API_BASE_URL}/api/requirements/${requirementId}`,
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
      "Unable to delete the requirement."
    );
  }
}

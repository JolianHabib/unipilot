const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5144";

export type ProjectMemberRole = "Viewer" | "Editor";

export type ProjectMember = {
  id: string;
  academicProjectId: string;
  userId: string | null;
  fullName: string;
  email: string;
  role: ProjectMemberRole;
  joinedAtUtc: string;
  isPending: boolean;
};

type ApiError = {
  message?: string;
};

async function readError(response: Response, fallback: string) {
  if (response.status === 401) {
    return new Error("SESSION_EXPIRED");
  }

  try {
    const body = (await response.json()) as ApiError;
    return new Error(body.message ?? fallback);
  } catch {
    return new Error(fallback);
  }
}

export async function getProjectMembers(
  token: string,
  projectId: string
): Promise<ProjectMember[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/members`,
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  );

  if (!response.ok) {
    throw await readError(response, "Unable to load project members.");
  }

  return (await response.json()) as ProjectMember[];
}

export async function addProjectMember(
  token: string,
  projectId: string,
  email: string,
  role: ProjectMemberRole
): Promise<ProjectMember> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/members`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ email, role }),
    }
  );

  if (!response.ok) {
    throw await readError(response, "Unable to add the project member.");
  }

  return (await response.json()) as ProjectMember;
}

export async function resendProjectInvitation(
  token: string,
  projectId: string,
  memberId: string
): Promise<ProjectMember> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/members/${memberId}/resend-invitation`,
    {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
    }
  );

  if (!response.ok) {
    throw await readError(response, "Unable to resend the invitation.");
  }

  return (await response.json()) as ProjectMember;
}

export async function updateProjectMemberRole(
  token: string,
  projectId: string,
  memberId: string,
  role: ProjectMemberRole
): Promise<ProjectMember> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/members/${memberId}`,
    {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ role }),
    }
  );

  if (!response.ok) {
    throw await readError(response, "Unable to change the member role.");
  }

  return (await response.json()) as ProjectMember;
}

export async function removeProjectMember(
  token: string,
  projectId: string,
  memberId: string
): Promise<void> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/members/${memberId}`,
    {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` },
    }
  );

  if (!response.ok) {
    throw await readError(response, "Unable to remove the project member.");
  }
}

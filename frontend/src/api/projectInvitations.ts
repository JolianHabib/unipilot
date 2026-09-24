const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5144";

type InvitationError = {
  message?: string;
};

export type AcceptProjectInvitationResult = {
  academicProjectId: string;
};

export async function acceptProjectInvitation(
  token: string,
  invitationToken: string
): Promise<AcceptProjectInvitationResult> {
  const response = await fetch(
    `${API_BASE_URL}/api/project-invitations/accept`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        token: invitationToken,
      }),
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (!response.ok) {
    let message =
      "Unable to accept this invitation.";

    try {
      const body =
        (await response.json()) as InvitationError;

      if (body.message) {
        message = body.message;
      }
    } catch {
      // Keep the safe fallback message.
    }

    throw new Error(message);
  }

  return (
    await response.json()
  ) as AcceptProjectInvitationResult;
}

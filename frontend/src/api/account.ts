import type {
  CurrentUser,
} from "./auth";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5144";

type ApiErrorResponse = {
  message?: string;
};

async function getErrorMessage(
  response: Response,
  fallback: string
): Promise<string> {
  try {
    const body =
      (await response.json()) as ApiErrorResponse;

    return body.message ?? fallback;
  } catch {
    return fallback;
  }
}

export async function updateProfile(
  token: string,
  fullName: string
): Promise<CurrentUser> {
  const response = await fetch(
    `${API_BASE_URL}/api/users/me`,
    {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        fullName,
      }),
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (!response.ok) {
    throw new Error(
      await getErrorMessage(
        response,
        "Unable to update your profile."
      )
    );
  }

  return (await response.json()) as CurrentUser;
}

export async function changePassword(
  token: string,
  currentPassword: string,
  newPassword: string
): Promise<void> {
  const response = await fetch(
    `${API_BASE_URL}/api/users/me/password`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        currentPassword,
        newPassword,
      }),
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (!response.ok) {
    throw new Error(
      await getErrorMessage(
        response,
        "Unable to change your password."
      )
    );
  }
}
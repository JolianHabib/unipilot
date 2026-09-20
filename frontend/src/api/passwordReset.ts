const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5144";

type MessageResponse = {
  message: string;
};

export async function forgotPassword(
  email: string
): Promise<string> {
  const response = await fetch(
    `${API_BASE_URL}/api/auth/forgot-password`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        email: email.trim().toLowerCase(),
      }),
    }
  );

  if (!response.ok) {
    throw new Error(
      "Unable to send the password reset email."
    );
  }

  const result =
    (await response.json()) as MessageResponse;

  return result.message;
}

export async function resetPassword(
  email: string,
  token: string,
  newPassword: string
): Promise<void> {
  const response = await fetch(
    `${API_BASE_URL}/api/auth/reset-password`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        email,
        token,
        newPassword,
      }),
    }
  );

  if (response.ok) {
    return;
  }

  let serverMessage: string | null = null;

  try {
    const body =
      (await response.json()) as {
        message?: string;
      };

    serverMessage = body.message ?? null;
  } catch {
    serverMessage = null;
  }

  throw new Error(
    serverMessage ??
      "The reset link is invalid or has expired."
  );
}
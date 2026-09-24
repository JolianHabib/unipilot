const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5144";

export type CurrentUser = {
  id: string;
  fullName: string;
  email: string;
};

export type Course = {
  id: string;
  name: string;
  code: string | null;
  description: string | null;
  createdAtUtc: string;
};

type LoginResponse = {
  accessToken: string;
};
type ApiErrorResponse = {
  message?: string;
};

export async function register(
  fullName: string,
  email: string,
  password: string
): Promise<void> {
  const response = await fetch(
    `${API_BASE_URL}/api/auth/register`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        fullName,
        email,
        password,
      }),
    }
  );

  if (response.ok) {
    return;
  }

  let serverMessage: string | null = null;

  try {
    const errorBody =
      (await response.json()) as ApiErrorResponse;

    serverMessage = errorBody.message ?? null;
  } catch {
    serverMessage = null;
  }

  if (response.status === 409) {
    throw new Error(
      serverMessage ??
        "An account with this email already exists."
    );
  }

  if (response.status === 400) {
    throw new Error(
      serverMessage ??
        "Please check your registration information."
    );
  }

  throw new Error(
    serverMessage ??
      "Unable to create your account. Please try again."
  );
}
export async function login(
  email: string,
  password: string
): Promise<string> {
  const response = await fetch(
    `${API_BASE_URL}/api/auth/login`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        email,
        password,
      }),
    }
  );

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error("Invalid email or password.");
    }

    throw new Error(
      "Unable to sign in. Please try again."
    );
  }

  const data =
    (await response.json()) as LoginResponse;

  return data.accessToken;
}


// Add this function to src/api/auth.ts after the existing login function.
export async function googleLogin(
  credential: string
): Promise<string> {
  const response = await fetch(
    `${API_BASE_URL}/api/auth/google`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ credential }),
    }
  );

  if (!response.ok) {
    let message =
      "Unable to sign in with Google. Please try again.";

    try {
      const body = (await response.json()) as {
        message?: string;
      };

      if (body.message) {
        message = body.message;
      }
    } catch {
      // Keep the fallback message when no JSON body is returned.
    }

    throw new Error(message);
  }

  const data =
    (await response.json()) as LoginResponse;

  return data.accessToken;
}

export async function getCurrentUser(
  token: string
): Promise<CurrentUser> {
  const response = await fetch(
    `${API_BASE_URL}/api/users/me`,
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
    throw new Error("Unable to load your profile.");
  }

  return (await response.json()) as CurrentUser;
}

export async function getCourses(
  token: string
): Promise<Course[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/courses`,
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
    throw new Error("Unable to load courses.");
  }

  return (await response.json()) as Course[];

  
}
export type CreateCourseInput = {
  name: string;
  code: string | null;
  description: string | null;
};

export async function createCourse(
  token: string,
  input: CreateCourseInput
): Promise<Course> {
  const response = await fetch(
    `${API_BASE_URL}/api/courses`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(input),
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (response.status === 400) {
    throw new Error(
      "Please check the course information."
    );
  }

  if (!response.ok) {
    throw new Error(
      "Unable to create the course."
    );
  }

  return (await response.json()) as Course;
}

export type UpdateCourseInput = {
  name: string;
  code: string | null;
  description: string | null;
};

export async function updateCourse(
  token: string,
  courseId: string,
  input: UpdateCourseInput
): Promise<Course> {
  const response = await fetch(
    `${API_BASE_URL}/api/courses/${courseId}`,
    {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(input),
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (response.status === 404) {
    throw new Error("Course not found.");
  }

  if (response.status === 400) {
    throw new Error(
      "Please check the course information."
    );
  }

  if (!response.ok) {
    throw new Error(
      "Unable to update the course."
    );
  }

  return (await response.json()) as Course;
}

export async function deleteCourse(
  token: string,
  courseId: string
): Promise<void> {
  const response = await fetch(
    `${API_BASE_URL}/api/courses/${courseId}`,
    {
      method: "DELETE",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (response.status === 404) {
    throw new Error("Course not found.");
  }

  if (!response.ok) {
    throw new Error(
      "Unable to delete the course."
    );
  }
}
export type AcademicProject = {
  id: string;
  courseId: string;
  title: string;
  description: string | null;
  status: string;
  dueDateUtc: string | null;
  createdAtUtc: string;
  accessRole: "Owner" | "Editor" | "Viewer";
};

export async function getProjectsByCourse(
  token: string,
  courseId: string
): Promise<AcademicProject[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/courses/${courseId}/projects`,
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
    throw new Error("Course not found.");
  }

  if (!response.ok) {
    throw new Error(
      "Unable to load course projects."
    );
  }

  return (await response.json()) as AcademicProject[];
}
export type ProjectStatus =
  | "Draft"
  | "Active"
  | "Completed"
  | "Archived";

export type SaveProjectInput = {
  title: string;
  description: string | null;
  dueDateUtc: string | null;
  status: ProjectStatus;
};

export async function createProject(
  token: string,
  courseId: string,
  input: SaveProjectInput
): Promise<AcademicProject> {
  const response = await fetch(
    `${API_BASE_URL}/api/courses/${courseId}/projects`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(input),
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (response.status === 404) {
    throw new Error("Course not found.");
  }

  if (response.status === 400) {
    throw new Error(
      "Please check the project information."
    );
  }

  if (!response.ok) {
    throw new Error(
      "Unable to create the project."
    );
  }

  return (
    await response.json()
  ) as AcademicProject;
}

export async function updateProject(
  token: string,
  courseId: string,
  projectId: string,
  input: SaveProjectInput
): Promise<AcademicProject> {
  const response = await fetch(
    `${API_BASE_URL}/api/courses/${courseId}/projects/${projectId}`,
    {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(input),
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (response.status === 404) {
    throw new Error("Project not found.");
  }

  if (response.status === 400) {
    throw new Error(
      "Please check the project information."
    );
  }

  if (!response.ok) {
    throw new Error(
      "Unable to update the project."
    );
  }

  return (
    await response.json()
  ) as AcademicProject;
}

export async function deleteProject(
  token: string,
  courseId: string,
  projectId: string
): Promise<void> {
  const response = await fetch(
    `${API_BASE_URL}/api/courses/${courseId}/projects/${projectId}`,
    {
      method: "DELETE",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (response.status === 404) {
    throw new Error("Project not found.");
  }

  if (!response.ok) {
    throw new Error(
      "Unable to delete the project."
    );
  }
}
export type ProjectDocument = {
  id: string;
  academicProjectId: string;
  originalFileName: string;
  documentType: string;
  processingStatus: string;
  fileSizeBytes: number;
  pageCount: number;
  failureReason: string | null;
  uploadedAtUtc: string;
};

export type ProjectRequirement = {
  id: string;
  academicProjectId: string;
  projectDocumentId: string | null;
  sourcePageNumber: number;
  title: string;
  description: string;
  type: string;
  priority: string;
  isCompleted: boolean;
  createdAtUtc: string;
};

export async function getProjectDocuments(
  token: string,
  projectId: string
): Promise<ProjectDocument[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/documents`,
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
    throw new Error("Project not found.");
  }

  if (!response.ok) {
    throw new Error(
      "Unable to load project documents."
    );
  }

  return (await response.json()) as ProjectDocument[];
}

export async function getProjectRequirements(
  token: string,
  projectId: string
): Promise<ProjectRequirement[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/requirements`,
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
    throw new Error("Project not found.");
  }

  if (!response.ok) {
    throw new Error(
      "Unable to load project requirements."
    );
  }

  return (await response.json()) as ProjectRequirement[];
}
export async function uploadProjectDocument(
  token: string,
  projectId: string,
  file: File
): Promise<ProjectDocument> {
  const formData = new FormData();

  formData.append("File", file);
  formData.append(
    "DocumentType",
    "Specification"
  );

  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/documents`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
      },
      body: formData,
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (response.status === 404) {
    throw new Error("Project not found.");
  }

  if (response.status === 409) {
    throw new Error(
      "This PDF has already been uploaded."
    );
  }

  if (response.status === 400) {
    throw new Error(
      "Please select a valid PDF file."
    );
  }

  if (!response.ok) {
    throw new Error(
      "Unable to upload the PDF."
    );
  }

  return (await response.json()) as ProjectDocument;
}

export async function deleteProjectDocument(
  token: string,
  projectId: string,
  documentId: string
): Promise<void> {
  const response = await fetch(
    `${API_BASE_URL}/api/projects/${projectId}/documents/${documentId}`,
    {
      method: "DELETE",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (response.status === 404) {
    throw new Error("Document not found.");
  }

  if (!response.ok) {
    throw new Error(
      "Unable to delete the document."
    );
  }
}
export async function extractDocumentRequirements(
  token: string,
  documentId: string
): Promise<ProjectRequirement[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/documents/${documentId}/requirements/extract`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (response.ok) {
    return (
      await response.json()
    ) as ProjectRequirement[];
  }

  let serverMessage: string | null = null;

  try {
    const errorBody =
      (await response.json()) as {
        message?: string;
      };

    serverMessage =
      errorBody.message ?? null;
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
        "The document is not ready for AI analysis."
    );
  }

  if (response.status === 503) {
    throw new Error(
      serverMessage ??
        "AI service is temporarily unavailable. Please try again."
    );
  }

  throw new Error(
    serverMessage ??
      "Unable to analyze this document."
  );
}
export async function setRequirementCompletion(
  token: string,
  requirementId: string,
  isCompleted: boolean
): Promise<ProjectRequirement> {
  const response = await fetch(
    `${API_BASE_URL}/api/requirements/${requirementId}`,
    {
      method: "PATCH",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        isCompleted,
      }),
    }
  );

  if (response.status === 401) {
    throw new Error("SESSION_EXPIRED");
  }

  if (response.status === 404) {
    throw new Error(
      "Requirement not found."
    );
  }

  if (!response.ok) {
    let message =
      "Unable to update the requirement.";

    try {
      const body = (await response.json()) as {
        message?: string;
      };

      if (body.message) {
        message = body.message;
      }
    } catch {
      // Keep the default message.
    }

    throw new Error(message);
  }

  return (await response.json()) as ProjectRequirement;
}
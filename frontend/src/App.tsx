import { useEffect, useState } from "react";
import {
  createCourse,
  deleteCourse,
  getCourses,
  getCurrentUser,
  updateCourse,
  type AcademicProject,
  type Course,
  type CreateCourseInput,
  type CurrentUser,
  type UpdateCourseInput,
} from "./api/auth";
import { CourseWorkspacePage } from "./pages/CourseWorkspacePage";
import { DashboardPage } from "./pages/DashboardPage";
import { ForgotPasswordPage } from "./pages/ForgotPasswordPage";
import { LoginPage } from "./pages/LoginPage";
import { ProjectInvitationPage } from "./pages/ProjectInvitationPage";
import { ProjectWorkspacePage } from "./pages/ProjectWorkspacePage";
import { ResetPasswordPage } from "./pages/ResetPasswordPage";

function App() {
  const [token, setToken] = useState<string | null>(() =>
    localStorage.getItem("accessToken")
  );
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [courses, setCourses] = useState<Course[]>([]);
  const [selectedCourse, setSelectedCourse] = useState<Course | null>(null);
  const [selectedProject, setSelectedProject] =
    useState<AcademicProject | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function handleLogin(accessToken: string) {
    localStorage.setItem("accessToken", accessToken);
    setToken(accessToken);
  }

  function handleLogout() {
    localStorage.removeItem("accessToken");
    setToken(null);
    setUser(null);
    setCourses([]);
    setSelectedCourse(null);
    setSelectedProject(null);
    setError(null);
  }

  function handleUserUpdated(updatedUser: CurrentUser) {
    setUser(updatedUser);
  }

  function handleCourseSelect(course: Course) {
    setSelectedCourse(course);
    setSelectedProject(null);
  }

  function handleProjectSelect(project: AcademicProject) {
    const projectCourse =
      courses.find((course) => course.id === project.courseId) ?? null;

    if (projectCourse) {
      setSelectedCourse(projectCourse);
    }

    setSelectedProject(project);
  }

  function handleBackToDashboard() {
    setSelectedProject(null);
    setSelectedCourse(null);
  }

  function handleBackToCourse() {
    setSelectedProject(null);
  }

  async function handleCreateCourse(input: CreateCourseInput) {
    if (!token) throw new Error("SESSION_EXPIRED");

    try {
      const createdCourse = await createCourse(token, input);
      setCourses((current) => [createdCourse, ...current]);
    } catch (exception) {
      if (exception instanceof Error && exception.message === "SESSION_EXPIRED") {
        handleLogout();
      }
      throw exception;
    }
  }

  async function handleUpdateCourse(
    courseId: string,
    input: UpdateCourseInput
  ) {
    if (!token) throw new Error("SESSION_EXPIRED");

    try {
      const updated = await updateCourse(token, courseId, input);
      setCourses((current) =>
        current.map((course) => (course.id === courseId ? updated : course))
      );
      setSelectedCourse((current) =>
        current?.id === courseId ? updated : current
      );
    } catch (exception) {
      if (exception instanceof Error && exception.message === "SESSION_EXPIRED") {
        handleLogout();
      }
      throw exception;
    }
  }

  async function handleDeleteCourse(courseId: string) {
    if (!token) throw new Error("SESSION_EXPIRED");

    try {
      await deleteCourse(token, courseId);
      setCourses((current) =>
        current.filter((course) => course.id !== courseId)
      );

      if (selectedCourse?.id === courseId) {
        setSelectedCourse(null);
        setSelectedProject(null);
      }
    } catch (exception) {
      if (exception instanceof Error && exception.message === "SESSION_EXPIRED") {
        handleLogout();
      }
      throw exception;
    }
  }

  useEffect(() => {
    if (!token) return;
    let isCancelled = false;

    async function loadDashboard() {
      setIsLoading(true);
      setError(null);

      try {
        const [currentUser, courseResults] = await Promise.all([
          getCurrentUser(token as string),
          getCourses(token as string),
        ]);

        if (!isCancelled) {
          setUser(currentUser);
          setCourses(courseResults);
        }
      } catch (exception) {
        if (isCancelled) return;
        const message =
          exception instanceof Error
            ? exception.message
            : "Unable to load the dashboard.";

        if (message === "SESSION_EXPIRED") {
          handleLogout();
          return;
        }
        setError(message);
      } finally {
        if (!isCancelled) setIsLoading(false);
      }
    }

    void loadDashboard();
    return () => {
      isCancelled = true;
    };
  }, [token]);

  const currentPath = window.location.pathname;
  const invitationToken = new URLSearchParams(
    window.location.search
  ).get("token");

  if (currentPath === "/forgot-password") {
    return <ForgotPasswordPage />;
  }

  if (currentPath === "/reset-password") {
    return <ResetPasswordPage />;
  }

  if (!token) return <LoginPage onLogin={handleLogin} />;

  if (isLoading) {
    return (
      <main className="dashboard-loading">
        <div><div className="loading-spinner" /><p>Preparing your workspace...</p></div>
      </main>
    );
  }

  if (error || !user) {
    return (
      <main className="dashboard-error">
        <div>
          <p className="eyebrow">Something went wrong</p>
          <h1>Unable to load UniPilot</h1>
          <p>{error ?? "Your user profile could not be loaded."}</p>
          <button className="secondary-button" type="button" onClick={handleLogout}>
            Return to sign in
          </button>
        </div>
      </main>
    );
  }

  if (currentPath === "/invitations/accept") {
    return (
      <ProjectInvitationPage
        token={token}
        invitationToken={invitationToken}
        onSessionExpired={handleLogout}
      />
    );
  }

  if (selectedProject) {
    return (
      <ProjectWorkspacePage
        token={token}
        project={selectedProject}
        onBack={handleBackToCourse}
        onSessionExpired={handleLogout}
      />
    );
  }

  if (selectedCourse) {
    return (
      <CourseWorkspacePage
        token={token}
        course={selectedCourse}
        onProjectSelect={handleProjectSelect}
        onBack={handleBackToDashboard}
        onSessionExpired={handleLogout}
      />
    );
  }

  return (
    <DashboardPage
      token={token}
      user={user}
      courses={courses}
      onCourseSelect={handleCourseSelect}
      onProjectSelect={handleProjectSelect}
      onCreateCourse={handleCreateCourse}
      onUpdateCourse={handleUpdateCourse}
      onDeleteCourse={handleDeleteCourse}
      onUserUpdated={handleUserUpdated}
      onLogout={handleLogout}
    />
  );
}

export default App;

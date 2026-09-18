import {
  useEffect,
  useState,
} from "react";

import {
  createCourse,
  getCourses,
  getCurrentUser,
  type AcademicProject,
  type Course,
  type CreateCourseInput,
  type CurrentUser,
} from "./api/auth";

import {
  CourseWorkspacePage,
} from "./pages/CourseWorkspacePage";

import {
  DashboardPage,
} from "./pages/DashboardPage";

import {
  LoginPage,
} from "./pages/LoginPage";

import {
  ProjectWorkspacePage,
} from "./pages/ProjectWorkspacePage";

function App() {
  const [token, setToken] = useState<string | null>(
    () => localStorage.getItem("accessToken")
  );

  const [user, setUser] =
    useState<CurrentUser | null>(null);

  const [courses, setCourses] =
    useState<Course[]>([]);

  const [selectedCourse, setSelectedCourse] =
    useState<Course | null>(null);

  const [selectedProject, setSelectedProject] =
    useState<AcademicProject | null>(null);

  const [isLoading, setIsLoading] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  function handleLogin(accessToken: string) {
    localStorage.setItem(
      "accessToken",
      accessToken
    );

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

  function handleCourseSelect(course: Course) {
    setSelectedCourse(course);
    setSelectedProject(null);
  }

  function handleProjectSelect(
    project: AcademicProject
  ) {
    setSelectedProject(project);
  }

  function handleBackToDashboard() {
    setSelectedProject(null);
    setSelectedCourse(null);
  }

  function handleBackToCourse() {
    setSelectedProject(null);
  }

  async function handleCreateCourse(
    input: CreateCourseInput
  ) {
    if (!token) {
      throw new Error("SESSION_EXPIRED");
    }

    try {
      const createdCourse =
        await createCourse(token, input);

      setCourses((currentCourses) => [
        createdCourse,
        ...currentCourses,
      ]);
    } catch (createError) {
      if (
        createError instanceof Error &&
        createError.message === "SESSION_EXPIRED"
      ) {
        handleLogout();
      }

      throw createError;
    }
  }

  useEffect(() => {
    if (!token) {
      return;
    }

    let isCancelled = false;

    async function loadDashboard() {
      setIsLoading(true);
      setError(null);

      try {
        const [currentUser, courseResults] =
          await Promise.all([
            getCurrentUser(token as string),
            getCourses(token as string),
          ]);

        if (isCancelled) {
          return;
        }

        setUser(currentUser);
        setCourses(courseResults);
      } catch (loadError) {
        if (isCancelled) {
          return;
        }

        const message =
          loadError instanceof Error
            ? loadError.message
            : "Unable to load the dashboard.";

        if (message === "SESSION_EXPIRED") {
          handleLogout();
          return;
        }

        setError(message);
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadDashboard();

    return () => {
      isCancelled = true;
    };
  }, [token]);

  if (!token) {
    return (
      <LoginPage onLogin={handleLogin} />
    );
  }

  if (isLoading) {
    return (
      <main className="dashboard-loading">
        <div>
          <div className="loading-spinner" />

          <p>Preparing your workspace...</p>
        </div>
      </main>
    );
  }

  if (error || !user) {
    return (
      <main className="dashboard-error">
        <div>
          <p className="eyebrow">
            Something went wrong
          </p>

          <h1>Unable to load UniPilot</h1>

          <p>
            {error ??
              "Your user profile could not be loaded."}
          </p>

          <button
            className="secondary-button"
            type="button"
            onClick={handleLogout}
          >
            Return to sign in
          </button>
        </div>
      </main>
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
      user={user}
      courses={courses}
      onCourseSelect={handleCourseSelect}
      onCreateCourse={handleCreateCourse}
      onLogout={handleLogout}
    />
  );
}

export default App;
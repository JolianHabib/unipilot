import {
  useEffect,
  useState,
} from "react";

import {
  ArrowLeft,
  BookOpen,
  CalendarDays,
  ChevronRight,
  FolderKanban,
  Plus,
} from "lucide-react";

import {
  getProjectsByCourse,
  type AcademicProject,
  type Course,
} from "../api/auth";

type CourseWorkspacePageProps = {
  token: string;
  course: Course;
  onProjectSelect: (
    project: AcademicProject
  ) => void;
  onBack: () => void;
  onSessionExpired: () => void;
};

export function CourseWorkspacePage({
  token,
  course,
  onProjectSelect,
  onBack,
  onSessionExpired,
}: CourseWorkspacePageProps) {
  const [projects, setProjects] = useState<
    AcademicProject[]
  >([]);

  const [isLoading, setIsLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    let isCancelled = false;

    async function loadProjects() {
      setIsLoading(true);
      setError(null);

      try {
        const results =
          await getProjectsByCourse(
            token,
            course.id
          );

        if (!isCancelled) {
          setProjects(results);
        }
      } catch (loadError) {
        if (isCancelled) {
          return;
        }

        const message =
          loadError instanceof Error
            ? loadError.message
            : "Unable to load projects.";

        if (message === "SESSION_EXPIRED") {
          onSessionExpired();
          return;
        }

        setError(message);
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadProjects();

    return () => {
      isCancelled = true;
    };
  }, [
    token,
    course.id,
    onSessionExpired,
  ]);

  function formatDate(
    value: string | null
  ): string {
    if (!value) {
      return "No due date";
    }

    return new Intl.DateTimeFormat(
      "en",
      {
        day: "numeric",
        month: "short",
        year: "numeric",
      }
    ).format(new Date(value));
  }

  return (
    <main className="course-workspace">
      <header className="course-workspace-topbar">
        <button
          className="workspace-back-button"
          type="button"
          onClick={onBack}
        >
          <ArrowLeft size={18} />
          Dashboard
        </button>

        <div className="workspace-brand">
          <BookOpen size={18} />
          <span>UniPilot</span>
        </div>
      </header>

      <div className="course-workspace-content">
        <section className="course-hero">
          <div className="course-hero-icon">
            <BookOpen size={25} />
          </div>

          <div className="course-hero-text">
            {course.code && (
              <span>{course.code}</span>
            )}

            <h1>{course.name}</h1>

            <p>
              {course.description ??
                "No course description has been added."}
            </p>
          </div>

          <button
            className="new-project-button"
            type="button"
          >
            <Plus size={18} />
            New project
          </button>
        </section>

        <section className="projects-section">
          <div className="projects-heading">
            <div>
              <h2>Course projects</h2>

              <p>
                Manage projects, documents, and
                extracted requirements.
              </p>
            </div>

            {!isLoading && !error && (
              <span className="project-count">
                {projects.length}{" "}
                {projects.length === 1
                  ? "project"
                  : "projects"}
              </span>
            )}
          </div>

          {isLoading && (
            <div className="workspace-message">
              <div className="loading-spinner" />
              <p>Loading projects...</p>
            </div>
          )}

          {!isLoading && error && (
            <div className="workspace-message error">
              <h3>Unable to load projects</h3>
              <p>{error}</p>
            </div>
          )}

          {!isLoading &&
            !error &&
            projects.length === 0 && (
              <div className="empty-projects">
                <div className="empty-projects-icon">
                  <FolderKanban size={28} />
                </div>

                <h3>Create your first project</h3>

                <p>
                  Projects keep documents,
                  requirements, and tasks organized.
                </p>

                <button
                  className="new-project-button"
                  type="button"
                >
                  <Plus size={18} />
                  New project
                </button>
              </div>
            )}

          {!isLoading &&
            !error &&
            projects.length > 0 && (
              <div className="projects-grid">
                {projects.map((project) => (
                  <button
                    className="project-card"
                    type="button"
                    key={project.id}
                    onClick={() =>
                      onProjectSelect(project)
                    }
                  >
                    <div className="project-card-header">
                      <div className="project-folder-icon">
                        <FolderKanban size={21} />
                      </div>

                      <span
                        className={`project-status status-${project.status.toLowerCase()}`}
                      >
                        {project.status}
                      </span>
                    </div>

                    <div className="project-card-body">
                      <h3>{project.title}</h3>

                      <p>
                        {project.description ??
                          "No description added yet."}
                      </p>
                    </div>

                    <div className="project-card-footer">
                      <span>
                        <CalendarDays size={15} />

                        {formatDate(
                          project.dueDateUtc
                        )}
                      </span>

                      <ChevronRight size={18} />
                    </div>
                  </button>
                ))}
              </div>
            )}
        </section>
      </div>
    </main>
  );
}
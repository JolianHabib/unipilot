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
  LoaderCircle,
  Pencil,
  Plus,
  Trash2,
  X,
} from "lucide-react";

import {
  createProject,
  deleteProject,
  getProjectsByCourse,
  updateProject,
  type AcademicProject,
  type Course,
  type SaveProjectInput,
} from "../api/auth";

import {
  ProjectModal,
} from "../components/ProjectModal";

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
  const [projects, setProjects] =
    useState<AcademicProject[]>([]);

  const [isLoading, setIsLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(null);

  const [
    isCreateProjectOpen,
    setIsCreateProjectOpen,
  ] = useState(false);

  const [
    editingProject,
    setEditingProject,
  ] = useState<AcademicProject | null>(
    null
  );

  const [
    deletingProject,
    setDeletingProject,
  ] = useState<AcademicProject | null>(
    null
  );

  const [isDeleting, setIsDeleting] =
    useState(false);

  const [
    deleteError,
    setDeleteError,
  ] = useState<string | null>(null);

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

        if (
          message === "SESSION_EXPIRED"
        ) {
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
        timeZone: "UTC",
      }
    ).format(new Date(value));
  }

  function handleSessionError(
    requestError: unknown
  ) {
    if (
      requestError instanceof Error &&
      requestError.message ===
        "SESSION_EXPIRED"
    ) {
      onSessionExpired();
    }
  }

  function openCreateProject() {
    setEditingProject(null);
    setIsCreateProjectOpen(true);
  }

  function openEditProject(
    project: AcademicProject
  ) {
    setIsCreateProjectOpen(false);
    setEditingProject(project);
  }

  function closeProjectModal() {
    setIsCreateProjectOpen(false);
    setEditingProject(null);
  }

  async function handleCreateProject(
    input: SaveProjectInput
  ) {
    try {
      const project =
        await createProject(
          token,
          course.id,
          input
        );

      setProjects(
        (currentProjects) => [
          project,
          ...currentProjects,
        ]
      );
    } catch (requestError) {
      handleSessionError(requestError);
      throw requestError;
    }
  }

  async function handleUpdateProject(
    projectId: string,
    input: SaveProjectInput
  ) {
    try {
      const updatedProject =
        await updateProject(
          token,
          course.id,
          projectId,
          input
        );

      setProjects((currentProjects) =>
        currentProjects.map((project) =>
          project.id === projectId
            ? updatedProject
            : project
        )
      );
    } catch (requestError) {
      handleSessionError(requestError);
      throw requestError;
    }
  }

  function openDeleteProject(
    project: AcademicProject
  ) {
    setDeletingProject(project);
    setDeleteError(null);
  }

  function closeDeleteProject() {
    if (isDeleting) {
      return;
    }

    setDeletingProject(null);
    setDeleteError(null);
  }

  async function confirmDeleteProject() {
    if (!deletingProject) {
      return;
    }

    setIsDeleting(true);
    setDeleteError(null);

    try {
      await deleteProject(
        token,
        course.id,
        deletingProject.id
      );

      setProjects((currentProjects) =>
        currentProjects.filter(
          (project) =>
            project.id !==
            deletingProject.id
        )
      );

      setDeletingProject(null);
    } catch (requestError) {
      handleSessionError(requestError);

      setDeleteError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to delete the project."
      );
    } finally {
      setIsDeleting(false);
    }
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
            onClick={openCreateProject}
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
                Manage projects, documents,
                and extracted requirements.
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
              <h3>
                Unable to load projects
              </h3>

              <p>{error}</p>
            </div>
          )}

          {!isLoading &&
            !error &&
            projects.length === 0 && (
              <div className="empty-projects">
                <div className="empty-projects-icon">
                  <FolderKanban
                    size={28}
                  />
                </div>

                <h3>
                  Create your first project
                </h3>

                <p>
                  Projects keep documents,
                  requirements, and tasks
                  organized.
                </p>

                <button
                  className="new-project-button"
                  type="button"
                  onClick={openCreateProject}
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
                  <article
                    className="project-card"
                    key={project.id}
                    role="button"
                    tabIndex={0}
                    onClick={() =>
                      onProjectSelect(
                        project
                      )
                    }
                    onKeyDown={(event) => {
                      if (
                        event.target !==
                        event.currentTarget
                      ) {
                        return;
                      }

                      if (
                        event.key ===
                          "Enter" ||
                        event.key === " "
                      ) {
                        event.preventDefault();

                        onProjectSelect(
                          project
                        );
                      }
                    }}
                  >
                    <div className="project-card-header">
                      <div className="project-folder-icon">
                        <FolderKanban
                          size={21}
                        />
                      </div>

                      <div className="project-header-right">
                        <span
                          className={`project-status status-${project.status.toLowerCase()}`}
                        >
                          {project.status}
                        </span>

                        <div className="project-card-actions">
                          <button
                            className="project-action-button"
                            type="button"
                            title="Edit project"
                            aria-label={`Edit ${project.title}`}
                            onClick={(
                              event
                            ) => {
                              event.stopPropagation();

                              openEditProject(
                                project
                              );
                            }}
                          >
                            <Pencil
                              size={16}
                            />
                          </button>

                          <button
                            className="project-action-button delete"
                            type="button"
                            title="Delete project"
                            aria-label={`Delete ${project.title}`}
                            onClick={(
                              event
                            ) => {
                              event.stopPropagation();

                              openDeleteProject(
                                project
                              );
                            }}
                          >
                            <Trash2
                              size={16}
                            />
                          </button>
                        </div>
                      </div>
                    </div>

                    <div className="project-card-body">
                      <h3>
                        {project.title}
                      </h3>

                      <p>
                        {project.description ??
                          "No description added yet."}
                      </p>
                    </div>

                    <div className="project-card-footer">
                      <span>
                        <CalendarDays
                          size={15}
                        />

                        {formatDate(
                          project.dueDateUtc
                        )}
                      </span>

                      <ChevronRight
                        size={18}
                      />
                    </div>
                  </article>
                ))}
              </div>
            )}
        </section>
      </div>

      <ProjectModal
        isOpen={
          isCreateProjectOpen ||
          editingProject !== null
        }
        project={editingProject}
        onClose={closeProjectModal}
        onSave={(input) =>
          editingProject
            ? handleUpdateProject(
                editingProject.id,
                input
              )
            : handleCreateProject(input)
        }
      />

      {deletingProject && (
        <div
          className="modal-backdrop"
          onMouseDown={(event) => {
            if (
              event.target ===
              event.currentTarget
            ) {
              closeDeleteProject();
            }
          }}
        >
          <section
            className="delete-course-modal"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="delete-project-title"
          >
            <button
              className="modal-close-button delete-modal-close"
              type="button"
              onClick={closeDeleteProject}
              disabled={isDeleting}
              aria-label="Close"
            >
              <X size={20} />
            </button>

            <div className="delete-modal-icon">
              <Trash2 size={25} />
            </div>

            <h2 id="delete-project-title">
              Delete project?
            </h2>

            <p>
              <strong>
                {deletingProject.title}
              </strong>{" "}
              and all its PDFs,
              requirements, and tasks will
              be permanently deleted.
            </p>

            {deleteError && (
              <p
                className="modal-error"
                role="alert"
              >
                {deleteError}
              </p>
            )}

            <div className="delete-modal-actions">
              <button
                className="cancel-button"
                type="button"
                onClick={closeDeleteProject}
                disabled={isDeleting}
              >
                Cancel
              </button>

              <button
                className="confirm-delete-button"
                type="button"
                onClick={() =>
                  void confirmDeleteProject()
                }
                disabled={isDeleting}
              >
                {isDeleting ? (
                  <>
                    <LoaderCircle
                      className="button-spinner"
                      size={17}
                    />
                    Deleting...
                  </>
                ) : (
                  <>
                    <Trash2 size={17} />
                    Delete project
                  </>
                )}
              </button>
            </div>
          </section>
        </div>
      )}
    </main>
  );
}
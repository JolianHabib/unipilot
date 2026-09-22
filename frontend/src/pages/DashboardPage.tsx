import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
  type ReactNode,
} from "react";

import {
  BookOpen,
  CheckCircle2,
  ChevronRight,
  FolderKanban,
  LayoutDashboard,
  LogOut,
  Pencil,
  Plus,
  Search,
  Settings,
  Sparkles,
  Trash2,
  X,
} from "lucide-react";

import {
  CreateCourseModal,
} from "../components/CreateCourseModal";

import {
  NotificationMenu,
} from "../components/NotificationMenu";

import {
  ProfileModal,
} from "../components/ProfileModal";

import {
  getWorkspaceProjects,
  getWorkspaceRequirements,
} from "../api/workspace";

import {
  ProjectStatusFilter,
  type ProjectStatusFilterValue,
} from "../components/ProjectStatusFilter";

import type {
  AcademicProject,
  Course,
  CreateCourseInput,
  CurrentUser,
  ProjectRequirement,
  UpdateCourseInput,
} from "../api/auth";

type Section =
  | "overview"
  | "courses"
  | "projects"
  | "requirements";

type DashboardPageProps = {
  token: string;
  user: CurrentUser;
  courses: Course[];
  onCourseSelect: (course: Course) => void;
  onProjectSelect: (
    project: AcademicProject
  ) => void;
  onCreateCourse: (
    input: CreateCourseInput
  ) => Promise<void>;
  onUpdateCourse: (
    courseId: string,
    input: UpdateCourseInput
  ) => Promise<void>;
  onDeleteCourse: (
    courseId: string
  ) => Promise<void>;
  onUserUpdated: (
    user: CurrentUser
  ) => void;
  onLogout: () => void;
};

export function DashboardPage({
  token,
  user,
  courses,
  onCourseSelect,
  onProjectSelect,
  onCreateCourse,
  onUpdateCourse,
  onDeleteCourse,
  onUserUpdated,
  onLogout,
}: DashboardPageProps) {
  const [section, setSection] =
    useState<Section>("overview");

  const [search, setSearch] =
    useState("");

  const [
    projectStatusFilter,
    setProjectStatusFilter,
  ] = useState<ProjectStatusFilterValue>("All");

  const [projects, setProjects] =
    useState<AcademicProject[]>([]);

  const [requirements, setRequirements] =
    useState<ProjectRequirement[]>([]);

  const [isLoading, setIsLoading] =
    useState(true);

  const [loadError, setLoadError] =
    useState<string | null>(null);

  const [isCreateOpen, setIsCreateOpen] =
    useState(false);

  const [editingCourse, setEditingCourse] =
    useState<Course | null>(null);

  const [deletingCourse, setDeletingCourse] =
    useState<Course | null>(null);

  const [isProfileOpen, setIsProfileOpen] =
    useState(false);

  useEffect(() => {
    let cancelled = false;

    async function loadWorkspace() {
      setIsLoading(true);
      setLoadError(null);

      try {
        const [
          projectResults,
          requirementResults,
        ] = await Promise.all([
          getWorkspaceProjects(token),
          getWorkspaceRequirements(token),
        ]);

        if (!cancelled) {
          setProjects(projectResults);
          setRequirements(requirementResults);
        }
      } catch (exception) {
        if (cancelled) {
          return;
        }

        const message =
          exception instanceof Error
            ? exception.message
            : "Unable to load the workspace.";

        if (message === "SESSION_EXPIRED") {
          onLogout();
          return;
        }

        setLoadError(message);
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadWorkspace();

    return () => {
      cancelled = true;
    };
  }, [token, onLogout]);

  const query =
    search.trim().toLowerCase();

  const isSearching =
    query.length > 0;

  const courseById = useMemo(
    () =>
      new Map(
        courses.map((course) => [
          course.id,
          course,
        ])
      ),
    [courses]
  );

  const projectById = useMemo(
    () =>
      new Map(
        projects.map((project) => [
          project.id,
          project,
        ])
      ),
    [projects]
  );

  const filteredCourses = courses.filter(
    (course) =>
      [
        course.name,
        course.code,
        course.description,
      ].some((value) =>
        value
          ?.toLowerCase()
          .includes(query)
      )
  );

  const filteredProjects = projects.filter(
    (project) => {
      const course = courseById.get(
        project.courseId
      );

      return [
        project.title,
        project.description,
        project.status,
        course?.name,
        course?.code,
      ].some((value) =>
        value
          ?.toLowerCase()
          .includes(query)
      );
    }
  );
  const statusProjects =
    projectStatusFilter === "All"
      ? projects
      : projects.filter(
          (project) =>
            project.status ===
            projectStatusFilter
        );

  const filteredRequirements =
    requirements.filter((requirement) => {
      const project = projectById.get(
        requirement.academicProjectId
      );

      const course = project
        ? courseById.get(project.courseId)
        : undefined;

      return [
        requirement.title,
        requirement.description,
        requirement.type,
        requirement.priority,
        project?.title,
        course?.name,
        course?.code,
      ].some((value) =>
        value
          ?.toLowerCase()
          .includes(query)
      );
    });

  const completedCount =
    requirements.filter(
      (item) => item.isCompleted
    ).length;

  const initials = user.fullName
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) =>
      part[0].toUpperCase()
    )
    .join("");

  const firstName =
    user.fullName
      .trim()
      .split(/\s+/)[0] || "there";

  function changeSection(
    next: Section
  ) {
    setSection(next);
    setSearch("");
  }

  return (
    <div className="dashboard-layout">
      <aside className="dashboard-sidebar">
        <div className="dashboard-brand">
          <span className="dashboard-brand-icon">
            <Sparkles size={20} />
          </span>

          <span>UniPilot</span>
        </div>

        <nav className="dashboard-navigation">
          <p className="navigation-label">
            Workspace
          </p>

          <NavButton
            active={section === "overview"}
            onClick={() =>
              changeSection("overview")
            }
            icon={
              <LayoutDashboard size={18} />
            }
            label="Overview"
          />

          <NavButton
            active={section === "courses"}
            onClick={() =>
              changeSection("courses")
            }
            icon={<BookOpen size={18} />}
            label="Courses"
          />

          <NavButton
            active={section === "projects"}
            onClick={() =>
              changeSection("projects")
            }
            icon={
              <FolderKanban size={18} />
            }
            label="Projects"
          />

          <NavButton
            active={
              section === "requirements"
            }
            onClick={() =>
              changeSection("requirements")
            }
            icon={<Sparkles size={18} />}
            label="AI requirements"
          />
        </nav>

        <div className="sidebar-profile">
          <div className="profile-avatar">
            {initials}
          </div>

          <button
            className="profile-details profile-settings-button"
            type="button"
            onClick={() =>
              setIsProfileOpen(true)
            }
          >
            <strong>{user.fullName}</strong>
            <span>{user.email}</span>
            <Settings size={15} />
          </button>

          <button
            className="sidebar-logout"
            type="button"
            onClick={onLogout}
            aria-label="Sign out"
          >
            <LogOut size={18} />
          </button>
        </div>
      </aside>

      <main className="dashboard-main">
        <header className="dashboard-topbar">
          <div className="dashboard-search">
            <Search size={18} />

            <input
              type="search"
              value={search}
              onChange={(event) =>
                setSearch(event.target.value)
              }
              placeholder="Search courses, projects, and requirements..."
              aria-label="Search workspace"
            />
          </div>

          <NotificationMenu
            token={token}
            onSessionExpired={onLogout}
            onOpenProject={(projectId) => {
              const project = projects.find(
                (item) =>
                  item.id === projectId
              );

              if (project) {
                onProjectSelect(project);
              }
            }}
          />
        </header>

        <div className="dashboard-content">
          {isSearching && (
            <GlobalSearchResults
              courses={filteredCourses}
              projects={filteredProjects}
              requirements={
                filteredRequirements
              }
              courseById={courseById}
              projectById={projectById}
              onCourseSelect={
                onCourseSelect
              }
              onProjectSelect={
                onProjectSelect
              }
            />
          )}

          {!isSearching &&
            section === "overview" && (
              <>
                <section className="dashboard-heading">
                  <div>
                    <p className="dashboard-eyebrow">
                      Student workspace
                    </p>

                    <h1>
                      Welcome back, {firstName}
                    </h1>

                    <p>
                      Manage your courses,
                      projects, and AI-extracted
                      requirements.
                    </p>
                  </div>

                  <button
                    className="new-course-button"
                    type="button"
                    onClick={() =>
                      setIsCreateOpen(true)
                    }
                  >
                    <Plus size={18} />
                    New course
                  </button>
                </section>

                <section className="dashboard-stats">
                  <Stat
                    icon={
                      <BookOpen size={21} />
                    }
                    color="purple"
                    label="Total courses"
                    value={String(
                      courses.length
                    )}
                  />

                  <Stat
                    icon={
                      <FolderKanban
                        size={21}
                      />
                    }
                    color="blue"
                    label="Projects"
                    value={String(
                      projects.length
                    )}
                  />

                  <Stat
                    icon={
                      <CheckCircle2
                        size={21}
                      />
                    }
                    color="green"
                    label="Completed requirements"
                    value={`${completedCount}/${requirements.length}`}
                  />
                </section>

                <CourseSection
                  courses={courses}
                  onOpen={onCourseSelect}
                  onEdit={setEditingCourse}
                  onDelete={setDeletingCourse}
                  onCreate={() =>
                    setIsCreateOpen(true)
                  }
                  onViewAll={() =>
                    changeSection("courses")
                  }
                />
              </>
            )}

          {!isSearching &&
            section === "courses" && (
              <>
                <PageHeading
                  eyebrow="Workspace"
                  title="Courses"
                  text="Manage all your courses."
                  action={
                    <button
                      className="new-course-button"
                      type="button"
                      onClick={() =>
                        setIsCreateOpen(true)
                      }
                    >
                      <Plus size={18} />
                      New course
                    </button>
                  }
                />

                <CourseGrid
                  courses={courses}
                  onOpen={onCourseSelect}
                  onEdit={setEditingCourse}
                  onDelete={setDeletingCourse}
                />
              </>
            )}

          {!isSearching &&
            section === "projects" && (
              <>
                <PageHeading
                  eyebrow="Workspace"
                  title="Projects"
                  text="Open any project across your courses."
                />

                <ProjectStatusFilter
                  projects={projects}
                  value={projectStatusFilter}
                  onChange={
                    setProjectStatusFilter
                  }
                />

                <WorkspaceState
                  loading={isLoading}
                  error={loadError}
                  empty={!statusProjects.length}
                  emptyText="No projects found."
                />

                {!isLoading &&
                  !loadError && (
                    <div className="projects-grid">
                      {statusProjects.map(
                        (project) => (
                          <button
                            className="project-card"
                            type="button"
                            key={project.id}
                            onClick={() =>
                              onProjectSelect(
                                project
                              )
                            }
                          >
                            <div className="project-card-header">
                              <div className="project-folder-icon">
                                <FolderKanban
                                  size={21}
                                />
                              </div>

                              <span
                                className={`project-status status-${project.status.toLowerCase()}`}
                              >
                                {project.status}
                              </span>
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
                                <BookOpen
                                  size={15}
                                />

                                {courseById.get(
                                  project.courseId
                                )?.name ??
                                  "Course"}
                              </span>

                              <ChevronRight
                                size={18}
                              />
                            </div>
                          </button>
                        )
                      )}
                    </div>
                  )}
              </>
            )}

          {!isSearching &&
            section === "requirements" && (
              <>
                <PageHeading
                  eyebrow="AI workspace"
                  title="AI requirements"
                  text="Review all extracted requirements in one place."
                />

                <WorkspaceState
                  loading={isLoading}
                  error={loadError}
                  empty={!requirements.length}
                  emptyText="No requirements found."
                />

                {!isLoading &&
                  !loadError && (
                    <div className="requirements-list">
                      {requirements.map(
                        (requirement) => (
                          <article
                            className={`requirement-row ${
                              requirement.isCompleted
                                ? "completed"
                                : ""
                            }`}
                            key={requirement.id}
                          >
                            <div className="requirement-completion">
                              <CheckCircle2
                                size={21}
                              />
                            </div>

                            <div className="requirement-content">
                              <h3>
                                {
                                  requirement.title
                                }
                              </h3>

                              <p>
                                {
                                  requirement.description
                                }
                              </p>

                              <div className="requirement-meta">
                                <span>
                                  {
                                    requirement.type
                                  }
                                </span>

                                <span>
                                  {
                                    requirement.priority
                                  }
                                </span>

                                <span>
                                  {projectById.get(
                                    requirement.academicProjectId
                                  )?.title ??
                                    "Project"}
                                </span>
                              </div>
                            </div>
                          </article>
                        )
                      )}
                    </div>
                  )}
              </>
            )}
        </div>
      </main>

      <CreateCourseModal
        isOpen={isCreateOpen}
        onClose={() =>
          setIsCreateOpen(false)
        }
        onCreate={onCreateCourse}
      />

      {editingCourse && (
        <EditCourseModal
          course={editingCourse}
          onClose={() =>
            setEditingCourse(null)
          }
          onSave={onUpdateCourse}
        />
      )}

      {deletingCourse && (
        <DeleteCourseModal
          course={deletingCourse}
          onClose={() =>
            setDeletingCourse(null)
          }
          onDelete={onDeleteCourse}
        />
      )}

      <ProfileModal
        isOpen={isProfileOpen}
        token={token}
        user={user}
        onClose={() =>
          setIsProfileOpen(false)
        }
        onUserUpdated={onUserUpdated}
        onSessionExpired={onLogout}
      />
    </div>
  );
}

type GlobalSearchResultsProps = {
  courses: Course[];
  projects: AcademicProject[];
  requirements: ProjectRequirement[];
  courseById: Map<string, Course>;
  projectById: Map<
    string,
    AcademicProject
  >;
  onCourseSelect: (course: Course) => void;
  onProjectSelect: (
    project: AcademicProject
  ) => void;
};

function GlobalSearchResults({
  courses,
  projects,
  requirements,
  courseById,
  projectById,
  onCourseSelect,
  onProjectSelect,
}: GlobalSearchResultsProps) {
  const resultCount =
    courses.length +
    projects.length +
    requirements.length;

  return (
    <section className="global-search-results">
      <div className="search-results-heading">
        <p className="dashboard-eyebrow">
          Workspace search
        </p>

        <h1>Search results</h1>

        <p>
          {resultCount === 0
            ? "No matching results found."
            : `${resultCount} matching result${
                resultCount === 1 ? "" : "s"
              } found.`}
        </p>
      </div>

      {resultCount === 0 && (
        <div className="workspace-message">
          <Search size={28} />
          <h3>No results found</h3>
          <p>
            Try another name, code, status,
            type, or priority.
          </p>
        </div>
      )}

      {courses.length > 0 && (
        <SearchResultGroup title="Courses">
          {courses.map((course) => (
            <button
              className="search-result-item"
              type="button"
              key={course.id}
              onClick={() =>
                onCourseSelect(course)
              }
            >
              <span className="search-result-icon purple">
                <BookOpen size={19} />
              </span>

              <span className="search-result-content">
                <strong>{course.name}</strong>
                <span>
                  {course.code ||
                    "Course workspace"}
                </span>
              </span>

              <ChevronRight size={18} />
            </button>
          ))}
        </SearchResultGroup>
      )}

      {projects.length > 0 && (
        <SearchResultGroup title="Projects">
          {projects.map((project) => (
            <button
              className="search-result-item"
              type="button"
              key={project.id}
              onClick={() =>
                onProjectSelect(project)
              }
            >
              <span className="search-result-icon blue">
                <FolderKanban size={19} />
              </span>

              <span className="search-result-content">
                <strong>{project.title}</strong>

                <span>
                  {courseById.get(
                    project.courseId
                  )?.name ?? "Course"}
                  {" · "}
                  {project.status}
                </span>
              </span>

              <ChevronRight size={18} />
            </button>
          ))}
        </SearchResultGroup>
      )}

      {requirements.length > 0 && (
        <SearchResultGroup title="AI requirements">
          {requirements.map(
            (requirement) => {
              const project =
                projectById.get(
                  requirement.academicProjectId
                );

              return (
                <button
                  className="search-result-item"
                  type="button"
                  key={requirement.id}
                  disabled={!project}
                  onClick={() => {
                    if (project) {
                      onProjectSelect(project);
                    }
                  }}
                >
                  <span className="search-result-icon green">
                    <Sparkles size={19} />
                  </span>

                  <span className="search-result-content">
                    <strong>
                      {requirement.title}
                    </strong>

                    <span>
                      {project?.title ??
                        "Project"}
                      {" · "}
                      {requirement.priority}
                    </span>
                  </span>

                  <ChevronRight size={18} />
                </button>
              );
            }
          )}
        </SearchResultGroup>
      )}
    </section>
  );
}

function SearchResultGroup({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <div className="search-result-group">
      <h2>{title}</h2>

      <div className="search-result-list">
        {children}
      </div>
    </div>
  );
}

function NavButton({
  active,
  onClick,
  icon,
  label,
}: {
  active: boolean;
  onClick: () => void;
  icon: ReactNode;
  label: string;
}) {
  return (
    <button
      className={`navigation-item ${
        active ? "active" : ""
      }`}
      type="button"
      onClick={onClick}
    >
      {icon}
      <span>{label}</span>
    </button>
  );
}

function Stat({
  icon,
  color,
  label,
  value,
}: {
  icon: ReactNode;
  color: string;
  label: string;
  value: string;
}) {
  return (
    <article className="stat-card">
      <div className={`stat-icon ${color}`}>
        {icon}
      </div>

      <div>
        <span>{label}</span>
        <strong>{value}</strong>
      </div>
    </article>
  );
}

function PageHeading({
  eyebrow,
  title,
  text,
  action,
}: {
  eyebrow: string;
  title: string;
  text: string;
  action?: ReactNode;
}) {
  return (
    <section className="dashboard-heading">
      <div>
        <p className="dashboard-eyebrow">
          {eyebrow}
        </p>

        <h1>{title}</h1>
        <p>{text}</p>
      </div>

      {action}
    </section>
  );
}

type CourseGridProps = {
  courses: Course[];
  onOpen: (course: Course) => void;
  onEdit: (course: Course) => void;
  onDelete: (course: Course) => void;
};

function CourseSection(
  props: CourseGridProps & {
    onCreate: () => void;
    onViewAll: () => void;
  }
) {
  return (
    <section className="courses-section">
      <div className="section-heading">
        <div>
          <h2>Your courses</h2>

          <p>
            Select a course to manage its
            projects and documents.
          </p>
        </div>

        <button
          className="view-all-button"
          type="button"
          onClick={props.onViewAll}
        >
          View all
          <ChevronRight size={17} />
        </button>
      </div>

      {props.courses.length ? (
        <CourseGrid {...props} />
      ) : (
        <div className="empty-courses">
          <div className="empty-courses-icon">
            <BookOpen size={27} />
          </div>

          <h3>Create your first course</h3>

          <p>
            Courses organize your academic
            projects and requirements.
          </p>

          <button
            className="new-course-button"
            type="button"
            onClick={props.onCreate}
          >
            <Plus size={18} />
            New course
          </button>
        </div>
      )}
    </section>
  );
}

function CourseGrid({
  courses,
  onOpen,
  onEdit,
  onDelete,
}: CourseGridProps) {
  return (
    <div className="course-grid">
      {courses.map((course, index) => (
        <article
          className="course-card"
          key={course.id}
        >
          <div
            className={`course-accent accent-${
              (index % 4) + 1
            }`}
          />

          <div className="course-card-header">
            <div className="course-icon">
              <BookOpen size={22} />
            </div>

            <div>
              <button
                type="button"
                className="icon-button"
                onClick={() => onEdit(course)}
                aria-label="Edit course"
              >
                <Pencil size={17} />
              </button>

              <button
                type="button"
                className="icon-button danger"
                onClick={() =>
                  onDelete(course)
                }
                aria-label="Delete course"
              >
                <Trash2 size={17} />
              </button>
            </div>
          </div>

          <button
            className="course-card-content"
            type="button"
            onClick={() => onOpen(course)}
          >
            {course.code && (
              <span className="course-code">
                {course.code}
              </span>
            )}

            <h3>{course.name}</h3>

            <p>
              {course.description ??
                "No description added yet."}
            </p>
          </button>

          <button
            className="course-card-footer"
            type="button"
            onClick={() => onOpen(course)}
          >
            <span>Open workspace</span>
            <ChevronRight size={16} />
          </button>
        </article>
      ))}
    </div>
  );
}

function WorkspaceState({
  loading,
  error,
  empty,
  emptyText,
}: {
  loading: boolean;
  error: string | null;
  empty: boolean;
  emptyText: string;
}) {
  if (loading) {
    return (
      <div className="workspace-message">
        <div className="loading-spinner" />
        <p>Loading...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="workspace-message error">
        <h3>Unable to load data</h3>
        <p>{error}</p>
      </div>
    );
  }

  if (empty) {
    return (
      <div className="workspace-message">
        <h3>{emptyText}</h3>
      </div>
    );
  }

  return null;
}

function EditCourseModal({
  course,
  onClose,
  onSave,
}: {
  course: Course;
  onClose: () => void;
  onSave: (
    id: string,
    input: UpdateCourseInput
  ) => Promise<void>;
}) {
  const [name, setName] =
    useState(course.name);

  const [code, setCode] =
    useState(course.code ?? "");

  const [description, setDescription] =
    useState(course.description ?? "");

  const [busy, setBusy] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  async function submit(
    event: FormEvent
  ) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    try {
      await onSave(course.id, {
        name: name.trim(),
        code: code.trim() || null,
        description:
          description.trim() || null,
      });

      onClose();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to update course."
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="modal-backdrop">
      <section
        className="course-modal"
        role="dialog"
        aria-modal="true"
      >
        <header className="course-modal-header">
          <div>
            <h2>Edit course</h2>
            <p>
              Update the course information.
            </p>
          </div>

          <button
            className="modal-close-button"
            type="button"
            onClick={onClose}
          >
            <X size={20} />
          </button>
        </header>

        <form
          className="course-modal-form"
          onSubmit={submit}
        >
          <div className="form-field">
            <label htmlFor="edit-course-name">
              Course name
            </label>

            <input
              id="edit-course-name"
              value={name}
              onChange={(event) =>
                setName(event.target.value)
              }
              disabled={busy}
              required
            />
          </div>

          <div className="form-field">
            <label htmlFor="edit-course-code">
              Course code
            </label>

            <input
              id="edit-course-code"
              value={code}
              onChange={(event) =>
                setCode(event.target.value)
              }
              disabled={busy}
            />
          </div>

          <div className="form-field">
            <label htmlFor="edit-course-description">
              Description
            </label>

            <textarea
              id="edit-course-description"
              value={description}
              onChange={(event) =>
                setDescription(
                  event.target.value
                )
              }
              disabled={busy}
              rows={4}
            />
          </div>

          {error && (
            <p
              className="modal-error"
              role="alert"
            >
              {error}
            </p>
          )}

          <footer className="course-modal-actions">
            <button
              className="cancel-button"
              type="button"
              onClick={onClose}
              disabled={busy}
            >
              Cancel
            </button>

            <button
              className="create-button"
              type="submit"
              disabled={busy}
            >
              {busy
                ? "Saving..."
                : "Save changes"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}

function DeleteCourseModal({
  course,
  onClose,
  onDelete,
}: {
  course: Course;
  onClose: () => void;
  onDelete: (id: string) => Promise<void>;
}) {
  const [busy, setBusy] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  async function remove() {
    setBusy(true);
    setError(null);

    try {
      await onDelete(course.id);
      onClose();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to delete course."
      );

      setBusy(false);
    }
  }

  return (
    <div className="modal-backdrop">
      <section
        className="course-modal"
        role="dialog"
        aria-modal="true"
      >
        <header className="course-modal-header">
          <div>
            <h2>Delete course?</h2>

            <p>
              This also deletes its projects,
              documents, requirements, and tasks.
            </p>
          </div>

          <button
            className="modal-close-button"
            type="button"
            onClick={onClose}
            disabled={busy}
          >
            <X size={20} />
          </button>
        </header>

        {error && (
          <p
            className="modal-error"
            role="alert"
          >
            {error}
          </p>
        )}

        <footer className="course-modal-actions">
          <button
            className="cancel-button"
            type="button"
            onClick={onClose}
            disabled={busy}
          >
            Cancel
          </button>

          <button
            className="delete-confirm-button"
            type="button"
            onClick={() => void remove()}
            disabled={busy}
          >
            {busy
              ? "Deleting..."
              : "Delete course"}
          </button>
        </footer>
      </section>
    </div>
  );
}

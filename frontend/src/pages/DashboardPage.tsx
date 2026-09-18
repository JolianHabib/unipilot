import { useState } from "react";

import {
  Bell,
  BookOpen,
  ChevronRight,
  FolderKanban,
  LayoutDashboard,
  LogOut,
  Plus,
  Search,
  Sparkles,
} from "lucide-react";

import {
  CreateCourseModal,
} from "../components/CreateCourseModal";

import type {
  Course,
  CreateCourseInput,
  CurrentUser,
} from "../api/auth";

type DashboardPageProps = {
  user: CurrentUser;
  courses: Course[];
  onCourseSelect: (course: Course) => void;
  onCreateCourse: (
    input: CreateCourseInput
  ) => Promise<void>;
  onLogout: () => void;
};

export function DashboardPage({
  user,
  courses,
  onCourseSelect,
  onCreateCourse,
  onLogout,
}: DashboardPageProps) {
  const [
    isCreateCourseOpen,
    setIsCreateCourseOpen,
  ] = useState(false);

  const initials = user.fullName
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0].toUpperCase())
    .join("");

  function openCreateCourseModal() {
    setIsCreateCourseOpen(true);
  }

  function closeCreateCourseModal() {
    setIsCreateCourseOpen(false);
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

          <button
            className="navigation-item active"
            type="button"
          >
            <LayoutDashboard size={18} />
            <span>Overview</span>
          </button>

          <button
            className="navigation-item"
            type="button"
          >
            <BookOpen size={18} />
            <span>Courses</span>
          </button>

          <button
            className="navigation-item"
            type="button"
          >
            <FolderKanban size={18} />
            <span>Projects</span>
          </button>

          <button
            className="navigation-item"
            type="button"
          >
            <Sparkles size={18} />
            <span>AI requirements</span>
          </button>
        </nav>

        <div className="sidebar-profile">
          <div className="profile-avatar">
            {initials}
          </div>

          <div className="profile-details">
            <strong>{user.fullName}</strong>
            <span>{user.email}</span>
          </div>

          <button
            className="sidebar-logout"
            type="button"
            onClick={onLogout}
            aria-label="Sign out"
            title="Sign out"
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
              placeholder="Search your workspace..."
              aria-label="Search workspace"
            />
          </div>

          <button
            className="notification-button"
            type="button"
            aria-label="Notifications"
          >
            <Bell size={20} />
            <span />
          </button>
        </header>

        <div className="dashboard-content">
          <section className="dashboard-heading">
            <div>
              <p className="dashboard-eyebrow">
                Student workspace
              </p>

              <h1>
                Welcome back,{" "}
                {user.fullName.split(" ")[0]}
              </h1>

              <p>
                Manage your courses, projects, and
                AI-extracted requirements.
              </p>
            </div>

            <button
              className="new-course-button"
              type="button"
              onClick={openCreateCourseModal}
            >
              <Plus size={18} />
              New course
            </button>
          </section>

          <section className="dashboard-stats">
            <article className="stat-card">
              <div className="stat-icon purple">
                <BookOpen size={21} />
              </div>

              <div>
                <span>Total courses</span>
                <strong>{courses.length}</strong>
              </div>
            </article>

            <article className="stat-card">
              <div className="stat-icon blue">
                <FolderKanban size={21} />
              </div>

              <div>
                <span>Workspace</span>
                <strong>Active</strong>
              </div>
            </article>

            <article className="stat-card">
              <div className="stat-icon green">
                <Sparkles size={21} />
              </div>

              <div>
                <span>AI analysis</span>
                <strong>Ready</strong>
              </div>
            </article>
          </section>

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
              >
                View all
                <ChevronRight size={17} />
              </button>
            </div>

            {courses.length === 0 ? (
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
                  onClick={openCreateCourseModal}
                >
                  <Plus size={18} />
                  New course
                </button>
              </div>
            ) : (
              <div className="course-grid">
                {courses.map((course, index) => (
                  <button
                    className="course-card"
                    type="button"
                    key={course.id}
                    onClick={() =>
                      onCourseSelect(course)
                    }
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

                      <ChevronRight
                        className="course-arrow"
                        size={20}
                      />
                    </div>

                    <div className="course-card-content">
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
                    </div>

                    <div className="course-card-footer">
                      <span>Open workspace</span>
                      <ChevronRight size={16} />
                    </div>
                  </button>
                ))}
              </div>
            )}
          </section>
        </div>
      </main>

      <CreateCourseModal
        isOpen={isCreateCourseOpen}
        onClose={closeCreateCourseModal}
        onCreate={onCreateCourse}
      />
    </div>
  );
}

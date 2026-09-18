import {
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  ArrowLeft,
  CalendarDays,
  CheckCircle2,
  Circle,
  FileText,
  FolderKanban,
  ListPlus,
  Sparkles,
  Upload,
} from "lucide-react";

import {
  extractDocumentRequirements,
  getProjectDocuments,
  getProjectRequirements,
  uploadProjectDocument,
  type AcademicProject,
  type ProjectDocument,
  type ProjectRequirement,
} from "../api/auth";

import {
  createProjectTask,
  getProjectTasks,
  type ProjectTaskPriority,
} from "../api/tasks";

import {
  UploadDocumentModal,
} from "../components/UploadDocumentModal";

import {
  TaskBoard,
} from "../components/TaskBoard";

type ProjectWorkspacePageProps = {
  token: string;
  project: AcademicProject;
  onBack: () => void;
  onSessionExpired: () => void;
};

type ProjectTab =
  | "overview"
  | "documents"
  | "requirements"
  | "tasks";

export function ProjectWorkspacePage({
  token,
  project,
  onBack,
  onSessionExpired,
}: ProjectWorkspacePageProps) {
  const [documents, setDocuments] =
    useState<ProjectDocument[]>([]);

  const [requirements, setRequirements] =
    useState<ProjectRequirement[]>([]);

  const [activeTab, setActiveTab] =
    useState<ProjectTab>("overview");

  const [
    isUploadModalOpen,
    setIsUploadModalOpen,
  ] = useState(false);

  const [
    analyzingDocumentId,
    setAnalyzingDocumentId,
  ] = useState<string | null>(null);

  const [analysisError, setAnalysisError] =
    useState<string | null>(null);

  const [taskCreationError, setTaskCreationError] =
    useState<string | null>(null);

  const [convertingRequirementId, setConvertingRequirementId] =
    useState<string | null>(null);

  const [isLoading, setIsLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    let isCancelled = false;

    async function loadProject() {
      setIsLoading(true);
      setError(null);

      try {
        const [
          documentResults,
          requirementResults,
        ] = await Promise.all([
          getProjectDocuments(token, project.id),
          getProjectRequirements(token, project.id),
        ]);

        if (isCancelled) {
          return;
        }

        setDocuments(documentResults);
        setRequirements(requirementResults);
      } catch (loadError) {
        if (isCancelled) {
          return;
        }

        const message =
          loadError instanceof Error
            ? loadError.message
            : "Unable to load the project.";

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

    void loadProject();

    return () => {
      isCancelled = true;
    };
  }, [
    token,
    project.id,
    onSessionExpired,
  ]);

  const completedRequirements = useMemo(
    () =>
      requirements.filter(
        (requirement) =>
          requirement.isCompleted
      ).length,
    [requirements]
  );

  const completionPercentage =
    requirements.length === 0
      ? 0
      : Math.round(
          (completedRequirements /
            requirements.length) *
            100
        );

  async function handleUploadDocument(
    file: File
  ) {
    try {
      await uploadProjectDocument(
        token,
        project.id,
        file
      );

      const updatedDocuments =
        await getProjectDocuments(
          token,
          project.id
        );

      setDocuments(updatedDocuments);
      setActiveTab("documents");
      setIsUploadModalOpen(false);
    } catch (uploadError) {
      if (
        uploadError instanceof Error &&
        uploadError.message ===
          "SESSION_EXPIRED"
      ) {
        onSessionExpired();
      }

      throw uploadError;
    }
  }

 async function handleAnalyzeDocument(
  documentId: string
) {
  setAnalyzingDocumentId(documentId);
  setAnalysisError(null);

  try {
    await extractDocumentRequirements(
      token,
      documentId
    );

    const updatedRequirements =
      await getProjectRequirements(
        token,
        project.id
      );

    setRequirements(updatedRequirements);
    setActiveTab("requirements");
  } catch (analyzeError) {
    const message =
      analyzeError instanceof Error
        ? analyzeError.message
        : "Unable to analyze this document.";

    if (message === "SESSION_EXPIRED") {
      onSessionExpired();
      return;
    }

    setAnalysisError(message);
  } finally {
    setAnalyzingDocumentId(null);
  }
}
  function formatFileSize(bytes: number) {
    if (bytes < 1024) {
      return `${bytes} B`;
    }

    if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(1)} KB`;
    }

    return `${(
      bytes /
      (1024 * 1024)
    ).toFixed(1)} MB`;
  }

  function formatDueDate(value: string) {
    return new Intl.DateTimeFormat("en", {
      day: "numeric",
      month: "short",
      year: "numeric",
    }).format(new Date(value));
  }

  async function handleCreateTaskFromRequirement(
    requirement: ProjectRequirement
  ) {
    setConvertingRequirementId(requirement.id);
    setTaskCreationError(null);

    try {
      const existingTasks = await getProjectTasks(
        token,
        project.id
      );

      const alreadyCreated = existingTasks.some(
        (task) =>
          task.projectRequirementId === requirement.id
      );

      if (alreadyCreated) {
        setTaskCreationError(
          "A task already exists for this requirement."
        );
        return;
      }

      const priority =
        (["Low", "Medium", "High"].includes(
          requirement.priority
        )
          ? requirement.priority
          : "Medium") as ProjectTaskPriority;

      await createProjectTask(token, project.id, {
        projectRequirementId: requirement.id,
        title: requirement.title,
        description: requirement.description,
        priority,
        dueDateUtc: null,
      });

      setActiveTab("tasks");
    } catch (createError) {
      const message =
        createError instanceof Error
          ? createError.message
          : "Unable to create a task.";

      if (message === "SESSION_EXPIRED") {
        onSessionExpired();
        return;
      }

      setTaskCreationError(message);
    } finally {
      setConvertingRequirementId(null);
    }
  }

  return (
    <main className="project-workspace">
      <header className="project-workspace-topbar">
        <button
          className="workspace-back-button"
          type="button"
          onClick={onBack}
        >
          <ArrowLeft size={18} />
          Course
        </button>

        <div className="workspace-brand">
          <Sparkles size={18} />
          <span>UniPilot</span>
        </div>
      </header>

      <div className="project-workspace-content">
        <section className="project-hero">
          <div className="project-hero-icon">
            <FolderKanban size={26} />
          </div>

          <div className="project-hero-details">
            <div className="project-title-row">
              <h1>{project.title}</h1>

              <span className="project-hero-status">
                {project.status}
              </span>
            </div>

            <p>
              {project.description ??
                "No project description added."}
            </p>

            {project.dueDateUtc && (
              <span className="project-due-date">
                <CalendarDays size={15} />

                {formatDueDate(
                  project.dueDateUtc
                )}
              </span>
            )}
          </div>

          <button
            className="upload-document-button"
            type="button"
            onClick={() =>
              setIsUploadModalOpen(true)
            }
          >
            <Upload size={18} />
            Upload PDF
          </button>
        </section>

        <nav className="project-tabs">
          <button
            className={
              activeTab === "overview"
                ? "active"
                : ""
            }
            type="button"
            onClick={() =>
              setActiveTab("overview")
            }
          >
            Overview
          </button>

          <button
            className={
              activeTab === "documents"
                ? "active"
                : ""
            }
            type="button"
            onClick={() =>
              setActiveTab("documents")
            }
          >
            Documents
            <span>{documents.length}</span>
          </button>

          <button
            className={
              activeTab === "requirements"
                ? "active"
                : ""
            }
            type="button"
            onClick={() =>
              setActiveTab("requirements")
            }
          >
            AI requirements
            <span>{requirements.length}</span>
          </button>

          <button
            className={
              activeTab === "tasks"
                ? "active"
                : ""
            }
            type="button"
            onClick={() =>
              setActiveTab("tasks")
            }
          >
            Task board
          </button>
        </nav>

        {isLoading && (
          <div className="workspace-message">
            <div className="loading-spinner" />
            <p>Loading project workspace...</p>
          </div>
        )}

        {!isLoading && error && (
          <div className="workspace-message error">
            <h3>Unable to load project</h3>
            <p>{error}</p>
          </div>
        )}

        {!isLoading &&
          !error &&
          activeTab === "overview" && (
            <>
              <section className="project-summary-grid">
                <article className="project-summary-card">
                  <div className="summary-icon documents">
                    <FileText size={21} />
                  </div>

                  <div>
                    <span>Documents</span>

                    <strong>
                      {documents.length}
                    </strong>
                  </div>
                </article>

                <article className="project-summary-card">
                  <div className="summary-icon requirements">
                    <Sparkles size={21} />
                  </div>

                  <div>
                    <span>Requirements</span>

                    <strong>
                      {requirements.length}
                    </strong>
                  </div>
                </article>

                <article className="project-summary-card">
                  <div className="summary-icon completed">
                    <CheckCircle2 size={21} />
                  </div>

                  <div>
                    <span>Completed</span>

                    <strong>
                      {completionPercentage}%
                    </strong>
                  </div>
                </article>
              </section>

              <section className="completion-panel">
                <div className="completion-heading">
                  <div>
                    <h2>
                      Requirements progress
                    </h2>

                    <p>
                      {completedRequirements} of{" "}
                      {requirements.length} completed
                    </p>
                  </div>

                  <strong>
                    {completionPercentage}%
                  </strong>
                </div>

                <div className="completion-track">
                  <div
                    style={{
                      width: `${completionPercentage}%`,
                    }}
                  />
                </div>
              </section>
            </>
          )}

        {!isLoading &&
          !error &&
          activeTab === "documents" && (
            <section className="documents-list">
              {analysisError && (
                <div className="document-analysis-error">
                  {analysisError}
                </div>
              )}

              {documents.length === 0 ? (
                <div className="empty-projects">
                  <div className="empty-projects-icon">
                    <FileText size={28} />
                  </div>

                  <h3>No documents yet</h3>

                  <p>
                    Upload a PDF to extract its text
                    and analyze requirements.
                  </p>

                  <button
                    className="upload-document-button"
                    type="button"
                    onClick={() =>
                      setIsUploadModalOpen(true)
                    }
                  >
                    <Upload size={18} />
                    Upload PDF
                  </button>
                </div>
              ) : (
                documents.map((document) => {
                  const isAnalyzing =
                    analyzingDocumentId ===
                    document.id;

                  const alreadyAnalyzed =
                    requirements.some(
                      (requirement) =>
                        requirement.projectDocumentId ===
                        document.id
                    );

                  const isReady =
                    document.processingStatus.toLowerCase() ===
                    "ready";

                  return (
                    <article
                      className="document-row"
                      key={document.id}
                    >
                      <div className="document-row-icon">
                        <FileText size={21} />
                      </div>

                      <div className="document-row-details">
                        <h3>
                          {
                            document.originalFileName
                          }
                        </h3>

                        <p>
                          {formatFileSize(
                            document.fileSizeBytes
                          )}
                          {" · "}
                          {document.pageCount} pages
                          {" · "}
                          {document.documentType}
                        </p>

                        {document.failureReason && (
                          <small className="document-failure">
                            {
                              document.failureReason
                            }
                          </small>
                        )}
                      </div>

                      <div className="document-row-actions">
                        <span
                          className={`document-status document-${document.processingStatus.toLowerCase()}`}
                        >
                          {
                            document.processingStatus
                          }
                        </span>

                        {alreadyAnalyzed ? (
                          <button
                            className="analyze-document-button completed"
                            type="button"
                            onClick={() =>
                              setActiveTab(
                                "requirements"
                              )
                            }
                          >
                            <CheckCircle2
                              size={16}
                            />

                            View requirements
                          </button>
                        ) : (
                          <button
                            className="analyze-document-button"
                            type="button"
                            disabled={
                              !isReady ||
                              isAnalyzing
                            }
                            onClick={() =>
                              void handleAnalyzeDocument(
                                document.id
                              )
                            }
                          >
                            <Sparkles size={16} />

                            {isAnalyzing
                              ? "Analyzing..."
                              : "Analyze with AI"}
                          </button>
                        )}
                      </div>
                    </article>
                  );
                })
              )}
            </section>
          )}

        {!isLoading &&
          !error &&
          activeTab === "requirements" && (
            <section className="requirements-list">
              {taskCreationError && (
                <div className="document-analysis-error">
                  {taskCreationError}
                </div>
              )}

              {requirements.length === 0 ? (
                <div className="empty-projects">
                  <div className="empty-projects-icon">
                    <Sparkles size={28} />
                  </div>

                  <h3>No requirements yet</h3>

                  <p>
                    Upload and analyze a document to
                    generate AI requirements.
                  </p>

                  <button
                    className="analyze-document-button"
                    type="button"
                    onClick={() =>
                      setActiveTab("documents")
                    }
                  >
                    <FileText size={16} />
                    View documents
                  </button>
                </div>
              ) : (
                requirements.map(
                  (requirement) => (
                    <article
                      className={`requirement-row ${
                        requirement.isCompleted
                          ? "completed"
                          : ""
                      }`}
                      key={requirement.id}
                    >
                      <div className="requirement-check">
                        {requirement.isCompleted ? (
                          <CheckCircle2
                            size={21}
                          />
                        ) : (
                          <Circle size={21} />
                        )}
                      </div>

                      <div className="requirement-details">
                        <div className="requirement-title">
                          <h3>
                            {requirement.title}
                          </h3>

                          <span
                            className={`priority-${requirement.priority.toLowerCase()}`}
                          >
                            {
                              requirement.priority
                            }
                          </span>
                        </div>

                        <p>
                          {
                            requirement.description
                          }
                        </p>

                        <small>
                          {requirement.type}
                          {" · Page "}
                          {
                            requirement.sourcePageNumber
                          }
                        </small>

                        <button
                          className="requirement-task-button"
                          type="button"
                          disabled={
                            convertingRequirementId ===
                            requirement.id
                          }
                          onClick={() =>
                            void handleCreateTaskFromRequirement(
                              requirement
                            )
                          }
                        >
                          <ListPlus size={16} />
                          {convertingRequirementId ===
                          requirement.id
                            ? "Creating..."
                            : "Create task"}
                        </button>
                      </div>
                    </article>
                  )
                )
              )}
            </section>
          )}

        {!isLoading &&
          !error &&
          activeTab === "tasks" && (
            <TaskBoard
              token={token}
              projectId={project.id}
              onSessionExpired={
                onSessionExpired
              }
              onRequirementsChanged={async () => {
                const updatedRequirements =
                  await getProjectRequirements(
                    token,
                    project.id
                  );

                setRequirements(
                  updatedRequirements
                );
              }}
            />
          )}
      </div>

      <UploadDocumentModal
        isOpen={isUploadModalOpen}
        onClose={() =>
          setIsUploadModalOpen(false)
        }
        onUpload={handleUploadDocument}
      />
    </main>
  );
}

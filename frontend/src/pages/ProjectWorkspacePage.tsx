import {
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  ArrowLeft,
  CalendarDays,
  CheckCircle2,
  FileText,
  FolderKanban,
  ListPlus,
  Search,
  Sparkles,
  Upload,
  X,
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
  moveProjectTask,
  type ProjectTaskPriority,
} from "../api/tasks";

import {
  UploadDocumentModal,
} from "../components/UploadDocumentModal";

import {
  TaskBoard,
} from "../components/TaskBoard";

import {
  DeleteDocumentButton,
} from "../components/DeleteDocumentButton";
import {
  DocumentFileButtons,
} from "../components/DocumentFileButtons";

import {
  RetryDocumentButton,
} from "../components/RetryDocumentButton";

import {
  RequirementSourceButton,
} from "../components/RequirementSourceButton";

import {
  RequirementCompletionButton,
} from "../components/RequirementCompletionButton";

import {
  RequirementActions,
} from "../components/RequirementActions";

import {
  ProjectActivityPanel,
} from "../components/ProjectActivityPanel";

import {
  ExportProjectReportButton,
} from "../components/ExportProjectReportButton";

import {
  CreateRequirementButton,
} from "../components/CreateRequirementButton";

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
  | "tasks"
  | "activity";

type RequirementCompletionFilter =
  | "All"
  | "Open"
  | "Completed";

type RequirementSort =
  | "Newest"
  | "Priority";

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

  const [requirementQuery, setRequirementQuery] =
    useState("");

  const [requirementType, setRequirementType] =
    useState("All");

  const [requirementPriority, setRequirementPriority] =
    useState("All");

  const [requirementCompletion, setRequirementCompletion] =
    useState<RequirementCompletionFilter>("All");

  const [requirementSort, setRequirementSort] =
    useState<RequirementSort>("Newest");

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

  const visibleRequirements = useMemo(() => {
    const query = requirementQuery
      .trim()
      .toLowerCase();

    const priorityWeight: Record<string, number> = {
      Critical: 4,
      High: 3,
      Medium: 2,
      Low: 1,
    };

    const filtered = requirements.filter(
      (requirement) => {
        const matchesQuery =
          !query ||
          requirement.title
            .toLowerCase()
            .includes(query) ||
          requirement.description
            .toLowerCase()
            .includes(query);

        const matchesType =
          requirementType === "All" ||
          requirement.type === requirementType;

        const matchesPriority =
          requirementPriority === "All" ||
          requirement.priority === requirementPriority;

        const matchesCompletion =
          requirementCompletion === "All" ||
          (requirementCompletion === "Completed"
            ? requirement.isCompleted
            : !requirement.isCompleted);

        return (
          matchesQuery &&
          matchesType &&
          matchesPriority &&
          matchesCompletion
        );
      }
    );

    return [...filtered].sort((left, right) => {
      if (requirementSort === "Priority") {
        return (
          (priorityWeight[right.priority] ?? 0) -
          (priorityWeight[left.priority] ?? 0)
        );
      }

      return (
        new Date(right.createdAtUtc).getTime() -
        new Date(left.createdAtUtc).getTime()
      );
    });
  }, [
    requirements,
    requirementQuery,
    requirementType,
    requirementPriority,
    requirementCompletion,
    requirementSort,
  ]);

  const hasRequirementFilters =
    requirementQuery.trim().length > 0 ||
    requirementType !== "All" ||
    requirementPriority !== "All" ||
    requirementCompletion !== "All" ||
    requirementSort !== "Newest";

  function clearRequirementFilters() {
    setRequirementQuery("");
    setRequirementType("All");
    setRequirementPriority("All");
    setRequirementCompletion("All");
    setRequirementSort("Newest");
  }

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

      const createdTask =
  await createProjectTask(
    token,
    project.id,
    {
      projectRequirementId:
        requirement.id,
      title: requirement.title,
      description:
        requirement.description,
      priority,
      dueDateUtc: null,
    }
  );

if (requirement.isCompleted) {
  const completedPosition =
    existingTasks.filter(
      (task) => task.status === "Done"
    ).length;

  await moveProjectTask(
    token,
    project.id,
    createdTask.id,
    "Done",
    completedPosition
  );
}

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
  function handleDocumentDeleted(
  documentId: string
) {
  setDocuments((currentDocuments) =>
    currentDocuments.filter(
      (document) =>
        document.id !== documentId
    )
  );

  setRequirements(
    (currentRequirements) =>
      currentRequirements.map(
        (requirement) =>
          requirement.projectDocumentId ===
          documentId
            ? {
                ...requirement,
                projectDocumentId: null,
              }
            : requirement
      )
  );

  setAnalysisError(null);
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

        <div className="workspace-topbar-actions">
          <ExportProjectReportButton
            token={token}
            project={project}
            documents={documents}
            requirements={requirements}
            onSessionExpired={
              onSessionExpired
            }
          />

          <div className="workspace-brand">
            <Sparkles size={18} />
            <span>UniPilot</span>
          </div>
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
          <button
  className={
    activeTab === "activity"
      ? "active"
      : ""
  }
  type="button"
  onClick={() =>
    setActiveTab("activity")
  }
>
  Activity
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
                        {document.processingStatus
  .toLowerCase() === "failed" && (
  <RetryDocumentButton
    token={token}
    projectId={project.id}
    document={document}
    onRetried={(
      updatedDocument
    ) => {
      setDocuments(
        (currentDocuments) =>
          currentDocuments.map(
            (currentDocument) =>
              currentDocument.id ===
              updatedDocument.id
                ? updatedDocument
                : currentDocument
          )
      );
    }}
    onSessionExpired={
      onSessionExpired
    }
  />
)}
                        <DocumentFileButtons
  token={token}
  projectId={project.id}
  documentId={document.id}
  fileName={
    document.originalFileName
  }
  onSessionExpired={
    onSessionExpired
  }
/>
                        <DeleteDocumentButton
  token={token}
  projectId={project.id}
  document={document}
  onDeleted={
    handleDocumentDeleted
  }
  onSessionExpired={
    onSessionExpired
  }
/>
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
              <div className="requirements-toolbar">
                <div>
                  <strong>
                    {visibleRequirements.length}
                    {" of "}
                    {requirements.length}
                  </strong>
                  <span> requirements shown</span>
                </div>

                <CreateRequirementButton
                  token={token}
                  projectId={project.id}
                  onCreated={(createdRequirement) => {
                    setRequirements(
                      (currentRequirements) => [
                        createdRequirement,
                        ...currentRequirements,
                      ]
                    );
                  }}
                  onSessionExpired={onSessionExpired}
                />
              </div>

              {requirements.length > 0 && (
                <div className="requirement-filter-panel">
                  <label className="requirement-search-field">
                    <Search size={17} />
                    <input
                      value={requirementQuery}
                      onChange={(event) =>
                        setRequirementQuery(
                          event.target.value
                        )
                      }
                      placeholder="Search requirements..."
                      aria-label="Search requirements"
                    />
                  </label>

                  <select
                    value={requirementType}
                    onChange={(event) =>
                      setRequirementType(event.target.value)
                    }
                    aria-label="Filter by requirement type"
                  >
                    <option value="All">All types</option>
                    <option value="Functional">Functional</option>
                    <option value="NonFunctional">
                      Non-functional
                    </option>
                    <option value="Constraint">Constraint</option>
                  </select>

                  <select
                    value={requirementPriority}
                    onChange={(event) =>
                      setRequirementPriority(
                        event.target.value
                      )
                    }
                    aria-label="Filter by priority"
                  >
                    <option value="All">All priorities</option>
                    <option value="Critical">Critical</option>
                    <option value="High">High</option>
                    <option value="Medium">Medium</option>
                    <option value="Low">Low</option>
                  </select>

                  <select
                    value={requirementCompletion}
                    onChange={(event) =>
                      setRequirementCompletion(
                        event.target
                          .value as RequirementCompletionFilter
                      )
                    }
                    aria-label="Filter by completion"
                  >
                    <option value="All">All progress</option>
                    <option value="Open">Open</option>
                    <option value="Completed">Completed</option>
                  </select>

                  <select
                    value={requirementSort}
                    onChange={(event) =>
                      setRequirementSort(
                        event.target.value as RequirementSort
                      )
                    }
                    aria-label="Sort requirements"
                  >
                    <option value="Newest">Newest first</option>
                    <option value="Priority">
                      Highest priority
                    </option>
                  </select>

                  {hasRequirementFilters && (
                    <button
                      className="clear-requirement-filters"
                      type="button"
                      onClick={clearRequirementFilters}
                    >
                      <X size={15} />
                      Clear
                    </button>
                  )}
                </div>
              )}

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
              ) : visibleRequirements.length === 0 ? (
                <div className="empty-projects compact">
                  <div className="empty-projects-icon">
                    <Search size={25} />
                  </div>

                  <h3>No matching requirements</h3>
                  <p>
                    Try changing or clearing the filters.
                  </p>

                  <button
                    className="analyze-document-button"
                    type="button"
                    onClick={clearRequirementFilters}
                  >
                    <X size={16} />
                    Clear filters
                  </button>
                </div>
              ) : (
                visibleRequirements.map(
                  (requirement) => (
                    <article
                      className={`requirement-row ${
                        requirement.isCompleted
                          ? "completed"
                          : ""
                      }`}
                      key={requirement.id}
                    >
                      <RequirementCompletionButton
                        token={token}
                        requirement={requirement}
                        onSessionExpired={
                          onSessionExpired
                        }
                        onUpdated={(updated) => {
                          setRequirements(
                            (currentRequirements) =>
                              currentRequirements.map(
                                (currentRequirement) =>
                                  currentRequirement.id ===
                                  updated.id
                                    ? updated
                                    : currentRequirement
                              )
                          );
                        }}
                      />

                      <RequirementActions
                        token={token}
                        requirement={requirement}
                        onUpdated={(updated) => {
                          setRequirements(
                            (currentRequirements) =>
                              currentRequirements.map(
                                (currentRequirement) =>
                                  currentRequirement.id ===
                                  updated.id
                                    ? updated
                                    : currentRequirement
                              )
                          );
                        }}
                        onDeleted={(requirementId) => {
                          setRequirements(
                            (currentRequirements) =>
                              currentRequirements.filter(
                                (currentRequirement) =>
                                  currentRequirement.id !==
                                  requirementId
                              )
                          );
                        }}
                        onSessionExpired={
                          onSessionExpired
                        }
                      />

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
</small>

{requirement.projectDocumentId &&
  requirement.sourcePageNumber && (
    <RequirementSourceButton
      token={token}
      projectId={project.id}
      documentId={
        requirement.projectDocumentId
      }
      pageNumber={
        requirement.sourcePageNumber
      }
      onSessionExpired={
        onSessionExpired
      }
    />
  )}

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

        {!isLoading &&
          !error &&
          activeTab === "activity" && (
            <ProjectActivityPanel
              token={token}
              projectId={project.id}
              onSessionExpired={
                onSessionExpired
              }
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

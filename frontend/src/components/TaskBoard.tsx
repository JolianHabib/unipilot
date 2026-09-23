import {
  useEffect,
  useState,
  type DragEvent,
  type FormEvent,
} from "react";

import {
  CalendarDays,
  GripVertical,
  Pencil,
  Plus,
  Sparkles,
  Trash2,
  X,
} from "lucide-react";

import {
  createProjectTask,
  deleteProjectTask,
  getProjectTasks,
  moveProjectTask,
  updateProjectTask,
  type ProjectTask,
  type ProjectTaskPriority,
  type ProjectTaskStatus,
} from "../api/tasks";

type TaskBoardProps = {
  token: string;
  projectId: string;
  readOnly?: boolean;
  onSessionExpired: () => void;
  onRequirementsChanged?: () =>
    Promise<void> | void;
};

type BoardColumn = {
  status: ProjectTaskStatus;
  title: string;
  description: string;
};

const columns: BoardColumn[] = [
  {
    status: "ToDo",
    title: "To do",
    description: "Tasks ready to start",
  },
  {
    status: "InProgress",
    title: "In progress",
    description: "Tasks currently being worked on",
  },
  {
    status: "Done",
    title: "Done",
    description: "Completed tasks",
  },
];

export function TaskBoard({
  token,
  projectId,
  readOnly = false,
  onSessionExpired,
  onRequirementsChanged,
}: TaskBoardProps) {
  const [tasks, setTasks] =
    useState<ProjectTask[]>([]);

  const [isLoading, setIsLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(null);

  const [isCreateOpen, setIsCreateOpen] =
    useState(false);

  const [editingTask, setEditingTask] =
    useState<ProjectTask | null>(null);

  const [title, setTitle] = useState("");
  const [description, setDescription] =
    useState("");

  const [priority, setPriority] =
    useState<ProjectTaskPriority>("Medium");

  const [dueDate, setDueDate] = useState("");

  const [isSaving, setIsSaving] =
    useState(false);

  const [draggedTaskId, setDraggedTaskId] =
    useState<string | null>(null);

  useEffect(() => {
    let isCancelled = false;

    async function loadTasks() {
      setIsLoading(true);
      setError(null);

      try {
        const results = await getProjectTasks(
          token,
          projectId
        );

        if (!isCancelled) {
          setTasks(results);
        }
      } catch (loadError) {
        if (isCancelled) {
          return;
        }

        handleError(loadError);
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadTasks();

    return () => {
      isCancelled = true;
    };
  }, [token, projectId]);

  function handleError(value: unknown) {
    const message =
      value instanceof Error
        ? value.message
        : "Something went wrong.";

    if (message === "SESSION_EXPIRED") {
      onSessionExpired();
      return;
    }

    setError(message);
  }

  function getColumnTasks(
    status: ProjectTaskStatus
  ) {
    return tasks
      .filter((task) => task.status === status)
      .sort((first, second) =>
        first.position - second.position
      );
  }

  function resetForm() {
    setTitle("");
    setDescription("");
    setPriority("Medium");
    setDueDate("");
  }

  function openCreateModal() {
    if (readOnly) {
      return;
    }

    setEditingTask(null);
    resetForm();
    setError(null);
    setIsCreateOpen(true);
  }

  function openEditModal(task: ProjectTask) {
    if (readOnly) {
      return;
    }

    setEditingTask(task);
    setTitle(task.title);
    setDescription(task.description ?? "");
    setPriority(task.priority);
    setDueDate(
      task.dueDateUtc
        ? task.dueDateUtc.slice(0, 10)
        : ""
    );
    setError(null);
    setIsCreateOpen(true);
  }

  function closeCreateModal() {
    if (isSaving) {
      return;
    }

    setIsCreateOpen(false);
    setEditingTask(null);
    resetForm();
  }

  async function handleCreate(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    if (readOnly) {
      return;
    }

    if (!title.trim()) {
      setError("Task title is required.");
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      const input = {
        projectRequirementId:
          editingTask?.projectRequirementId ?? null,
        title: title.trim(),
        description: description.trim() || null,
        priority,
        dueDateUtc: dueDate
          ? new Date(dueDate).toISOString()
          : null,
      };

      if (editingTask) {
        const updatedTask = await updateProjectTask(
          token,
          projectId,
          editingTask.id,
          input
        );

        setTasks((currentTasks) =>
          currentTasks.map((task) =>
            task.id === updatedTask.id
              ? updatedTask
              : task
          )
        );
      } else {
        const createdTask = await createProjectTask(
          token,
          projectId,
          input
        );

        setTasks((currentTasks) => [
          ...currentTasks,
          createdTask,
        ]);
      }

      setIsCreateOpen(false);
      setEditingTask(null);
      resetForm();
    } catch (createError) {
      handleError(createError);
    } finally {
      setIsSaving(false);
    }
  }

  function handleDragStart(
    event: DragEvent<HTMLDivElement>,
    taskId: string
  ) {
    if (readOnly) {
      return;
    }

    setDraggedTaskId(taskId);
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData(
      "text/plain",
      taskId
    );
  }

  function handleDragOver(
    event: DragEvent<HTMLDivElement>
  ) {
    if (readOnly) {
      return;
    }

    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
  }

  async function handleDrop(
    event: DragEvent<HTMLDivElement>,
    status: ProjectTaskStatus
  ) {
    event.preventDefault();

    if (readOnly) {
      return;
    }

    const taskId =
      event.dataTransfer.getData("text/plain") ||
      draggedTaskId;

    if (!taskId) {
      return;
    }

    const currentTask = tasks.find(
      (task) => task.id === taskId
    );

    if (!currentTask) {
      return;
    }

    const targetPosition =
      getColumnTasks(status).filter(
        (task) => task.id !== taskId
      ).length;

    setDraggedTaskId(null);
    setError(null);

    try {
      const movedTask =
        await moveProjectTask(
          token,
          projectId,
          taskId,
          status,
          targetPosition
        );

      const refreshedTasks =
        await getProjectTasks(
          token,
          projectId
        );

      setTasks(
        refreshedTasks.map((task) =>
          task.id === movedTask.id
            ? movedTask
            : task
        )
      );

      await onRequirementsChanged?.();
    } catch (moveError) {
      handleError(moveError);
    }
  }

  async function handleDelete(
    taskId: string
  ) {
    if (readOnly) {
      return;
    }

    const shouldDelete = window.confirm(
      "Delete this task?"
    );

    if (!shouldDelete) {
      return;
    }

    setError(null);

    try {
      await deleteProjectTask(
        token,
        projectId,
        taskId
      );

      setTasks((currentTasks) =>
        currentTasks.filter(
          (task) => task.id !== taskId
        )
      );
    } catch (deleteError) {
      handleError(deleteError);
    }
  }

  function formatDate(value: string) {
    return new Intl.DateTimeFormat("en", {
      day: "numeric",
      month: "short",
      year: "numeric",
    }).format(new Date(value));
  }

  return (
    <section className="task-board-section">
      <div className="task-board-heading">
        <div>
          <h2>Project board</h2>

          <p>
            Organize work and move tasks through
            each stage.
          </p>
        </div>

        {readOnly ? (
          <span className="project-status">
            View only
          </span>
        ) : (
          <button
            className="new-project-button"
            type="button"
            onClick={openCreateModal}
          >
            <Plus size={18} />
            New task
          </button>
        )}
      </div>

      {error && (
        <div className="task-board-error">
          {error}
        </div>
      )}

      {isLoading ? (
        <div className="workspace-message">
          <div className="loading-spinner" />
          <p>Loading tasks...</p>
        </div>
      ) : (
        <div className="task-board">
          {columns.map((column) => {
            const columnTasks =
              getColumnTasks(column.status);

            return (
              <div
                className={`task-column task-column-${column.status.toLowerCase()}`}
                key={column.status}
                onDragOver={
                  readOnly
                    ? undefined
                    : handleDragOver
                }
                onDrop={
                  readOnly
                    ? undefined
                    : (event) =>
                        void handleDrop(
                          event,
                          column.status
                        )
                }
              >
                <div className="task-column-header">
                  <div>
                    <h3>{column.title}</h3>
                    <p>{column.description}</p>
                  </div>

                  <span>{columnTasks.length}</span>
                </div>

                <div className="task-column-content">
                  {columnTasks.length === 0 ? (
                    <div className="empty-task-column">
                      {readOnly
                        ? "No tasks"
                        : "Drop tasks here"}
                    </div>
                  ) : (
                    columnTasks.map((task) => (
                      <div
                        className={`task-board-card ${
                          draggedTaskId === task.id
                            ? "dragging"
                            : ""
                        }`}
                        key={task.id}
                        draggable={!readOnly}
                        onDragStart={
                          readOnly
                            ? undefined
                            : (event) =>
                                handleDragStart(
                                  event,
                                  task.id
                                )
                        }
                        onDragEnd={
                          readOnly
                            ? undefined
                            : () =>
                                setDraggedTaskId(null)
                        }
                      >
                        <div className="task-card-top">
                          {!readOnly && (
                            <GripVertical
                              className="task-drag-handle"
                              size={17}
                            />
                          )}

                          <span
                            className={`task-priority priority-${task.priority.toLowerCase()}`}
                          >
                            {task.priority}
                          </span>

                          {!readOnly && (
                            <button
                            type="button"
                            className="task-edit-button"
                            onClick={() =>
                              openEditModal(task)
                            }
                            aria-label="Edit task"
                          >
                            <Pencil size={15} />
                            </button>
                          )}

                          {!readOnly && (
                            <button
                            type="button"
                            className="task-delete-button"
                            onClick={() =>
                              void handleDelete(
                                task.id
                              )
                            }
                            aria-label="Delete task"
                          >
                            <Trash2 size={15} />
                            </button>
                          )}
                        </div>

                        <h4>{task.title}</h4>

                        {task.description && (
                          <p>{task.description}</p>
                        )}

                        {task.projectRequirementId && (
                          <div className="task-ai-source">
                            <Sparkles size={13} />
                            AI requirement
                          </div>
                        )}

                        {task.dueDateUtc && (
                          <div className="task-due-date">
                            <CalendarDays size={14} />
                            {formatDate(
                              task.dueDateUtc
                            )}
                          </div>
                        )}
                      </div>
                    ))
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {!readOnly && isCreateOpen && (
        <div
          className="modal-backdrop"
          onMouseDown={closeCreateModal}
        >
          <div
            className="modal-card task-modal-card"
            role="dialog"
            aria-modal="true"
            aria-labelledby="create-task-title"
            onMouseDown={(event) =>
              event.stopPropagation()
            }
          >
            <div className="modal-heading">
              <div>
                <p className="dashboard-eyebrow">
                  Project board
                </p>

                <h2 id="create-task-title">
                  {editingTask
                    ? "Edit task"
                    : "Create a new task"}
                </h2>
              </div>

              <button
                className="modal-close-button"
                type="button"
                onClick={closeCreateModal}
                disabled={isSaving}
                aria-label="Close"
              >
                <X size={19} />
              </button>
            </div>

            <form onSubmit={handleCreate}>
              <label htmlFor="task-title">
                Task title
              </label>

              <input
                id="task-title"
                value={title}
                onChange={(event) =>
                  setTitle(event.target.value)
                }
                placeholder="Example: Build login form"
                maxLength={250}
                autoFocus
              />

              <label htmlFor="task-description">
                Description
              </label>

              <textarea
                id="task-description"
                value={description}
                onChange={(event) =>
                  setDescription(
                    event.target.value
                  )
                }
                placeholder="Describe the task..."
                rows={4}
              />

              <label htmlFor="task-priority">
                Priority
              </label>

              <select
                id="task-priority"
                value={priority}
                onChange={(event) =>
                  setPriority(
                    event.target
                      .value as ProjectTaskPriority
                  )
                }
              >
                <option value="Low">Low</option>
                <option value="Medium">
                  Medium
                </option>
                <option value="High">High</option>
              </select>

              <label htmlFor="task-due-date">
                Due date
              </label>

              <input
                id="task-due-date"
                type="date"
                value={dueDate}
                onChange={(event) =>
                  setDueDate(event.target.value)
                }
              />

              <button
                className="new-project-button task-submit-button"
                type="submit"
                disabled={isSaving}
              >
                {isSaving
                  ? "Saving..."
                  : editingTask
                    ? "Save changes"
                    : "Create task"}
              </button>
            </form>
          </div>
        </div>
      )}
    </section>
  );
}

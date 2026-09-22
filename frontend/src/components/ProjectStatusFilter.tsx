import type {
  AcademicProject,
  ProjectStatus,
} from "../api/auth";

export type ProjectStatusFilterValue =
  | "All"
  | ProjectStatus;

type ProjectStatusFilterProps = {
  projects: AcademicProject[];
  value: ProjectStatusFilterValue;
  onChange: (
    value: ProjectStatusFilterValue
  ) => void;
};

const options: ProjectStatusFilterValue[] = [
  "All",
  "Draft",
  "Active",
  "Completed",
  "Archived",
];

export function ProjectStatusFilter({
  projects,
  value,
  onChange,
}: ProjectStatusFilterProps) {
  return (
    <div
      className="project-status-filter"
      aria-label="Filter projects by status"
    >
      {options.map((option) => {
        const count =
          option === "All"
            ? projects.length
            : projects.filter(
                (project) =>
                  project.status === option
              ).length;

        return (
          <button
            className={
              value === option ? "active" : ""
            }
            type="button"
            key={option}
            onClick={() => onChange(option)}
          >
            {option === "Active"
              ? "In progress"
              : option}
            <span>{count}</span>
          </button>
        );
      })}
    </div>
  );
}

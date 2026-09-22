import { useState } from "react";
import {
  FileDown,
  LoaderCircle,
} from "lucide-react";

import type {
  AcademicProject,
  ProjectDocument,
  ProjectRequirement,
} from "../api/auth";
import {
  getProjectActivities,
} from "../api/activities";
import { getProjectTasks } from "../api/tasks";

type ExportProjectReportButtonProps = {
  token: string;
  project: AcademicProject;
  documents: ProjectDocument[];
  requirements: ProjectRequirement[];
  onSessionExpired: () => void;
};

export function ExportProjectReportButton({
  token,
  project,
  documents,
  requirements,
  onSessionExpired,
}: ExportProjectReportButtonProps) {
  const [isExporting, setIsExporting] =
    useState(false);
  const [error, setError] =
    useState<string | null>(null);

  async function handleExport() {
    const reportWindow = window.open(
      "",
      "_blank"
    );

    if (!reportWindow) {
      setError("Allow pop-ups to export the report.");
      return;
    }

    reportWindow.opener = null;

    reportWindow.document.write(
      "<p style='font-family:Arial;padding:30px'>Preparing report...</p>"
    );

    setIsExporting(true);
    setError(null);

    try {
      const [tasks, activities] = await Promise.all([
        getProjectTasks(token, project.id),
        getProjectActivities(token, project.id),
      ]);

      reportWindow.document.open();
      reportWindow.document.write(
        buildReportHtml({
          project,
          documents,
          requirements,
          tasks,
          activities,
        })
      );
      reportWindow.document.close();
    } catch (exception) {
      const message =
        exception instanceof Error
          ? exception.message
          : "Unable to export the project report.";

      reportWindow.close();

      if (message === "SESSION_EXPIRED") {
        onSessionExpired();
        return;
      }

      setError(message);
    } finally {
      setIsExporting(false);
    }
  }

  return (
    <div className="export-project-report-control">
      <button
        className="export-project-report-button"
        type="button"
        onClick={() => void handleExport()}
        disabled={isExporting}
      >
        {isExporting ? (
          <LoaderCircle
            className="button-spinner"
            size={17}
          />
        ) : (
          <FileDown size={17} />
        )}
        {isExporting ? "Preparing..." : "Export report"}
      </button>

      {error && (
        <span className="export-project-report-error">
          {error}
        </span>
      )}
    </div>
  );
}

type ReportData = {
  project: AcademicProject;
  documents: ProjectDocument[];
  requirements: ProjectRequirement[];
  tasks: Awaited<ReturnType<typeof getProjectTasks>>;
  activities: Awaited<ReturnType<typeof getProjectActivities>>;
};

function buildReportHtml({
  project,
  documents,
  requirements,
  tasks,
  activities,
}: ReportData) {
  const completedRequirements = requirements.filter(
    (requirement) => requirement.isCompleted
  ).length;
  const completion = requirements.length
    ? Math.round(
        (completedRequirements / requirements.length) * 100
      )
    : 0;
  const doneTasks = tasks.filter(
    (task) => task.status === "Done"
  ).length;

  return `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>${escapeHtml(project.title)} — Project Report</title>
  <style>
    * { box-sizing: border-box; }
    body { margin: 0; color: #1d2639; background: #f4f6fb; font-family: Arial, sans-serif; }
    .actions { position: sticky; top: 0; display: flex; justify-content: flex-end; padding: 14px 28px; background: #fff; border-bottom: 1px solid #e5e8f0; }
    .actions button { padding: 10px 18px; color: #fff; border: 0; border-radius: 9px; background: #5b5ce2; font-weight: 700; cursor: pointer; }
    main { width: min(100%, 980px); margin: 30px auto; padding: 42px; background: #fff; box-shadow: 0 16px 50px rgba(35, 43, 80, .09); }
    header { padding-bottom: 28px; border-bottom: 3px solid #5b5ce2; }
    .brand { color: #5b5ce2; font-size: 13px; font-weight: 800; letter-spacing: 2px; text-transform: uppercase; }
    h1 { margin: 12px 0 8px; font-size: 34px; }
    .description { margin: 0; color: #687188; line-height: 1.6; }
    .meta { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 18px; }
    .pill { padding: 7px 11px; border-radius: 20px; color: #5158c9; background: #eeeeff; font-size: 12px; font-weight: 700; }
    .stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin: 28px 0; }
    .stat { padding: 17px; border: 1px solid #e5e8f0; border-radius: 12px; }
    .stat span { display: block; color: #7d8598; font-size: 12px; }
    .stat strong { display: block; margin-top: 7px; font-size: 24px; }
    section { margin-top: 32px; break-inside: avoid-page; }
    h2 { margin: 0 0 14px; font-size: 20px; }
    table { width: 100%; border-collapse: collapse; font-size: 12px; }
    th { color: #697286; background: #f5f6fa; text-align: left; }
    th, td { padding: 10px; border: 1px solid #e5e8f0; vertical-align: top; }
    .empty { padding: 16px; color: #81899b; border: 1px dashed #d8dce7; border-radius: 10px; }
    .activity { padding: 11px 0; border-bottom: 1px solid #edf0f5; }
    .activity strong { display: block; font-size: 13px; }
    .activity span { color: #737c91; font-size: 12px; line-height: 1.5; }
    .activity time { display: block; margin-top: 4px; color: #9aa1b1; font-size: 10px; }
    footer { margin-top: 38px; padding-top: 14px; color: #959cad; border-top: 1px solid #e5e8f0; font-size: 10px; text-align: center; }
    @media print {
      body { background: #fff; }
      .actions { display: none; }
      main { width: 100%; margin: 0; padding: 18px; box-shadow: none; }
      section { break-inside: auto; }
      tr { break-inside: avoid; }
    }
    @media (max-width: 700px) { .stats { grid-template-columns: repeat(2, 1fr); } main { margin: 0; padding: 24px; } }
  </style>
</head>
<body>
  <div class="actions"><button onclick="window.print()">Print / Save as PDF</button></div>
  <main>
    <header>
      <div class="brand">UniPilot Project Report</div>
      <h1>${escapeHtml(project.title)}</h1>
      <p class="description">${escapeHtml(project.description ?? "No project description.")}</p>
      <div class="meta">
        <span class="pill">Status: ${escapeHtml(project.status)}</span>
        <span class="pill">Due: ${formatDate(project.dueDateUtc)}</span>
        <span class="pill">Generated: ${formatDate(new Date().toISOString())}</span>
      </div>
    </header>

    <div class="stats">
      <div class="stat"><span>Documents</span><strong>${documents.length}</strong></div>
      <div class="stat"><span>Requirements</span><strong>${requirements.length}</strong></div>
      <div class="stat"><span>Requirements complete</span><strong>${completion}%</strong></div>
      <div class="stat"><span>Tasks done</span><strong>${doneTasks}/${tasks.length}</strong></div>
    </div>

    ${renderRequirements(requirements)}
    ${renderTasks(tasks)}
    ${renderDocuments(documents)}
    ${renderActivities(activities)}

    <footer>Generated by UniPilot</footer>
  </main>
</body>
</html>`;
}

function renderRequirements(requirements: ProjectRequirement[]) {
  if (!requirements.length) {
    return '<section><h2>Requirements</h2><div class="empty">No requirements.</div></section>';
  }

  const rows = requirements.map((item) => `
    <tr>
      <td>${escapeHtml(item.title)}</td>
      <td>${escapeHtml(item.type)}</td>
      <td>${escapeHtml(item.priority)}</td>
      <td>${item.isCompleted ? "Completed" : "Open"}</td>
      <td>${escapeHtml(item.description)}</td>
    </tr>`).join("");

  return `<section><h2>Requirements</h2><table><thead><tr><th>Title</th><th>Type</th><th>Priority</th><th>Status</th><th>Description</th></tr></thead><tbody>${rows}</tbody></table></section>`;
}

function renderTasks(tasks: ReportData["tasks"]) {
  if (!tasks.length) {
    return '<section><h2>Tasks</h2><div class="empty">No tasks.</div></section>';
  }

  const rows = tasks.map((item) => `
    <tr><td>${escapeHtml(item.title)}</td><td>${escapeHtml(item.status)}</td><td>${escapeHtml(item.priority)}</td><td>${formatDate(item.dueDateUtc)}</td></tr>`).join("");

  return `<section><h2>Tasks</h2><table><thead><tr><th>Title</th><th>Status</th><th>Priority</th><th>Due date</th></tr></thead><tbody>${rows}</tbody></table></section>`;
}

function renderDocuments(documents: ProjectDocument[]) {
  if (!documents.length) {
    return '<section><h2>Documents</h2><div class="empty">No documents.</div></section>';
  }

  const rows = documents.map((item) => `
    <tr><td>${escapeHtml(item.originalFileName)}</td><td>${escapeHtml(item.documentType)}</td><td>${escapeHtml(item.processingStatus)}</td><td>${item.pageCount}</td></tr>`).join("");

  return `<section><h2>Documents</h2><table><thead><tr><th>File</th><th>Type</th><th>Status</th><th>Pages</th></tr></thead><tbody>${rows}</tbody></table></section>`;
}

function renderActivities(activities: ReportData["activities"]) {
  if (!activities.length) {
    return '<section><h2>Recent activity</h2><div class="empty">No activity.</div></section>';
  }

  const items = activities.slice(0, 25).map((item) => `
    <div class="activity"><strong>${escapeHtml(item.title)}</strong><span>${escapeHtml(item.description ?? item.type)}</span><time>${formatDate(item.createdAtUtc)}</time></div>`).join("");

  return `<section><h2>Recent activity</h2>${items}</section>`;
}

function escapeHtml(value: string) {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function formatDate(value: string | null) {
  if (!value) {
    return "Not set";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Not set";
  }

  return new Intl.DateTimeFormat("en", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(date);
}

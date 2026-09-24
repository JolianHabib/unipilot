import {
  CircleAlert,
  LoaderCircle,
  Mail,
  RefreshCw,
  Trash2,
  UserPlus,
  Users,
} from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";

import {
  addProjectMember,
  getProjectMembers,
  removeProjectMember,
  resendProjectInvitation,
  updateProjectMemberRole,
  type ProjectMember,
  type ProjectMemberRole,
} from "../api/projectMembers";

type ProjectMembersPanelProps = {
  token: string;
  projectId: string;
  onSessionExpired: () => void;
};

export function ProjectMembersPanel({
  token,
  projectId,
  onSessionExpired,
}: ProjectMembersPanelProps) {
  const [members, setMembers] = useState<ProjectMember[]>([]);
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<ProjectMemberRole>("Viewer");
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [busyMemberId, setBusyMemberId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadMembers() {
      try {
        const results = await getProjectMembers(token, projectId);
        if (!cancelled) {
          setMembers(results);
          setError(null);
        }
      } catch (exception) {
        if (!cancelled) handleError(exception);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    }

    void loadMembers();
    return () => { cancelled = true; };
  }, [token, projectId]);

  function handleError(exception: unknown) {
    const message = exception instanceof Error
      ? exception.message
      : "Unable to manage project members.";

    if (message === "SESSION_EXPIRED") {
      onSessionExpired();
      return;
    }

    setSuccess(null);
    setError(message);
  }

  async function handleAdd(event: FormEvent) {
    event.preventDefault();
    if (!email.trim()) {
      setError("Enter the email address you want to invite.");
      return;
    }

    setIsSaving(true);
    setError(null);
    setSuccess(null);

    try {
      const added = await addProjectMember(token, projectId, email.trim(), role);
      setMembers((current) => [...current, added]);
      setEmail("");
      setRole("Viewer");
      setSuccess(
        added.isPending
          ? `Invitation sent to ${added.email}.`
          : `${added.fullName} was added to the project.`
      );
    } catch (exception) {
      handleError(exception);
    } finally {
      setIsSaving(false);
    }
  }

  async function handleResend(member: ProjectMember) {
    setBusyMemberId(member.id);
    setError(null);
    setSuccess(null);

    try {
      const updated = await resendProjectInvitation(
        token,
        projectId,
        member.id
      );
      setMembers((current) =>
        current.map((item) => item.id === updated.id ? updated : item)
      );
      setSuccess(`A new invitation was sent to ${updated.email}.`);
    } catch (exception) {
      handleError(exception);
    } finally {
      setBusyMemberId(null);
    }
  }

  async function handleRoleChange(
    member: ProjectMember,
    nextRole: ProjectMemberRole
  ) {
    if (member.role === nextRole) return;
    setBusyMemberId(member.id);
    setError(null);
    setSuccess(null);

    try {
      const updated = await updateProjectMemberRole(
        token,
        projectId,
        member.id,
        nextRole
      );
      setMembers((current) =>
        current.map((item) => item.id === updated.id ? updated : item)
      );
    } catch (exception) {
      handleError(exception);
    } finally {
      setBusyMemberId(null);
    }
  }

  async function handleRemove(member: ProjectMember) {
    if (!window.confirm(`Remove ${member.fullName} from this project?`)) return;
    setBusyMemberId(member.id);
    setError(null);
    setSuccess(null);

    try {
      await removeProjectMember(token, projectId, member.id);
      setMembers((current) =>
        current.filter((item) => item.id !== member.id)
      );
    } catch (exception) {
      handleError(exception);
    } finally {
      setBusyMemberId(null);
    }
  }

  return (
    <section className="project-members-panel">
      <div className="project-members-heading">
        <div>
          <h2>Project members</h2>
          <p>Invite people and control their access.</p>
        </div>
        <span>
          <Users size={17} />
          {members.length} member{members.length === 1 ? "" : "s"}
        </span>
      </div>

      <form className="project-member-form" onSubmit={handleAdd}>
        <label>
          <span>Email address</span>
          <div className="project-member-email-field">
            <Mail size={17} />
            <input
              type="email"
              value={email}
              placeholder="student@example.com"
              onChange={(event) => setEmail(event.target.value)}
            />
          </div>
        </label>
        <label>
          <span>Access</span>
          <select
            value={role}
            onChange={(event) =>
              setRole(event.target.value as ProjectMemberRole)
            }
          >
            <option value="Viewer">Viewer</option>
            <option value="Editor">Editor</option>
          </select>
        </label>
        <button type="submit" disabled={isSaving}>
          {isSaving
            ? <LoaderCircle className="button-spinner" size={17} />
            : <UserPlus size={17} />}
          {isSaving ? "Adding..." : "Add member"}
        </button>
      </form>

      {error && (
        <div className="project-members-error">
          <CircleAlert size={18} />
          {error}
        </div>
      )}

      {success && (
        <div
          role="status"
          style={{ marginTop: 12, color: "#16866b", fontWeight: 650 }}
        >
          {success}
        </div>
      )}

      {isLoading ? (
        <div className="project-members-state">
          <LoaderCircle className="button-spinner" size={22} />
          Loading members...
        </div>
      ) : members.length === 0 ? (
        <div className="project-members-state">
          <Users size={27} />
          <strong>No members yet</strong>
          <p>Invite a Viewer or Editor using their email address.</p>
        </div>
      ) : (
        <div className="project-members-list">
          {members.map((member) => {
            const isBusy = busyMemberId === member.id;
            return (
              <article className="project-member-row" key={member.id}>
                <div className="project-member-avatar">
                  {member.fullName.trim().charAt(0).toUpperCase()}
                </div>
                <div className="project-member-details">
                  <strong>{member.fullName}</strong>
                  <span>{member.email}</span>
                </div>
                <select
                  aria-label={`Access for ${member.fullName}`}
                  value={member.role}
                  disabled={isBusy}
                  onChange={(event) => void handleRoleChange(
                    member,
                    event.target.value as ProjectMemberRole
                  )}
                >
                  <option value="Viewer">Viewer</option>
                  <option value="Editor">Editor</option>
                </select>
                <div style={{ display: "flex", gap: 8 }}>
                  {member.isPending && (
                    <button
                      type="button"
                      title="Resend invitation"
                      aria-label={`Resend invitation to ${member.email}`}
                      disabled={isBusy}
                      onClick={() => void handleResend(member)}
                    >
                      {isBusy
                        ? <LoaderCircle className="button-spinner" size={17} />
                        : <RefreshCw size={17} />}
                    </button>
                  )}
                  <button
                    className="project-member-remove"
                    type="button"
                    aria-label={`Remove ${member.fullName}`}
                    disabled={isBusy}
                    onClick={() => void handleRemove(member)}
                  >
                    {isBusy
                      ? <LoaderCircle className="button-spinner" size={17} />
                      : <Trash2 size={17} />}
                  </button>
                </div>
              </article>
            );
          })}
        </div>
      )}
    </section>
  );
}

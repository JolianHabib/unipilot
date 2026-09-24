import {
  useState,
} from "react";

import {
  BrainCircuit,
  CheckCircle2,
  CircleAlert,
  MailCheck,
} from "lucide-react";

import {
  acceptProjectInvitation,
} from "../api/projectInvitations";

type ProjectInvitationPageProps = {
  token: string;
  invitationToken: string | null;
  onSessionExpired: () => void;
};

type InvitationState =
  | "ready"
  | "accepting"
  | "accepted"
  | "error";

export function ProjectInvitationPage({
  token,
  invitationToken,
  onSessionExpired,
}: ProjectInvitationPageProps) {
  const [state, setState] =
    useState<InvitationState>(
      invitationToken ? "ready" : "error"
    );

  const [error, setError] =
    useState<string | null>(
      invitationToken
        ? null
        : "This invitation link is missing its token."
    );

  async function handleAccept() {
    if (!invitationToken || state === "accepting") {
      return;
    }

    setState("accepting");
    setError(null);

    try {
      await acceptProjectInvitation(
        token,
        invitationToken
      );

      setState("accepted");
    } catch (exception) {
      const message =
        exception instanceof Error
          ? exception.message
          : "Unable to accept this invitation.";

      if (message === "SESSION_EXPIRED") {
        onSessionExpired();
        return;
      }

      setError(message);
      setState("error");
    }
  }

  function openWorkspace() {
    window.history.replaceState(
      {},
      "",
      "/"
    );

    window.location.reload();
  }

  return (
    <main className="auth-layout">
      <section className="auth-hero">
        <div className="brand">
          <div className="brand-icon">
            <BrainCircuit size={26} />
          </div>

          <span>UniPilot</span>
        </div>

        <div className="hero-content">
          <p className="eyebrow">
            Shared academic workspace
          </p>

          <h1>
            Collaborate on projects with
            <span> your team.</span>
          </h1>

          <p className="hero-description">
            Review documents, requirements,
            tasks, and project activity together
            in one organized workspace.
          </p>
        </div>
      </section>

      <section className="auth-panel">
        <div className="login-card">
          <div className="mobile-brand">
            <BrainCircuit size={25} />
            <span>UniPilot</span>
          </div>

          {state === "accepted" ? (
            <>
              <CheckCircle2
                size={42}
                color="#15956f"
              />

              <p className="eyebrow">
                Invitation accepted
              </p>

              <h2>Welcome to the project</h2>

              <p className="form-intro">
                The shared project is now available
                in your UniPilot workspace.
              </p>

              <button
                className="primary-button"
                type="button"
                onClick={openWorkspace}
              >
                Open workspace
              </button>
            </>
          ) : (
            <>
              {state === "error" ? (
                <CircleAlert
                  size={42}
                  color="#c43d32"
                />
              ) : (
                <MailCheck
                  size={42}
                  color="#5b5ce2"
                />
              )}

              <p className="eyebrow">
                Project invitation
              </p>

              <h2>Join this UniPilot project</h2>

              <p className="form-intro">
                Accept the invitation to add the
                shared project to your workspace.
              </p>

              {error && (
                <div className="auth-error">
                  {error}
                </div>
              )}

              {state !== "error" && (
                <button
                  className="primary-button"
                  type="button"
                  disabled={state === "accepting"}
                  onClick={() =>
                    void handleAccept()
                  }
                >
                  {state === "accepting"
                    ? "Accepting..."
                    : "Accept invitation"}
                </button>
              )}

              {state === "error" && (
                <button
                  className="secondary-button"
                  type="button"
                  onClick={openWorkspace}
                >
                  Return to workspace
                </button>
              )}
            </>
          )}
        </div>
      </section>
    </main>
  );
}

import {
  useState,
  type FormEvent,
} from "react";

import {
  ArrowRight,
  BrainCircuit,
  FileCheck2,
  KanbanSquare,
} from "lucide-react";

import { login } from "../api/auth";

type LoginPageProps = {
  onLogin: (token: string) => void;
};

export function LoginPage({
  onLogin,
}: LoginPageProps) {
  const [email, setEmail] = useState("");
  const [password, setPassword] =
    useState("");

  const [error, setError] = useState("");
  const [isLoading, setIsLoading] =
    useState(false);

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setError("");
    setIsLoading(true);

    try {
      const token = await login(
        email.trim(),
        password
      );

      onLogin(token);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to sign in."
      );
    } finally {
      setIsLoading(false);
    }
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
            AI-powered academic workspace
          </p>

          <h1>
            Turn project documents into
            <span> clear action.</span>
          </h1>

          <p className="hero-description">
            Upload specifications and rubrics.
            UniPilot extracts requirements,
            organizes priorities, and helps you
            track progress from brief to delivery.
          </p>

          <div className="feature-list">
            <div className="feature">
              <FileCheck2 size={21} />

              <div>
                <strong>
                  Understand every document
                </strong>

                <span>
                  AI extracts structured,
                  traceable requirements.
                </span>
              </div>
            </div>

            <div className="feature">
              <KanbanSquare size={21} />

              <div>
                <strong>
                  Keep work organized
                </strong>

                <span>
                  Track requirements and progress
                  in one workspace.
                </span>
              </div>
            </div>
          </div>
        </div>

        <p className="hero-footer">
          Built for university projects and
          student teams.
        </p>
      </section>

      <section className="auth-panel">
        <div className="login-card">
          <div className="mobile-brand">
            <BrainCircuit size={25} />
            <span>UniPilot</span>
          </div>

          <p className="eyebrow">
            Welcome back
          </p>

          <h2>Sign in to your workspace</h2>

          <p className="form-intro">
            Continue managing your courses,
            projects, and requirements.
          </p>

          <form onSubmit={handleSubmit}>
            <label htmlFor="email">
              Email address
            </label>

            <input
              id="email"
              type="email"
              autoComplete="email"
              placeholder="you@example.com"
              value={email}
              onChange={(event) =>
                setEmail(event.target.value)
              }
              required
            />

            <label htmlFor="password">
              Password
            </label>

            <input
              id="password"
              type="password"
              autoComplete="current-password"
              placeholder="Enter your password"
              value={password}
              onChange={(event) =>
                setPassword(event.target.value)
              }
              required
            />

            {error && (
              <div className="form-error">
                {error}
              </div>
            )}

            <button
              type="submit"
              className="primary-button"
              disabled={isLoading}
            >
              <span>
                {isLoading
                  ? "Signing in..."
                  : "Sign in"}
              </span>

              {!isLoading && (
                <ArrowRight size={18} />
              )}
            </button>
          </form>

          <p className="register-note">
            New to UniPilot? Registration is
            coming next.
          </p>
        </div>
      </section>
    </main>
  );
}
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

import {
  login,
  register,
} from "../api/auth";

type LoginPageProps = {
  onLogin: (token: string) => void;
};

type AuthMode = "login" | "register";

export function LoginPage({
  onLogin,
}: LoginPageProps) {
  const [mode, setMode] =
    useState<AuthMode>("login");

  const [fullName, setFullName] =
    useState("");

  const [email, setEmail] =
    useState("");

  const [password, setPassword] =
    useState("");

  const [
    confirmPassword,
    setConfirmPassword,
  ] = useState("");

  const [error, setError] =
    useState("");

  const [isLoading, setIsLoading] =
    useState(false);

  const isRegisterMode =
    mode === "register";

  function changeMode(nextMode: AuthMode) {
    setMode(nextMode);
    setError("");
    setPassword("");
    setConfirmPassword("");
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setError("");

    const normalizedEmail =
      email.trim().toLowerCase();

    if (isRegisterMode) {
      if (!fullName.trim()) {
        setError("Please enter your full name.");
        return;
      }

      if (password.length < 8) {
        setError(
          "Password must contain at least 8 characters."
        );
        return;
      }

      if (password !== confirmPassword) {
        setError("Passwords do not match.");
        return;
      }
    }

    setIsLoading(true);

    try {
      if (isRegisterMode) {
        await register(
          fullName.trim(),
          normalizedEmail,
          password
        );
      }

      const accessToken = await login(
        normalizedEmail,
        password
      );

      onLogin(accessToken);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : isRegisterMode
            ? "Unable to create your account."
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
            {isRegisterMode
              ? "Create your account"
              : "Welcome back"}
          </p>

          <h2>
            {isRegisterMode
              ? "Start your UniPilot workspace"
              : "Sign in to your workspace"}
          </h2>

          <p className="form-intro">
            {isRegisterMode
              ? "Create an account to organize your courses, projects, and requirements."
              : "Continue managing your courses, projects, and requirements."}
          </p>

          <form onSubmit={handleSubmit}>
            {isRegisterMode && (
              <>
                <label htmlFor="fullName">
                  Full name
                </label>

                <input
                  id="fullName"
                  type="text"
                  autoComplete="name"
                  placeholder="Your full name"
                  value={fullName}
                  onChange={(event) =>
                    setFullName(
                      event.target.value
                    )
                  }
                  disabled={isLoading}
                  required
                />
              </>
            )}

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
              disabled={isLoading}
              required
            />

            <label htmlFor="password">
              Password
            </label>

            <input
              id="password"
              type="password"
              autoComplete={
                isRegisterMode
                  ? "new-password"
                  : "current-password"
              }
              placeholder={
                isRegisterMode
                  ? "At least 8 characters"
                  : "Enter your password"
              }
              value={password}
              onChange={(event) =>
                setPassword(
                  event.target.value
                )
              }
              disabled={isLoading}
              minLength={
                isRegisterMode ? 8 : undefined
              }
              required
            />

            {isRegisterMode && (
              <>
                <label htmlFor="confirmPassword">
                  Confirm password
                </label>

                <input
                  id="confirmPassword"
                  type="password"
                  autoComplete="new-password"
                  placeholder="Enter your password again"
                  value={confirmPassword}
                  onChange={(event) =>
                    setConfirmPassword(
                      event.target.value
                    )
                  }
                  disabled={isLoading}
                  minLength={8}
                  required
                />
              </>
            )}

            {error && (
              <div
                className="form-error"
                role="alert"
              >
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
                  ? isRegisterMode
                    ? "Creating account..."
                    : "Signing in..."
                  : isRegisterMode
                    ? "Create account"
                    : "Sign in"}
              </span>

              {!isLoading && (
                <ArrowRight size={18} />
              )}
            </button>
          </form>

          <p className="register-note">
            {isRegisterMode
              ? "Already have an account?"
              : "New to UniPilot?"}

            <button
              className="auth-switch-button"
              type="button"
              onClick={() =>
                changeMode(
                  isRegisterMode
                    ? "login"
                    : "register"
                )
              }
              disabled={isLoading}
            >
              {isRegisterMode
                ? "Sign in"
                : "Create an account"}
            </button>
          </p>
        </div>
      </section>
    </main>
  );
}
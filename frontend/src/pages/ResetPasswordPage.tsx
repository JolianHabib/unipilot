import {
  useState,
  type FormEvent,
} from "react";

import {
  ArrowLeft,
  ArrowRight,
  BrainCircuit,
  CheckCircle2,
  KeyRound,
  LoaderCircle,
} from "lucide-react";

import {
  resetPassword,
} from "../api/passwordReset";

export function ResetPasswordPage() {
  const parameters =
    new URLSearchParams(
      window.location.search
    );

  const email =
    parameters.get("email") ?? "";

  const token =
    parameters.get("token") ?? "";

  const [password, setPassword] =
    useState("");

  const [
    confirmPassword,
    setConfirmPassword,
  ] = useState("");

  const [error, setError] = useState("");

  const [isLoading, setIsLoading] =
    useState(false);

  const [isSuccessful, setIsSuccessful] =
    useState(false);

  const hasValidLink =
    email.length > 0 && token.length > 0;

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setError("");

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

    setIsLoading(true);

    try {
      await resetPassword(
        email,
        token,
        password
      );

      setIsSuccessful(true);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to reset your password."
      );
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <main className="password-page">
      <section className="password-card">
        <a
          className="password-brand"
          href="/"
        >
          <span className="password-brand-icon">
            <BrainCircuit size={24} />
          </span>

          <span>UniPilot</span>
        </a>

        {isSuccessful ? (
          <>
            <div className="password-page-icon success">
              <CheckCircle2 size={29} />
            </div>

            <p className="eyebrow">
              Password updated
            </p>

            <h1>Your password was reset</h1>

            <p className="password-description">
              You can now sign in to UniPilot
              using your new password.
            </p>

            <a
              className="primary-button password-action-link"
              href="/"
            >
              Continue to sign in
              <ArrowRight size={18} />
            </a>
          </>
        ) : !hasValidLink ? (
          <>
            <div className="password-page-icon error">
              <KeyRound size={28} />
            </div>

            <p className="eyebrow">
              Invalid reset link
            </p>

            <h1>This link cannot be used</h1>

            <p className="password-description">
              The password reset link is incomplete
              or invalid. Request a new link and try
              again.
            </p>

            <a
              className="primary-button password-action-link"
              href="/forgot-password"
            >
              Request a new link
              <ArrowRight size={18} />
            </a>
          </>
        ) : (
          <>
            <div className="password-page-icon">
              <KeyRound size={28} />
            </div>

            <p className="eyebrow">
              Secure your account
            </p>

            <h1>Create a new password</h1>

            <p className="password-description">
              Enter a new password for{" "}
              <strong>{email}</strong>.
            </p>

            <form onSubmit={handleSubmit}>
              <label htmlFor="new-password">
                New password
              </label>

              <input
                id="new-password"
                type="password"
                autoComplete="new-password"
                placeholder="At least 8 characters"
                value={password}
                onChange={(event) =>
                  setPassword(
                    event.target.value
                  )
                }
                disabled={isLoading}
                minLength={8}
                required
                autoFocus
              />

              <label htmlFor="confirm-new-password">
                Confirm new password
              </label>

              <input
                id="confirm-new-password"
                type="password"
                autoComplete="new-password"
                placeholder="Enter the password again"
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

              {error && (
                <div
                  className="form-error"
                  role="alert"
                >
                  {error}
                </div>
              )}

              <button
                className="primary-button"
                type="submit"
                disabled={isLoading}
              >
                {isLoading ? (
                  <>
                    <LoaderCircle
                      className="button-spinner"
                      size={18}
                    />

                    Resetting...
                  </>
                ) : (
                  <>
                    Reset password
                    <ArrowRight size={18} />
                  </>
                )}
              </button>
            </form>

            <a
              className="back-to-login"
              href="/"
            >
              <ArrowLeft size={17} />
              Back to sign in
            </a>
          </>
        )}
      </section>
    </main>
  );
}
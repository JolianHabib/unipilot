import {
  useState,
  type FormEvent,
} from "react";

import {
  ArrowLeft,
  BrainCircuit,
  LoaderCircle,
  Mail,
  Send,
} from "lucide-react";

import {
  forgotPassword,
} from "../api/passwordReset";

export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [error, setError] = useState("");
  const [message, setMessage] =
    useState("");

  const [isLoading, setIsLoading] =
    useState(false);

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setError("");
    setMessage("");
    setIsLoading(true);

    try {
      const result = await forgotPassword(
        email
      );

      setMessage(result);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to send the reset email."
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

        <div className="password-page-icon">
          <Mail size={28} />
        </div>

        <p className="eyebrow">
          Account recovery
        </p>

        <h1>Forgot your password?</h1>

        <p className="password-description">
          Enter your account email and we will
          send you a secure password reset link.
        </p>

        {message ? (
          <div className="password-success">
            <Mail size={22} />

            <div>
              <strong>Check your inbox</strong>
              <p>{message}</p>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <label htmlFor="reset-email">
              Email address
            </label>

            <input
              id="reset-email"
              type="email"
              autoComplete="email"
              placeholder="you@example.com"
              value={email}
              onChange={(event) =>
                setEmail(event.target.value)
              }
              disabled={isLoading}
              required
              autoFocus
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

                  Sending...
                </>
              ) : (
                <>
                  Send reset link
                  <Send size={18} />
                </>
              )}
            </button>
          </form>
        )}

        <a
          className="back-to-login"
          href="/"
        >
          <ArrowLeft size={17} />
          Back to sign in
        </a>
      </section>
    </main>
  );
}
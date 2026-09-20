import {
  useEffect,
  useState,
  type FormEvent,
} from "react";

import {
  CheckCircle2,
  KeyRound,
  LoaderCircle,
  UserRound,
  X,
} from "lucide-react";

import {
  changePassword,
  updateProfile,
} from "../api/account";

import type {
  CurrentUser,
} from "../api/auth";

type ProfileModalProps = {
  isOpen: boolean;
  token: string;
  user: CurrentUser;
  onClose: () => void;
  onUserUpdated: (
    user: CurrentUser
  ) => void;
  onSessionExpired: () => void;
};

export function ProfileModal({
  isOpen,
  token,
  user,
  onClose,
  onUserUpdated,
  onSessionExpired,
}: ProfileModalProps) {
  const [fullName, setFullName] =
    useState(user.fullName);

  const [
    currentPassword,
    setCurrentPassword,
  ] = useState("");

  const [
    newPassword,
    setNewPassword,
  ] = useState("");

  const [
    confirmPassword,
    setConfirmPassword,
  ] = useState("");

  const [
    isSavingProfile,
    setIsSavingProfile,
  ] = useState(false);

  const [
    isChangingPassword,
    setIsChangingPassword,
  ] = useState(false);

  const [profileError, setProfileError] =
    useState<string | null>(null);

  const [
    passwordError,
    setPasswordError,
  ] = useState<string | null>(null);

  const [
    profileSuccess,
    setProfileSuccess,
  ] = useState(false);

  const [
    passwordSuccess,
    setPasswordSuccess,
  ] = useState(false);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setFullName(user.fullName);
    setCurrentPassword("");
    setNewPassword("");
    setConfirmPassword("");
    setProfileError(null);
    setPasswordError(null);
    setProfileSuccess(false);
    setPasswordSuccess(false);
  }, [
    isOpen,
    user.fullName,
  ]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    function handleEscape(
      event: KeyboardEvent
    ) {
      if (
        event.key === "Escape" &&
        !isSavingProfile &&
        !isChangingPassword
      ) {
        onClose();
      }
    }

    document.addEventListener(
      "keydown",
      handleEscape
    );

    return () => {
      document.removeEventListener(
        "keydown",
        handleEscape
      );
    };
  }, [
    isOpen,
    isSavingProfile,
    isChangingPassword,
    onClose,
  ]);

  if (!isOpen) {
    return null;
  }

  async function handleProfileSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    const trimmedName = fullName.trim();

    if (!trimmedName) {
      setProfileError(
        "Full name is required."
      );
      return;
    }

    setIsSavingProfile(true);
    setProfileError(null);
    setProfileSuccess(false);

    try {
      const updatedUser =
        await updateProfile(
          token,
          trimmedName
        );

      onUserUpdated(updatedUser);
      setFullName(updatedUser.fullName);
      setProfileSuccess(true);
    } catch (exception) {
      if (
        exception instanceof Error &&
        exception.message ===
          "SESSION_EXPIRED"
      ) {
        onSessionExpired();
        return;
      }

      setProfileError(
        exception instanceof Error
          ? exception.message
          : "Unable to update your profile."
      );
    } finally {
      setIsSavingProfile(false);
    }
  }

  async function handlePasswordSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setPasswordError(null);
    setPasswordSuccess(false);

    if (newPassword.length < 8) {
      setPasswordError(
        "New password must contain at least 8 characters."
      );
      return;
    }

    if (
      newPassword !== confirmPassword
    ) {
      setPasswordError(
        "The new passwords do not match."
      );
      return;
    }

    if (
      currentPassword === newPassword
    ) {
      setPasswordError(
        "New password must be different from the current password."
      );
      return;
    }

    setIsChangingPassword(true);

    try {
      await changePassword(
        token,
        currentPassword,
        newPassword
      );

      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setPasswordSuccess(true);
    } catch (exception) {
      if (
        exception instanceof Error &&
        exception.message ===
          "SESSION_EXPIRED"
      ) {
        onSessionExpired();
        return;
      }

      setPasswordError(
        exception instanceof Error
          ? exception.message
          : "Unable to change your password."
      );
    } finally {
      setIsChangingPassword(false);
    }
  }

  const initials = user.fullName
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) =>
      part[0].toUpperCase()
    )
    .join("");

  return (
    <div
      className="modal-backdrop"
      onMouseDown={(event) => {
        if (
          event.target ===
            event.currentTarget &&
          !isSavingProfile &&
          !isChangingPassword
        ) {
          onClose();
        }
      }}
    >
      <section
        className="profile-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="profile-modal-title"
      >
        <header className="course-modal-header">
          <div className="modal-heading-icon">
            <UserRound size={22} />
          </div>

          <div>
            <h2 id="profile-modal-title">
              Profile settings
            </h2>

            <p>
              Manage your personal information
              and password.
            </p>
          </div>

          <button
            className="modal-close-button"
            type="button"
            onClick={onClose}
            disabled={
              isSavingProfile ||
              isChangingPassword
            }
            aria-label="Close"
          >
            <X size={20} />
          </button>
        </header>

        <div className="profile-modal-body">
          <div className="profile-summary">
            <div className="profile-summary-avatar">
              {initials}
            </div>

            <div>
              <strong>
                {user.fullName}
              </strong>

              <span>{user.email}</span>
            </div>
          </div>

          <form
            className="profile-settings-form"
            onSubmit={
              handleProfileSubmit
            }
          >
            <div className="profile-form-heading">
              <UserRound size={18} />

              <div>
                <h3>Personal information</h3>
                <p>
                  Update the name displayed in
                  your workspace.
                </p>
              </div>
            </div>

            <div className="form-field">
              <label htmlFor="profile-name">
                Full name
              </label>

              <input
                id="profile-name"
                value={fullName}
                onChange={(event) => {
                  setFullName(
                    event.target.value
                  );
                  setProfileSuccess(false);
                }}
                maxLength={100}
                disabled={isSavingProfile}
                required
              />
            </div>

            <div className="form-field">
              <label htmlFor="profile-email">
                Email address
              </label>

              <input
                id="profile-email"
                value={user.email}
                disabled
              />
            </div>

            {profileError && (
              <p
                className="modal-error"
                role="alert"
              >
                {profileError}
              </p>
            )}

            {profileSuccess && (
              <p className="modal-success">
                <CheckCircle2 size={17} />
                Profile updated successfully.
              </p>
            )}

            <button
              className="create-button profile-save-button"
              type="submit"
              disabled={
                isSavingProfile ||
                fullName.trim() ===
                  user.fullName
              }
            >
              {isSavingProfile ? (
                <>
                  <LoaderCircle
                    className="button-spinner"
                    size={17}
                  />
                  Saving...
                </>
              ) : (
                "Save profile"
              )}
            </button>
          </form>

          <div className="profile-divider" />

          <form
            className="profile-settings-form"
            onSubmit={
              handlePasswordSubmit
            }
          >
            <div className="profile-form-heading">
              <KeyRound size={18} />

              <div>
                <h3>Change password</h3>
                <p>
                  Use at least 8 characters for
                  your new password.
                </p>
              </div>
            </div>

            <div className="form-field">
              <label htmlFor="current-password">
                Current password
              </label>

              <input
                id="current-password"
                type="password"
                autoComplete="current-password"
                value={currentPassword}
                onChange={(event) =>
                  setCurrentPassword(
                    event.target.value
                  )
                }
                disabled={
                  isChangingPassword
                }
                required
              />
            </div>

            <div className="form-field">
              <label htmlFor="new-password">
                New password
              </label>

              <input
                id="new-password"
                type="password"
                autoComplete="new-password"
                value={newPassword}
                onChange={(event) =>
                  setNewPassword(
                    event.target.value
                  )
                }
                minLength={8}
                disabled={
                  isChangingPassword
                }
                required
              />
            </div>

            <div className="form-field">
              <label htmlFor="confirm-password">
                Confirm new password
              </label>

              <input
                id="confirm-password"
                type="password"
                autoComplete="new-password"
                value={confirmPassword}
                onChange={(event) =>
                  setConfirmPassword(
                    event.target.value
                  )
                }
                minLength={8}
                disabled={
                  isChangingPassword
                }
                required
              />
            </div>

            {passwordError && (
              <p
                className="modal-error"
                role="alert"
              >
                {passwordError}
              </p>
            )}

            {passwordSuccess && (
              <p className="modal-success">
                <CheckCircle2 size={17} />
                Password changed successfully.
              </p>
            )}

            <button
              className="create-button profile-save-button"
              type="submit"
              disabled={
                isChangingPassword
              }
            >
              {isChangingPassword ? (
                <>
                  <LoaderCircle
                    className="button-spinner"
                    size={17}
                  />
                  Changing...
                </>
              ) : (
                "Change password"
              )}
            </button>
          </form>
        </div>
      </section>
    </div>
  );
}
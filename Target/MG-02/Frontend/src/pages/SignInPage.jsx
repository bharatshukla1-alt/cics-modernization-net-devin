import { useRef, useState } from 'react';
import Alert from '@mg01/components/Alert.jsx';
import TextField from '@mg01/components/TextField.jsx';
import { shellApi } from '../api/shellApi.js';

const MESSAGES = {
  USER_NOT_FOUND: 'User not found. Try again.',
  WRONG_PASSWORD: 'Wrong password. Try again.',
  UNABLE_TO_VERIFY: 'Unable to verify the user. Try again later.',
  TOO_MANY_REQUESTS: 'Too many sign-in attempts. Wait a minute and try again.',
};

/** Sign in (legacy sign-on screen). Validation order: user ID, then password. */
export default function SignInPage({ onSignedIn }) {
  const [userId, setUserId] = useState('');
  const [password, setPassword] = useState('');
  const [fieldErrors, setFieldErrors] = useState({});
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);
  const userRef = useRef(null);
  const passwordRef = useRef(null);

  const submit = async (e) => {
    e.preventDefault();
    setError(null);
    if (!userId.trim()) {
      setFieldErrors({ userId: 'Enter your user ID.' });
      userRef.current?.focus();
      return;
    }
    if (!password.trim()) {
      setFieldErrors({ password: 'Enter your password.' });
      passwordRef.current?.focus();
      return;
    }
    setFieldErrors({});
    setBusy(true);
    try {
      const result = await shellApi.signIn(userId.trim().toUpperCase(), password);
      onSignedIn(result);
    } catch (err) {
      setError(MESSAGES[err.code] ?? err.message);
      if (err.code === 'WRONG_PASSWORD') {
        setPassword('');
        passwordRef.current?.focus();
      } else {
        userRef.current?.focus();
      }
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="card max-w-[420px] mx-auto">
      <div className="card-body">
        <h1 className="text-xl m-0 mb-sp-2">Sign in to CardDemo</h1>
        <p className="text-muted m-0 mb-sp-4">Use your CardDemo user ID and password.</p>
        <Alert variant="danger">{error}</Alert>
        <form onSubmit={submit} noValidate className="flex flex-col gap-sp-3">
          <TextField
            ref={userRef}
            id="userId"
            label="User ID"
            value={userId}
            onChange={(v) => setUserId(v.toUpperCase())}
            error={fieldErrors.userId}
            maxLength={8}
            required
            autoFocus
          />
          <div className="form-group">
            <label htmlFor="password">
              Password<span className="text-muted font-normal"> (required)</span>
            </label>
            <input
              ref={passwordRef}
              id="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              maxLength={8}
              required
              aria-invalid={fieldErrors.password ? 'true' : undefined}
              aria-describedby={fieldErrors.password ? 'password-error' : undefined}
              className={fieldErrors.password ? 'is-invalid' : undefined}
            />
            {fieldErrors.password && (
              <span id="password-error" className="form-error">
                {fieldErrors.password}
              </span>
            )}
          </div>
          <button type="submit" className="btn btn-primary btn-block" disabled={busy}>
            {busy ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
        <div className="mt-sp-4">
          <Alert variant="warning">Demo environment: legacy password check. Do not use real credentials.</Alert>
        </div>
      </div>
    </div>
  );
}

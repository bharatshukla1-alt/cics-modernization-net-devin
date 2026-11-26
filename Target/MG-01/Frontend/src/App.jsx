import { useEffect, useRef, useState } from 'react';
import { Link, Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import TransactionTypeListPage from './screens/TransactionTypeListPage.jsx';
import TransactionTypeAddPage from './screens/TransactionTypeAddPage.jsx';
import TransactionTypeDetailPage from './screens/TransactionTypeDetailPage.jsx';
import ConfirmDialog from './components/ConfirmDialog.jsx';
import { useIdleTimeout } from './hooks/useIdleTimeout.js';
import Alert from './components/Alert.jsx';
import { clearSession, hasSession } from './api/transactionTypesApi.js';
import { completeSignIn, oidcConfigured, signIn, signOut } from './auth/oidc.js';

const IDLE_LIMIT_MS = 15 * 60 * 1000;
const WARNING_MS = 60 * 1000;

function AdminPlaceholder() {
  return (
    <div className="card">
      <div className="card-body">
        <h1 className="text-xl m-0 mb-sp-3">Admin menu</h1>
        <p className="text-muted">The admin menu is not available yet.</p>
        <Link className="btn btn-secondary" to="/transaction-types">Go to transaction types</Link>
      </div>
    </div>
  );
}

function SignedOut() {
  return (
    <div className="card">
      <div className="card-body">
        <h1 className="text-xl m-0 mb-sp-3">You have been signed out</h1>
        <p className="text-muted">Your session ended after a period of inactivity.</p>
        <a className="btn btn-primary" href="/transaction-types">Sign in again</a>
      </div>
    </div>
  );
}

function SignIn() {
  const location = useLocation();
  const [error, setError] = useState(null);
  const start = () => signIn(`${location.pathname}${location.search}`).catch((e) => setError(e.message));
  return (
    <div className="card max-w-form">
      <div className="card-body flex flex-col gap-sp-4">
        <h1 className="text-xl m-0">Sign in</h1>
        <p className="text-muted m-0">Sign in with your organisation account to maintain transaction types.</p>
        <Alert variant="danger">{oidcConfigured ? error : 'Sign-in is not configured for this environment.'}</Alert>
        <button type="button" className="btn btn-primary self-start" onClick={start} disabled={!oidcConfigured}>
          Sign in
        </button>
      </div>
    </div>
  );
}

function AuthCallback({ onSignedIn }) {
  const location = useLocation();
  const navigate = useNavigate();
  const [error, setError] = useState(null);
  const started = useRef(false);

  useEffect(() => {
    if (started.current) return;
    started.current = true;
    completeSignIn(location.search)
      .then((returnTo) => {
        onSignedIn();
        navigate(returnTo, { replace: true });
      })
      .catch((e) => setError(e.message));
  }, [location.search, navigate, onSignedIn]);

  return error ? <Alert variant="danger">{error}</Alert> : <p className="text-muted" role="status">Signing you in…</p>;
}

/** App shell: OAuth 2.0 sign-in gate, idle timeout and routes for S1–S3. */
export default function App() {
  const { warning, signedOut, stayActive } = useIdleTimeout(IDLE_LIMIT_MS, WARNING_MS, clearSession);
  const [authenticated, setAuthenticated] = useState(hasSession());
  const location = useLocation();
  const onSignedIn = () => setAuthenticated(true);
  const onSignOut = async () => {
    setAuthenticated(false);
    await signOut();
  };

  let content;
  if (location.pathname === '/auth/callback') content = <AuthCallback onSignedIn={onSignedIn} />;
  else if (signedOut) content = <SignedOut />;
  else if (!authenticated) content = <SignIn />;
  else {
    content = (
      <Routes>
        <Route path="/" element={<Navigate to="/transaction-types" replace />} />
        <Route path="/transaction-types" element={<TransactionTypeListPage />} />
        <Route path="/transaction-types/new" element={<TransactionTypeAddPage />} />
        <Route path="/transaction-types/:typeCode" element={<TransactionTypeDetailPage />} />
        <Route path="/admin" element={<AdminPlaceholder />} />
        <Route path="*" element={<Navigate to="/transaction-types" replace />} />
      </Routes>
    );
  }

  return (
    <>
      <header className="app-header">
        <span className="brand">
          <span className="brand-mark" aria-hidden="true">C</span>
          CardDemo Administration
        </span>
        <nav aria-label="Main">
          <Link to="/transaction-types">Transaction types</Link>
          {authenticated && !signedOut && (
            <button type="button" className="btn btn-ghost btn-sm !text-white/90 hover:!bg-white/10 hover:!text-white" onClick={onSignOut}>Sign out</button>
          )}
        </nav>
      </header>
      <main className="container mx-auto max-w-container px-sp-4 py-sp-6">
        {content}
      </main>
      {warning && !signedOut && (
        <ConfirmDialog
          title="Are you still there?"
          message="You will be signed out in one minute because of inactivity."
          confirmLabel="Stay signed in"
          cancelLabel="Sign out now"
          onConfirm={stayActive}
          onCancel={() => stayActive(true)}
        />
      )}
    </>
  );
}

import { useCallback, useState } from 'react';
import { Link, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import TransactionTypeListPage from '@mg01/screens/TransactionTypeListPage.jsx';
import TransactionTypeAddPage from '@mg01/screens/TransactionTypeAddPage.jsx';
import TransactionTypeDetailPage from '@mg01/screens/TransactionTypeDetailPage.jsx';
import ConfirmDialog from '@mg01/components/ConfirmDialog.jsx';
import { useIdleTimeout } from '@mg01/hooks/useIdleTimeout.js';
import UserListPage from '@mg03/screens/UserListPage.jsx';
import UserAddPage from '@mg03/screens/UserAddPage.jsx';
import UserUpdatePage from '@mg03/screens/UserUpdatePage.jsx';
import UserDeletePage from '@mg03/screens/UserDeletePage.jsx';
import { endSession, getSession, homeFor, isAdmin, startSession } from './auth/session.js';
import SignInPage from './pages/SignInPage.jsx';
import MenuPage from './pages/MenuPage.jsx';

const IDLE_LIMIT_MS = 15 * 60 * 1000;
const WARNING_MS = 60 * 1000;

function SignedOutPage() {
  return (
    <div className="card max-w-[420px] mx-auto">
      <div className="card-body">
        <h1 className="text-xl m-0 mb-sp-3">Thank you for using CardDemo.</h1>
        <p className="text-muted">You are signed out.</p>
        <a className="btn btn-primary" href="/signin">Sign in again</a>
      </div>
    </div>
  );
}

/** CardDemo shell: sign-in, role menus and the migrated function screens. */
export default function App() {
  const navigate = useNavigate();
  const [user, setUser] = useState(getSession()?.user ?? null);

  const signOut = useCallback(() => {
    endSession();
    setUser(null);
    navigate('/signed-out', { replace: true });
  }, [navigate]);
  const expire = useCallback(() => {
    endSession();
    setUser(null);
  }, []);
  const { warning, signedOut, stayActive } = useIdleTimeout(IDLE_LIMIT_MS, WARNING_MS, expire);

  const onSignedIn = (result) => {
    startSession(result);
    setUser(result.user);
    navigate(homeFor(result.user), { replace: true });
  };

  const guard = (needAdmin, element) => {
    if (!user) return <Navigate to="/signin" replace />;
    if (needAdmin !== null && needAdmin !== isAdmin(user)) return <Navigate to={homeFor(user)} replace />;
    return element;
  };

  return (
    <>
      <header className="app-header">
        <span className="brand">
          <span className="brand-mark" aria-hidden="true">C</span>
          CardDemo
        </span>
        {user && (
          <nav aria-label="Account" className="flex items-center gap-sp-3">
            <Link to={homeFor(user)}>{isAdmin(user) ? 'Administration' : 'Customer services'}</Link>
            <span className="text-white/90" data-testid="signed-in-user">
              {user.firstName} {user.lastName}
            </span>
            <button type="button" className="btn btn-ghost btn-sm !text-white/90 hover:!bg-white/10 hover:!text-white" onClick={signOut}>
              Sign out
            </button>
          </nav>
        )}
      </header>
      <main className="container mx-auto max-w-container px-sp-4 py-sp-6">
        {signedOut && !user ? (
          <SignedOutPage />
        ) : (
          <Routes>
            <Route path="/" element={<Navigate to={user ? homeFor(user) : '/signin'} replace />} />
            <Route path="/signin" element={user ? <Navigate to={homeFor(user)} replace /> : <SignInPage onSignedIn={onSignedIn} />} />
            <Route path="/signed-out" element={<SignedOutPage />} />
            <Route path="/menu" element={guard(false, <MenuPage kind="main" onUnauthorized={signOut} />)} />
            <Route path="/admin" element={guard(true, <MenuPage kind="admin" onUnauthorized={signOut} />)} />
            <Route path="/admin/users" element={guard(true, <UserListPage />)} />
            <Route path="/admin/users/new" element={guard(true, <UserAddPage />)} />
            <Route path="/admin/users/update" element={guard(true, <UserUpdatePage />)} />
            <Route path="/admin/users/update/:userId" element={guard(true, <UserUpdatePage key="update" />)} />
            <Route path="/admin/users/delete" element={guard(true, <UserDeletePage />)} />
            <Route path="/admin/users/delete/:userId" element={guard(true, <UserDeletePage key="delete" />)} />
            <Route path="/transaction-types" element={guard(true, <TransactionTypeListPage />)} />
            <Route path="/transaction-types/new" element={guard(true, <TransactionTypeAddPage />)} />
            <Route path="/transaction-types/:typeCode" element={guard(true, <TransactionTypeDetailPage />)} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        )}
      </main>
      {warning && user && (
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

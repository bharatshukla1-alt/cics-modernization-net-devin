import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import Alert from '@mg01/components/Alert.jsx';
import ConfirmDialog from '@mg01/components/ConfirmDialog.jsx';
import { usersApi } from '../api/usersApi.js';
import FetchBar from '../components/FetchBar.jsx';
import { typeLabel } from '../components/UserFields.jsx';
import { useFetchedUser } from './useFetchedUser.js';

/** CU03 / COUSR03C: fetch a user, show it read-only and delete on confirmation (PF5). */
export default function UserDeletePage() {
  const { userId } = useParams();
  const navigate = useNavigate();
  const { user, setUser, error, setError, busy, fetchUser } = useFetchedUser(userId);
  const [confirming, setConfirming] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [success, setSuccess] = useState('');

  const remove = async () => {
    setDeleting(true);
    try {
      const result = await usersApi.remove(user.userId, user.version);
      setUser(null);
      setSuccess(result.message);
    } catch (e) {
      setError(e.message);
    } finally {
      setDeleting(false);
      setConfirming(false);
    }
  };

  return (
    <div className="card max-w-form">
      <div className="card-header">
        <Link to="/admin" className="btn btn-ghost btn-sm px-0">← Back to Administration</Link>
        <h1 className="m-0 text-xl text-primary-900">Delete User</h1>
      </div>
      <div className="card-body flex flex-col gap-sp-4">
        <FetchBar key={userId ?? ''} initial={userId ?? ''} busy={busy} onFetch={(id) => { setSuccess(''); fetchUser(id); }} />
        <Alert variant="success">{success}</Alert>
        <Alert variant="danger">{error}</Alert>
        {user && (
          <>
            <Alert variant="info">Press Delete to delete this user ...</Alert>
            <dl className="grid grid-cols-1 gap-x-sp-4 md:grid-cols-12" data-testid="user-details">
              <div className="md:col-span-6"><dt className="text-muted text-sm">First name</dt><dd className="m-0">{user.firstName}</dd></div>
              <div className="md:col-span-6"><dt className="text-muted text-sm">Last name</dt><dd className="m-0">{user.lastName}</dd></div>
              <div className="md:col-span-6"><dt className="text-muted text-sm">User type</dt><dd className="m-0">{typeLabel(user.userType)} ({user.userType})</dd></div>
            </dl>
          </>
        )}
      </div>
      <div className="card-footer flex justify-end gap-sp-3">
        <button type="button" className="btn btn-secondary" onClick={() => navigate('/admin/users')}>Back to list</button>
        {user && <button type="button" className="btn btn-danger" onClick={() => setConfirming(true)}>Delete</button>}
      </div>
      {confirming && (
        <ConfirmDialog
          title={`Delete user ${user.userId}?`}
          message="This removes the user's sign-on access. This cannot be undone."
          confirmLabel="Delete"
          danger
          busy={deleting}
          onConfirm={remove}
          onCancel={() => setConfirming(false)}
        />
      )}
    </div>
  );
}

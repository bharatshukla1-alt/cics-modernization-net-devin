import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import Alert from '@mg01/components/Alert.jsx';
import { usersApi } from '../api/usersApi.js';
import FetchBar from '../components/FetchBar.jsx';
import { Field, TypeSelect, firstMissing } from '../components/UserFields.jsx';
import { useFetchedUser } from './useFetchedUser.js';

const ORDER = ['firstName', 'lastName', 'userType'];

/** CU02 / COUSR02C: fetch a user, edit and save (PF5) or save and exit (PF3). */
export default function UserUpdatePage() {
  const { userId } = useParams();
  const navigate = useNavigate();
  const { user, setUser, error, setError, busy, fetchUser } = useFetchedUser(userId);
  const [form, setForm] = useState(null);
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setForm(user ? { firstName: user.firstName, lastName: user.lastName, password: '', userType: user.userType } : null);
  }, [user]);

  const set = (field) => (value) => setForm((f) => ({ ...f, [field]: value }));

  const save = async (exit) => {
    setSuccess('');
    const missing = firstMissing(form, ORDER);
    if (missing) return setError(missing.message);
    setSaving(true);
    try {
      const result = await usersApi.update(user.userId, form, user.version);
      setError('');
      if (exit) return navigate('/admin', { replace: true, state: { message: result.message } });
      setUser(result.user);
      setSuccess(result.message);
    } catch (e) {
      setError(e.message);
    } finally {
      setSaving(false);
    }
    return undefined;
  };

  return (
    <div className="card max-w-form">
      <div className="card-header">
        <Link to="/admin" className="btn btn-ghost btn-sm px-0">← Back to Administration</Link>
        <h1 className="m-0 text-xl text-primary-900">Update User</h1>
      </div>
      <div className="card-body flex flex-col gap-sp-4">
        <FetchBar key={userId ?? ''} initial={userId ?? ''} busy={busy} onFetch={(id) => { setSuccess(''); fetchUser(id); }} />
        <Alert variant="success">{success}</Alert>
        <Alert variant="danger">{error}</Alert>
        {form && (
          <>
            <Alert variant="info">{success ? '' : 'Press Save to save your updates ...'}</Alert>
            <div className="grid grid-cols-1 gap-x-sp-4 md:grid-cols-12">
              <div className="md:col-span-6"><Field id="u-firstName" label="First name" value={form.firstName} onChange={set('firstName')} maxLength={20} /></div>
              <div className="md:col-span-6"><Field id="u-lastName" label="Last name" value={form.lastName} onChange={set('lastName')} maxLength={20} /></div>
              <div className="md:col-span-6"><Field id="u-password" label="Password" type="password" value={form.password} onChange={set('password')} maxLength={8} hint="Leave blank to keep the current password" /></div>
              <div className="md:col-span-6"><TypeSelect id="u-userType" value={form.userType} onChange={set('userType')} /></div>
            </div>
          </>
        )}
      </div>
      {form && (
        <div className="card-footer flex justify-end gap-sp-3">
          <button type="button" className="btn btn-secondary" onClick={() => navigate('/admin/users')}>Cancel</button>
          <button type="button" className="btn btn-secondary" disabled={saving} onClick={() => save(true)}>Save &amp; exit</button>
          <button type="button" className="btn btn-primary" disabled={saving} onClick={() => save(false)}>Save</button>
        </div>
      )}
    </div>
  );
}

import { useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import Alert from '@mg01/components/Alert.jsx';
import { usersApi } from '../api/usersApi.js';
import { Field, TypeSelect, firstMissing } from '../components/UserFields.jsx';

const EMPTY = { firstName: '', lastName: '', userId: '', password: '', userType: '' };
const ORDER = ['firstName', 'lastName', 'userId', 'password', 'userType'];
const SERVER_FIELD = { USER_ID_EXISTS: 'userId', INVALID_USER_ID: 'userId', INVALID_USER_TYPE: 'userType' };

/** CU01 / COUSR01C: add a user. */
export default function UserAddPage() {
  const [form, setForm] = useState(EMPTY);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState('');
  const [busy, setBusy] = useState(false);
  const refs = { firstName: useRef(), lastName: useRef(), userId: useRef(), password: useRef(), userType: useRef() };
  const set = (field) => (value) => setForm((f) => ({ ...f, [field]: value }));
  const fail = (field, message) => {
    setError({ field, message });
    refs[field]?.current?.focus();
  };

  const submit = async (e) => {
    e.preventDefault();
    setSuccess('');
    const missing = firstMissing(form, ORDER);
    if (missing) return fail(missing.field, missing.message);
    setBusy(true);
    try {
      const result = await usersApi.create(form);
      setForm(EMPTY);
      setError(null);
      setSuccess(result.message);
      refs.firstName.current?.focus();
    } catch (ex) {
      fail(SERVER_FIELD[ex.code] ?? 'firstName', ex.message);
    } finally {
      setBusy(false);
    }
    return undefined;
  };

  const err = (field) => (error?.field === field ? error.message : undefined);
  return (
    <form className="card max-w-form" onSubmit={submit} noValidate>
      <div className="card-header">
        <Link to="/admin" className="btn btn-ghost btn-sm px-0">← Back to Administration</Link>
        <h1 className="m-0 text-xl text-primary-900">Add User</h1>
      </div>
      <div className="card-body flex flex-col gap-sp-4">
        <Alert variant="success">{success}</Alert>
        <Alert variant="danger">{error?.message}</Alert>
        <div className="grid grid-cols-1 gap-x-sp-4 md:grid-cols-12">
          <div className="md:col-span-6"><Field id="firstName" label="First name" value={form.firstName} onChange={set('firstName')} maxLength={20} error={err('firstName')} inputRef={refs.firstName} /></div>
          <div className="md:col-span-6"><Field id="lastName" label="Last name" value={form.lastName} onChange={set('lastName')} maxLength={20} error={err('lastName')} inputRef={refs.lastName} /></div>
          <div className="md:col-span-6"><Field id="userId" label="User ID" value={form.userId} onChange={set('userId')} maxLength={8} upper hint="8 characters" error={err('userId')} inputRef={refs.userId} /></div>
          <div className="md:col-span-6"><Field id="password" label="Password" type="password" value={form.password} onChange={set('password')} maxLength={8} hint="8 characters" error={err('password')} inputRef={refs.password} /></div>
          <div className="md:col-span-6"><TypeSelect id="userType" value={form.userType} onChange={set('userType')} error={err('userType')} inputRef={refs.userType} /></div>
        </div>
      </div>
      <div className="card-footer flex justify-end gap-sp-3">
        <button type="button" className="btn btn-secondary" onClick={() => { setForm(EMPTY); setError(null); setSuccess(''); }}>Clear</button>
        <button type="submit" className="btn btn-primary" disabled={busy}>Add user</button>
      </div>
    </form>
  );
}

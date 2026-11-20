import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import Alert from '@mg01/components/Alert.jsx';
import { usersApi } from '../api/usersApi.js';
import { typeLabel } from '../components/UserFields.jsx';

/** CU00 / COUSR00C: list users 10 per page with search, paging and row actions. */
export default function UserListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(null);
  const [pageNum, setPageNum] = useState(1);
  const [info, setInfo] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const load = useCallback(async (params, nextPage) => {
    setBusy(true);
    setError('');
    setInfo('');
    try {
      const result = await usersApi.list(params);
      if (result.message) {
        setInfo(result.message);
      } else {
        setPage(result);
        setPageNum(nextPage);
      }
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }, []);

  useEffect(() => {
    load({}, 1);
  }, [load]);

  const onSearch = (e) => {
    e.preventDefault();
    load({ search: search.trim() }, 1);
  };

  return (
    <div className="stack flex flex-col gap-sp-5">
      <div className="flex flex-wrap items-center justify-between gap-sp-3">
        <div>
          <Link to="/admin" className="btn btn-ghost btn-sm px-0">← Back to Administration</Link>
          <h1 className="m-0 text-xl text-primary-900">List Users</h1>
        </div>
        <Link to="/admin/users/new" className="btn btn-primary">Add user</Link>
      </div>
      <form className="grid grid-cols-1 gap-x-sp-4 md:grid-cols-12" onSubmit={onSearch} role="search">
        <div className="form-group md:col-span-4">
          <label htmlFor="user-search">Search user ID</label>
          <input id="user-search" type="text" value={search} maxLength={8} onChange={(e) => setSearch(e.target.value.toUpperCase())} />
        </div>
        <div className="md:col-span-2 md:pt-[1.85rem]">
          <button type="submit" className="btn btn-secondary" disabled={busy}>Search</button>
        </div>
      </form>
      <Alert variant="danger">{error}</Alert>
      <Alert variant="info">{info}</Alert>
      <div className="card">
        <div className="table-wrap">
          <table className="data-table">
            <caption className="visually-hidden">Users, page {pageNum}</caption>
            <thead>
              <tr>
                <th>User ID</th>
                <th>First name</th>
                <th>Last name</th>
                <th>Type</th>
                <th className="text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {page?.users.length === 0 && (
                <tr><td colSpan={5} className="text-center text-muted">No users found.</td></tr>
              )}
              {page?.users.map((u) => (
                <tr key={u.userId}>
                  <td className="font-mono">{u.userId}</td>
                  <td>{u.firstName}</td>
                  <td>{u.lastName}</td>
                  <td><span className={`badge ${u.userType === 'A' ? 'badge--info' : 'badge--neutral'}`}>{typeLabel(u.userType)}</span></td>
                  <td className="text-right">
                    <span className="inline-flex gap-sp-2">
                      <button type="button" className="btn btn-secondary btn-sm" aria-label={`Update ${u.userId}`}
                        onClick={() => navigate(`/admin/users/update/${u.userId}`)}>Update</button>
                      <button type="button" className="btn btn-secondary btn-sm" aria-label={`Delete ${u.userId}`}
                        onClick={() => navigate(`/admin/users/delete/${u.userId}`)}>Delete</button>
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="card-footer flex items-center justify-between">
          <span className="text-muted text-sm" data-testid="page-number">Page {pageNum}</span>
          <span className="flex gap-sp-3">
            <button type="button" className="btn btn-secondary btn-sm" disabled={busy || !page?.hasPrevious}
              onClick={() => load({ before: page.firstUserId }, Math.max(1, pageNum - 1))}>Previous</button>
            <button type="button" className="btn btn-secondary btn-sm" disabled={busy || !page?.hasNext}
              onClick={() => load({ after: page.lastUserId }, pageNum + 1)}>Next</button>
          </span>
        </div>
      </div>
    </div>
  );
}

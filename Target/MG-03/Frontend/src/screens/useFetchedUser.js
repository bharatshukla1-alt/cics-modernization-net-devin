import { useCallback, useEffect, useState } from 'react';
import { usersApi } from '../api/usersApi.js';

/** COUSR02C/COUSR03C READ-USER-SEC-FILE: fetch the selected or typed user. */
export function useFetchedUser(initialId) {
  const [user, setUser] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const fetchUser = useCallback(async (id) => {
    setError('');
    if (!String(id ?? '').trim()) {
      setUser(null);
      setError('User ID can NOT be empty...');
      return null;
    }
    setBusy(true);
    try {
      const result = await usersApi.get(id.trim().toUpperCase());
      setUser(result);
      return result;
    } catch (e) {
      setUser(null);
      setError(e.message);
      return null;
    } finally {
      setBusy(false);
    }
  }, []);

  useEffect(() => {
    if (initialId) fetchUser(initialId);
  }, [initialId, fetchUser]);

  return { user, setUser, error, setError, busy, fetchUser };
}

import { getSession } from '../auth/session.js';

const API_BASE = import.meta.env?.VITE_API_BASE ?? '/api/v1';

export class ShellApiError extends Error {
  constructor(status, code, message) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

async function request(path, options = {}) {
  const headers = { Accept: 'application/json' };
  if (options.body) headers['Content-Type'] = 'application/json';
  const token = getSession()?.accessToken;
  if (token) headers.Authorization = `Bearer ${token}`;
  let response;
  try {
    response = await fetch(`${API_BASE}${path}`, { ...options, headers });
  } catch {
    throw new ShellApiError(0, 'NETWORK', 'The service is not reachable. Try again later.');
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    throw new ShellApiError(response.status, body?.code ?? `HTTP_${response.status}`, body?.message ?? 'Request failed.');
  }
  return body;
}

export const shellApi = {
  signIn: (userId, password) => request('/auth/sign-in', { method: 'POST', body: JSON.stringify({ userId, password }) }),
  menu: (kind) => request(`/menus/${kind}`),
};

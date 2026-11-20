/** UserAdminService client (MG-03). The shell sets the bearer token after sign-in. */
const API_BASE = import.meta.env?.VITE_API_BASE ?? '/api/v1';
let accessToken = null;
let onUnauthorized = null;

export function setAccessToken(token) {
  accessToken = token;
}

export function clearSession() {
  accessToken = null;
}

export function setUnauthorizedHandler(handler) {
  onUnauthorized = handler;
}

export class UsersApiError extends Error {
  constructor(status, code, message) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

async function request(path, { method = 'GET', body, version } = {}) {
  const headers = { Accept: 'application/json' };
  if (body) headers['Content-Type'] = 'application/json';
  if (accessToken) headers.Authorization = `Bearer ${accessToken}`;
  if (version !== undefined) headers['If-Match'] = `"${version}"`;
  let response;
  try {
    response = await fetch(`${API_BASE}${path}`, { method, headers, body: body ? JSON.stringify(body) : undefined });
  } catch {
    throw new UsersApiError(0, 'NETWORK', 'The user service is not reachable. Try again later.');
  }
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    if (response.status === 401) onUnauthorized?.();
    throw new UsersApiError(response.status, data?.code ?? `HTTP_${response.status}`, data?.message ?? 'Request failed.');
  }
  return data;
}

const query = (params) => {
  const q = new URLSearchParams(Object.entries(params).filter(([, v]) => v));
  return q.toString() ? `?${q}` : '';
};

export const usersApi = {
  list: ({ search, after, before } = {}) => request(`/users${query({ search, after, before })}`),
  get: (userId) => request(`/users/${encodeURIComponent(userId)}`),
  create: (user) => request('/users', { method: 'POST', body: user }),
  update: (userId, user, version) => request(`/users/${encodeURIComponent(userId)}`, { method: 'PUT', body: user, version }),
  remove: (userId, version) => request(`/users/${encodeURIComponent(userId)}`, { method: 'DELETE', version }),
};

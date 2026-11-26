const API_BASE = import.meta.env?.VITE_API_BASE ?? '/api/v1';
const DEV_BYPASS = Boolean(import.meta.env?.DEV) && import.meta.env?.VITE_AUTH_BYPASS === 'true';
const DEV_TOKEN = 'dummy-dev-token';

let accessToken = null;

/** Token is held in memory only (UI spec §10). */
export function setAccessToken(token) {
  accessToken = token;
}

export function clearSession() {
  accessToken = null;
}

/** True when requests will carry a bearer token (signed in, or local dev bypass). */
export function hasSession() {
  return accessToken !== null || DEV_BYPASS;
}

export class ApiError extends Error {
  constructor({ status, code, message, errors, traceId }) {
    super(message);
    this.status = status;
    this.code = code;
    this.errors = errors ?? {};
    this.traceId = traceId;
  }
}

const MESSAGES = {
  network: 'Cannot reach the service. Check your connection and try again.',
  401: 'Sign in to continue.',
  403: 'You do not have permission to maintain transaction types.',
  503: 'The service is temporarily unavailable. Try again later.',
  default: 'Something went wrong. Please try again.',
};

async function request(path, { method = 'GET', body } = {}) {
  const headers = { Accept: 'application/json' };
  const token = accessToken ?? (DEV_BYPASS ? DEV_TOKEN : null);
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  let response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError({ status: 0, code: 'NETWORK', message: MESSAGES.network });
  }

  if (response.status === 204) return null;
  const isJson = (response.headers.get('content-type') ?? '').includes('json');
  const data = isJson ? await response.json() : null;
  if (!response.ok) {
    throw new ApiError({
      status: response.status,
      code: data?.code ?? `HTTP_${response.status}`,
      message: data?.message ?? MESSAGES[response.status] ?? MESSAGES.default,
      errors: data?.errors,
      traceId: data?.traceId,
    });
  }
  return data;
}

function query(params) {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') search.set(k, v);
  });
  const s = search.toString();
  return s ? `?${s}` : '';
}

export const transactionTypesApi = {
  list: ({ typeCode, description, after, before, pageSize = 7 } = {}) =>
    request(`/transaction-types${query({ typeCode, description, after, before, pageSize })}`),
  get: (typeCode) => request(`/transaction-types/${encodeURIComponent(typeCode)}`),
  create: (typeCode, description) =>
    request('/transaction-types', { method: 'POST', body: { typeCode, description } }),
  update: (typeCode, description) =>
    request(`/transaction-types/${encodeURIComponent(typeCode)}`, { method: 'PUT', body: { description } }),
  remove: (typeCode) => request(`/transaction-types/${encodeURIComponent(typeCode)}`, { method: 'DELETE' }),
};

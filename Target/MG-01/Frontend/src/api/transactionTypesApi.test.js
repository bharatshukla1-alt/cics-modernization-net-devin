import { afterEach, describe, expect, it, vi } from 'vitest';
import { clearSession, hasSession, setAccessToken, transactionTypesApi } from './transactionTypesApi.js';

const reply = (status, body, contentType = 'application/problem+json') => ({
  status,
  ok: status < 400,
  headers: { get: () => contentType },
  json: async () => body,
});

afterEach(() => {
  vi.unstubAllGlobals();
  clearSession();
});

describe('transactionTypesApi', () => {
  it('sends the in-memory bearer token and only non-empty query parameters', async () => {
    const fetchMock = vi.fn().mockResolvedValue(reply(200, { items: [] }, 'application/json'));
    vi.stubGlobal('fetch', fetchMock);
    expect(hasSession()).toBe(false);
    setAccessToken('token-1');
    expect(hasSession()).toBe(true);
    await transactionTypesApi.list({ typeCode: '05', description: '' });
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe('/api/v1/transaction-types?typeCode=05&pageSize=7');
    expect(init.headers.Authorization).toBe('Bearer token-1');
  });

  it('sends JSON bodies for create and update and encodes the code in the path', async () => {
    const fetchMock = vi.fn().mockResolvedValue(reply(200, { typeCode: '05', description: 'X' }, 'application/json'));
    vi.stubGlobal('fetch', fetchMock);
    await transactionTypesApi.create('05', 'X');
    await transactionTypesApi.update('05', 'Y');
    await transactionTypesApi.get('0/5');
    expect(fetchMock.mock.calls[0][1]).toMatchObject({ method: 'POST', body: '{"typeCode":"05","description":"X"}' });
    expect(fetchMock.mock.calls[1][1]).toMatchObject({ method: 'PUT', body: '{"description":"Y"}' });
    expect(fetchMock.mock.calls[2][0]).toBe('/api/v1/transaction-types/0%2F5');
  });

  it('returns null for 204 responses', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(reply(204, null)));
    await expect(transactionTypesApi.remove('01')).resolves.toBeNull();
  });

  it('normalises problem details into ApiError', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(reply(409, { code: 'HAS_DEPENDENT_RECORDS', message: 'Child rows exist.', traceId: 't1' })));
    await expect(transactionTypesApi.remove('01')).rejects.toMatchObject({
      status: 409, code: 'HAS_DEPENDENT_RECORDS', message: 'Child rows exist.', traceId: 't1',
    });
  });

  it('falls back to friendly messages for non-JSON and network failures', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(reply(503, null, 'text/html')));
    await expect(transactionTypesApi.get('01')).rejects.toMatchObject({
      code: 'HTTP_503', message: 'The service is temporarily unavailable. Try again later.',
    });
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('offline')));
    await expect(transactionTypesApi.get('01')).rejects.toMatchObject({ status: 0, code: 'NETWORK' });
  });
});

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const originalLocation = window.location;

describe('OIDC Authorization Code + PKCE flow', () => {
  let oidc;
  let api;
  let assign;

  beforeEach(async () => {
    vi.resetModules();
    vi.stubEnv('VITE_OIDC_AUTHORITY', 'https://idp.test/realms/carddemo');
    vi.stubEnv('VITE_OIDC_CLIENT_ID', 'carddemo-ui');
    assign = vi.fn();
    Object.defineProperty(window, 'location', { configurable: true, value: { origin: 'http://localhost:5173', assign } });
    vi.stubGlobal('fetch', vi.fn(async (url) => ({
      ok: true,
      json: async () => (String(url).endsWith('openid-configuration')
        ? { authorization_endpoint: 'https://idp.test/auth', token_endpoint: 'https://idp.test/token', end_session_endpoint: 'https://idp.test/logout' }
        : { access_token: 'access-1' }),
    })));
    oidc = await import('./oidc.js');
    api = await import('../api/transactionTypesApi.js');
  });

  afterEach(() => {
    Object.defineProperty(window, 'location', { configurable: true, value: originalLocation });
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
    sessionStorage.clear();
  });

  it('redirects with an S256 challenge and exchanges the code for an in-memory token', async () => {
    expect(oidc.oidcConfigured).toBe(true);
    await oidc.signIn('/transaction-types/05');
    const url = new URL(assign.mock.calls[0][0]);
    expect(url.origin + url.pathname).toBe('https://idp.test/auth');
    expect(url.searchParams.get('code_challenge_method')).toBe('S256');
    expect(url.searchParams.get('redirect_uri')).toBe('http://localhost:5173/auth/callback');

    const returnTo = await oidc.completeSignIn(`?code=abc&state=${url.searchParams.get('state')}`);
    expect(returnTo).toBe('/transaction-types/05');
    expect(api.hasSession()).toBe(true);
    const tokenCall = fetch.mock.calls.find(([u]) => u === 'https://idp.test/token');
    expect(String(tokenCall[1].body)).toContain('code_verifier=');

    await oidc.signOut();
    expect(api.hasSession()).toBe(false);
    expect(assign.mock.calls[1][0]).toContain('https://idp.test/logout');
  });

  it('rejects a callback whose state does not match', async () => {
    await oidc.signIn('/transaction-types');
    await expect(oidc.completeSignIn('?code=abc&state=forged')).rejects.toThrow('Sign-in could not be verified.');
    expect(api.hasSession()).toBe(false);
  });
});

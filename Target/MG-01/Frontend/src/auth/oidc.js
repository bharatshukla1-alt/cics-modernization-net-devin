import { clearSession, setAccessToken } from '../api/transactionTypesApi.js';

const AUTHORITY = import.meta.env?.VITE_OIDC_AUTHORITY;
const CLIENT_ID = import.meta.env?.VITE_OIDC_CLIENT_ID;
const SCOPE = import.meta.env?.VITE_OIDC_SCOPE ?? 'openid transaction-types.read transaction-types.write';
const PKCE_KEY = 'oidc.pkce';
const CALLBACK_PATH = '/auth/callback';

/** True when an OAuth 2.0 / OIDC provider is configured for interactive sign-in. */
export const oidcConfigured = Boolean(AUTHORITY && CLIENT_ID);

/** Only same-origin relative paths are allowed as post-login targets (prevents open redirects). */
export function safeReturnTo(path) {
  return typeof path === 'string' && path.startsWith('/') && !path.startsWith('//') && !path.includes('\\')
    ? path
    : '/transaction-types';
}

function base64Url(bytes) {
  return btoa(String.fromCharCode(...new Uint8Array(bytes))).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function randomString() {
  return base64Url(crypto.getRandomValues(new Uint8Array(32)));
}

async function discover() {
  const response = await fetch(`${AUTHORITY.replace(/\/$/, '')}/.well-known/openid-configuration`);
  if (!response.ok) throw new Error('Sign-in service is unavailable.');
  return response.json();
}

/** Starts the Authorization Code + PKCE flow (S256). */
export async function signIn(returnTo) {
  const { authorization_endpoint: authorize } = await discover();
  const verifier = randomString();
  const state = randomString();
  const challenge = base64Url(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier)));
  sessionStorage.setItem(PKCE_KEY, JSON.stringify({ verifier, state, returnTo: safeReturnTo(returnTo) }));
  const params = new URLSearchParams({
    response_type: 'code',
    client_id: CLIENT_ID,
    redirect_uri: `${window.location.origin}${CALLBACK_PATH}`,
    scope: SCOPE,
    state,
    code_challenge: challenge,
    code_challenge_method: 'S256',
  });
  window.location.assign(`${authorize}?${params}`);
}

/** Completes the flow on the callback route; keeps the access token in memory only. Returns the safe return path. */
export async function completeSignIn(search) {
  const params = new URLSearchParams(search);
  const saved = JSON.parse(sessionStorage.getItem(PKCE_KEY) ?? 'null');
  sessionStorage.removeItem(PKCE_KEY);
  if (params.get('error')) throw new Error('Sign-in was cancelled or refused.');
  if (!saved || !params.get('code') || params.get('state') !== saved.state) throw new Error('Sign-in could not be verified.');

  const { token_endpoint: tokenEndpoint } = await discover();
  const response = await fetch(tokenEndpoint, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'authorization_code',
      code: params.get('code'),
      redirect_uri: `${window.location.origin}${CALLBACK_PATH}`,
      client_id: CLIENT_ID,
      code_verifier: saved.verifier,
    }),
  });
  if (!response.ok) throw new Error('Sign-in could not be completed.');
  const { access_token: accessToken } = await response.json();
  setAccessToken(accessToken);
  return safeReturnTo(saved.returnTo);
}

/** Clears the in-memory token and ends the provider session when supported. */
export async function signOut() {
  clearSession();
  if (!oidcConfigured) return;
  try {
    const { end_session_endpoint: endSession } = await discover();
    if (endSession) {
      const params = new URLSearchParams({ client_id: CLIENT_ID, post_logout_redirect_uri: window.location.origin });
      window.location.assign(`${endSession}?${params}`);
    }
  } catch {
    // Local session is already cleared.
  }
}

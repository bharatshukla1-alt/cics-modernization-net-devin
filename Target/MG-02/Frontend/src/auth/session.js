import { clearSession as clearMg01Session, setAccessToken } from '@mg01/api/transactionTypesApi.js';
import { clearSession as clearMg03Session, setAccessToken as setMg03AccessToken } from '@mg03/api/usersApi.js';

/** In-memory session (no web storage). Shares the token with the hosted MG-01 and MG-03 screens. */
let current = null;

export function getSession() {
  return current;
}

export function startSession({ accessToken, user }) {
  current = { accessToken, user };
  setAccessToken(accessToken);
  setMg03AccessToken(accessToken);
}

export function endSession() {
  current = null;
  clearMg01Session();
  clearMg03Session();
}

export const isAdmin = (user) => user?.userType === 'A';
export const homeFor = (user) => (isAdmin(user) ? '/admin' : '/menu');

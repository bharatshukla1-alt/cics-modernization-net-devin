import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import App from './App.jsx';
import { endSession } from './auth/session.js';

const ADMIN = { accessToken: 'admin-token', tokenType: 'Bearer', expiresIn: 900, user: { userId: 'ADMIN001', firstName: 'MARGARET', lastName: 'GOLD', userType: 'A' } };
const USER = { ...ADMIN, accessToken: 'user-token', user: { userId: 'USER0001', firstName: 'LAWRENCE', lastName: 'THOMAS', userType: 'U' } };
const menu = (title, n) => ({ title, options: Array.from({ length: n }, (_, i) => ({ number: i + 1, name: `Option ${i + 1}`, route: null, status: 'NOT_INSTALLED' })) });

function stubApi(signIn) {
  const fn = vi.fn(async (url, init) => {
    if (url.endsWith('/auth/sign-in')) return { ok: true, status: 200, json: async () => signIn };
    if (url.endsWith('/menus/admin')) return { ok: true, status: 200, json: async () => menu('Administration', 6) };
    if (url.endsWith('/menus/main')) return { ok: true, status: 200, json: async () => menu('Customer services', 11) };
    return { ok: true, status: 200, json: async () => ({ items: [], hasNext: false, hasPrevious: false, init }) };
  });
  vi.stubGlobal('fetch', fn);
  return fn;
}

async function signInAs(result, path = '/') {
  const fetch = stubApi(result);
  render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  );
  await userEvent.type(screen.getByLabelText(/User ID/), result.user.userId);
  await userEvent.type(screen.getByLabelText(/Password/), 'PASSWORD');
  await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
  return fetch;
}

afterEach(() => {
  endSession();
  vi.unstubAllGlobals();
});

describe('App shell', () => {
  it('redirects protected routes to sign-in without a session', () => {
    stubApi(ADMIN);
    render(
      <MemoryRouter initialEntries={['/admin']}>
        <App />
      </MemoryRouter>,
    );
    expect(screen.getByRole('heading', { name: 'Sign in to CardDemo' })).toBeInTheDocument();
  });

  it('routes an administrator to the administration menu with a bearer token', async () => {
    const fetch = await signInAs(ADMIN);
    expect(await screen.findByRole('heading', { name: 'Administration' })).toBeInTheDocument();
    expect(screen.getAllByRole('listitem')).toHaveLength(6);
    expect(screen.getByTestId('signed-in-user')).toHaveTextContent('MARGARET GOLD');
    const menuCall = fetch.mock.calls.find(([url]) => url.endsWith('/menus/admin'));
    expect(menuCall[1].headers.Authorization).toBe('Bearer admin-token');
  });

  it('routes a regular user to customer services and keeps them out of admin screens', async () => {
    await signInAs(USER, '/transaction-types');
    expect(await screen.findByRole('heading', { name: 'Customer services' })).toBeInTheDocument();
    expect(screen.getAllByRole('listitem')).toHaveLength(11);
  });

  it('signs out to the thank-you page', async () => {
    await signInAs(ADMIN);
    await screen.findByRole('heading', { name: 'Administration' });
    await userEvent.click(screen.getByRole('button', { name: 'Sign out' }));
    expect(screen.getByRole('heading', { name: 'Thank you for using CardDemo.' })).toBeInTheDocument();
    expect(screen.queryByTestId('signed-in-user')).not.toBeInTheDocument();
  });
});

import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';
import App from './App.jsx';
import { clearSession, setAccessToken } from './api/transactionTypesApi.js';

const renderAt = (path) => render(<MemoryRouter initialEntries={[path]}><App /></MemoryRouter>);

describe('App sign-in gate', () => {
  afterEach(() => clearSession());

  it('shows the sign-in screen when there is no session', () => {
    renderAt('/transaction-types');
    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
    expect(screen.getByText('Sign-in is not configured for this environment.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeDisabled();
  });

  it('reports an unverifiable callback', async () => {
    renderAt('/auth/callback?code=x&state=y');
    expect(await screen.findByText('Sign-in could not be verified.')).toBeInTheDocument();
  });

  it('renders routes for a signed-in user and signs out', async () => {
    setAccessToken('token-1');
    renderAt('/admin');
    expect(screen.getByRole('heading', { name: 'Admin menu' })).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Sign out' }));
    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
  });
});

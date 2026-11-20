import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import MenuPage from './MenuPage.jsx';

const MENU = {
  title: 'Administration',
  options: [
    { number: 1, name: 'User List', route: null, status: 'NOT_INSTALLED' },
    { number: 2, name: 'Later', route: null, status: 'COMING_SOON' },
    { number: 3, name: 'Secret', route: null, status: 'ADMIN_ONLY' },
    { number: 5, name: 'Transaction Type List/Update', route: '/transaction-types', status: 'AVAILABLE' },
  ],
};

function renderMenu(status = 200, body = MENU, onUnauthorized = vi.fn()) {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: status < 400, status, json: async () => body }));
  render(
    <MemoryRouter initialEntries={['/admin']}>
      <Routes>
        <Route path="/admin" element={<MenuPage kind="admin" onUnauthorized={onUnauthorized} />} />
        <Route path="/transaction-types" element={<p>Transaction types screen</p>} />
      </Routes>
    </MemoryRouter>,
  );
  return onUnauthorized;
}

afterEach(() => vi.unstubAllGlobals());

describe('MenuPage', () => {
  it('renders the title and one tile per option', async () => {
    renderMenu();
    expect(await screen.findByRole('heading', { name: 'Administration' })).toBeInTheDocument();
    expect(screen.getAllByRole('listitem')).toHaveLength(4);
    expect(fetch.mock.calls[0][0]).toBe('/api/v1/menus/admin');
  });

  it('opens an available option', async () => {
    renderMenu();
    await userEvent.click(await screen.findByRole('button', { name: /Transaction Type List/ }));
    expect(screen.getByText('Transaction types screen')).toBeInTheDocument();
  });

  it.each([
    [/User List/, 'This option User List is not installed yet.'],
    [/Later/, 'This option Later is coming soon.'],
    [/Secret/, 'No access - admin only option.'],
  ])('explains unavailable option %s', async (name, message) => {
    renderMenu();
    await userEvent.click(await screen.findByRole('button', { name }));
    expect(screen.getByText(message)).toBeInTheDocument();
  });

  it('quick option validates the range and selects by number', async () => {
    renderMenu();
    const input = await screen.findByLabelText('Go to option');
    for (const bad of ['0', '9']) {
      await userEvent.clear(input);
      await userEvent.type(input, bad);
      await userEvent.click(screen.getByRole('button', { name: 'Go' }));
      expect(screen.getByText('Enter a valid option number.')).toBeInTheDocument();
    }
    await userEvent.clear(input);
    await userEvent.click(screen.getByRole('button', { name: 'Go' }));
    expect(screen.getByText('Enter a valid option number.')).toBeInTheDocument();
    await userEvent.type(input, '1');
    await userEvent.click(screen.getByRole('button', { name: 'Go' }));
    expect(screen.getByText('This option User List is not installed yet.')).toBeInTheDocument();
  });

  it('signs out on 401 and shows other errors', async () => {
    const onUnauthorized = renderMenu(401, { code: 'UNAUTHENTICATED', message: 'Sign in to continue.' });
    await vi.waitFor(() => expect(onUnauthorized).toHaveBeenCalled());
  });

  it('shows a load error', async () => {
    renderMenu(500, { code: 'INTERNAL_ERROR', message: 'An unexpected error occurred.' });
    expect(await screen.findByText('An unexpected error occurred.')).toBeInTheDocument();
  });
});

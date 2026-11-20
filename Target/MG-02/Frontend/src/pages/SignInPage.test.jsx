import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import SignInPage from './SignInPage.jsx';

function mockFetch(status, body) {
  const fn = vi.fn().mockResolvedValue({ ok: status < 400, status, json: async () => body });
  vi.stubGlobal('fetch', fn);
  return fn;
}

afterEach(() => vi.unstubAllGlobals());

describe('SignInPage', () => {
  it('requires the user ID before the password', async () => {
    const fetch = mockFetch(200, {});
    render(<SignInPage onSignedIn={vi.fn()} />);
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
    expect(screen.getByText('Enter your user ID.')).toBeInTheDocument();
    expect(screen.getByLabelText(/User ID/)).toHaveFocus();
    await userEvent.type(screen.getByLabelText(/User ID/), 'user0001');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
    expect(screen.getByText('Enter your password.')).toBeInTheDocument();
    expect(screen.getByLabelText(/Password/)).toHaveFocus();
    expect(fetch).not.toHaveBeenCalled();
  });

  it('upper-cases the user ID and reports success', async () => {
    const result = { accessToken: 't', user: { userId: 'USER0001', userType: 'U' } };
    const fetch = mockFetch(200, result);
    const onSignedIn = vi.fn();
    render(<SignInPage onSignedIn={onSignedIn} />);
    await userEvent.type(screen.getByLabelText(/User ID/), 'user0001');
    await userEvent.type(screen.getByLabelText(/Password/), 'password');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
    expect(JSON.parse(fetch.mock.calls[0][1].body)).toEqual({ userId: 'USER0001', password: 'password' });
    expect(onSignedIn).toHaveBeenCalledWith(result);
  });

  it.each([
    ['USER_NOT_FOUND', 'User not found. Try again.', /User ID/],
    ['WRONG_PASSWORD', 'Wrong password. Try again.', /Password/],
    ['UNABLE_TO_VERIFY', 'Unable to verify the user. Try again later.', /User ID/],
  ])('maps %s to its message and focus', async (code, message, focus) => {
    mockFetch(code === 'UNABLE_TO_VERIFY' ? 503 : 401, { code, message: 'x' });
    render(<SignInPage onSignedIn={vi.fn()} />);
    await userEvent.type(screen.getByLabelText(/User ID/), 'USER0001');
    await userEvent.type(screen.getByLabelText(/Password/), 'WRONG');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByLabelText(focus)).toHaveFocus();
    if (code === 'WRONG_PASSWORD') expect(screen.getByLabelText(/Password/)).toHaveValue('');
  });

  it('reports an unreachable service', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('down')));
    render(<SignInPage onSignedIn={vi.fn()} />);
    await userEvent.type(screen.getByLabelText(/User ID/), 'USER0001');
    await userEvent.type(screen.getByLabelText(/Password/), 'PASSWORD');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
    expect(await screen.findByText('The service is not reachable. Try again later.')).toBeInTheDocument();
  });

  it('shows the demo notice and masks the password', () => {
    render(<SignInPage onSignedIn={vi.fn()} />);
    expect(screen.getByText(/Demo environment/)).toBeInTheDocument();
    expect(screen.getByLabelText(/Password/)).toHaveAttribute('type', 'password');
  });
});

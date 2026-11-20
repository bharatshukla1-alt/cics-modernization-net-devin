import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import UserListPage from '@mg03/screens/UserListPage.jsx';
import UserAddPage from '@mg03/screens/UserAddPage.jsx';
import UserUpdatePage from '@mg03/screens/UserUpdatePage.jsx';
import UserDeletePage from '@mg03/screens/UserDeletePage.jsx';
import { setAccessToken } from '@mg03/api/usersApi.js';

const json = (status, body, headers = {}) => Promise.resolve(new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json', ...headers } }));
const users = (from, n) => Array.from({ length: n }, (_, i) => ({ userId: `${from}${i}`, firstName: 'F', lastName: 'L', userType: i === 0 ? 'A' : 'U' }));

function renderAt(path, element, pattern = path) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path={pattern} element={element} />
        <Route path="/admin" element={<p>Admin menu</p>} />
        <Route path="/admin/users/update/:userId" element={<p>Update screen</p>} />
        <Route path="/admin/users/delete/:userId" element={<p>Delete screen</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

let fetchMock;
beforeEach(() => {
  fetchMock = vi.fn();
  vi.stubGlobal('fetch', fetchMock);
  setAccessToken('tok');
});
afterEach(() => vi.unstubAllGlobals());

describe('UserListPage (COUSR00)', () => {
  it('lists a page, pages forward and opens row actions', async () => {
    fetchMock
      .mockReturnValueOnce(json(200, { users: users('ADMIN00', 10), firstUserId: 'ADMIN000', lastUserId: 'ADMIN009', hasPrevious: false, hasNext: true }))
      .mockReturnValueOnce(json(200, { users: users('USER000', 2), firstUserId: 'USER0000', lastUserId: 'USER0001', hasPrevious: true, hasNext: false }));
    renderAt('/admin/users', <UserListPage />);
    expect(await screen.findByText('ADMIN000')).toBeInTheDocument();
    expect(fetchMock.mock.calls[0][1].headers.Authorization).toBe('Bearer tok');
    expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();
    await userEvent.click(screen.getByRole('button', { name: 'Next' }));
    expect(await screen.findByText('USER0001')).toBeInTheDocument();
    expect(fetchMock.mock.calls[1][0]).toContain('after=ADMIN009');
    expect(screen.getByTestId('page-number')).toHaveTextContent('Page 2');
    await userEvent.click(screen.getByRole('button', { name: 'Update USER0001' }));
    expect(screen.getByText('Update screen')).toBeInTheDocument();
  });

  it('searches by user ID and shows end-of-list messages', async () => {
    fetchMock
      .mockReturnValueOnce(json(200, { users: users('A', 1), firstUserId: 'A0', lastUserId: 'A0', hasPrevious: false, hasNext: false }))
      .mockReturnValueOnce(json(200, { users: [], hasPrevious: true, hasNext: false, message: 'You are already at the bottom of the page...' }));
    renderAt('/admin/users', <UserListPage />);
    await screen.findByText('A0');
    await userEvent.type(screen.getByLabelText('Search user ID'), 'user0003');
    await userEvent.click(screen.getByRole('button', { name: 'Search' }));
    expect(fetchMock.mock.calls[1][0]).toContain('search=USER0003');
    expect(await screen.findByText('You are already at the bottom of the page...')).toBeInTheDocument();
  });

  it('shows service errors', async () => {
    fetchMock.mockReturnValueOnce(json(503, { code: 'UNABLE_TO_LOOKUP', message: 'Unable to lookup User...' }));
    renderAt('/admin/users', <UserListPage />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Unable to lookup User...');
  });
});

describe('UserAddPage (COUSR01)', () => {
  it('checks required fields in legacy order', async () => {
    renderAt('/admin/users/new', <UserAddPage />);
    await userEvent.click(screen.getByRole('button', { name: 'Add user' }));
    expect(screen.getByRole('alert')).toHaveTextContent('First Name can NOT be empty...');
    await userEvent.type(screen.getByLabelText('First name'), 'Ann');
    await userEvent.type(screen.getByLabelText('Last name'), 'Lee');
    await userEvent.click(screen.getByRole('button', { name: 'Add user' }));
    expect(screen.getByRole('alert')).toHaveTextContent('User ID can NOT be empty...');
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('adds a user, upper-cases the ID and shows server errors on the field', async () => {
    fetchMock
      .mockReturnValueOnce(json(201, { user: {}, message: 'User USER0009 has been added ...' }))
      .mockReturnValueOnce(json(409, { code: 'USER_ID_EXISTS', message: 'User ID already exist...' }));
    renderAt('/admin/users/new', <UserAddPage />);
    const fill = async () => {
      await userEvent.type(screen.getByLabelText('First name'), 'Ann');
      await userEvent.type(screen.getByLabelText('Last name'), 'Lee');
      await userEvent.type(screen.getByLabelText('User ID'), 'user0009');
      await userEvent.type(screen.getByLabelText('Password'), 'secret');
      await userEvent.selectOptions(screen.getByLabelText('User type'), 'U');
      await userEvent.click(screen.getByRole('button', { name: 'Add user' }));
    };
    await fill();
    expect(await screen.findByText('User USER0009 has been added ...')).toBeInTheDocument();
    expect(JSON.parse(fetchMock.mock.calls[0][1].body).userId).toBe('USER0009');
    expect(screen.getByLabelText('First name')).toHaveValue('');
    await fill();
    expect(await screen.findByRole('alert')).toHaveTextContent('User ID already exist...');
    expect(screen.getByLabelText('User ID')).toHaveAttribute('aria-invalid', 'true');
  });
});

describe('UserUpdatePage (COUSR02)', () => {
  const ann = { userId: 'USER0002', firstName: 'Ann', lastName: 'Lee', userType: 'U', version: 7 };

  it('fetches the selected user and saves with If-Match', async () => {
    fetchMock
      .mockReturnValueOnce(json(200, ann))
      .mockReturnValueOnce(json(200, { user: { ...ann, lastName: 'Wong', version: 8 }, message: 'User USER0002 has been updated ...' }));
    renderAt('/admin/users/update/USER0002', <UserUpdatePage />, '/admin/users/update/:userId');
    expect(await screen.findByDisplayValue('Lee')).toBeInTheDocument();
    expect(screen.getByText('Press Save to save your updates ...')).toBeInTheDocument();
    await userEvent.clear(screen.getByLabelText('Last name'));
    await userEvent.type(screen.getByLabelText('Last name'), 'Wong');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));
    expect(await screen.findByText('User USER0002 has been updated ...')).toBeInTheDocument();
    const [url, init] = fetchMock.mock.calls[1];
    expect(url).toContain('/users/USER0002');
    expect(init.headers['If-Match']).toBe('"7"');
    expect(JSON.parse(init.body)).toEqual({ firstName: 'Ann', lastName: 'Wong', password: '', userType: 'U' });
  });

  it('requires a user ID to fetch, shows not-found and save-and-exit returns to the menu', async () => {
    fetchMock
      .mockReturnValueOnce(json(404, { code: 'USER_NOT_FOUND', message: 'User ID NOT found...' }))
      .mockReturnValueOnce(json(200, ann))
      .mockReturnValueOnce(json(200, { user: ann, message: 'User USER0002 has been updated ...' }));
    renderAt('/admin/users/update', <UserUpdatePage />);
    await userEvent.click(screen.getByRole('button', { name: 'Fetch' }));
    expect(screen.getByRole('alert')).toHaveTextContent('User ID can NOT be empty...');
    await userEvent.type(screen.getByLabelText('Enter user ID'), 'nouser');
    await userEvent.click(screen.getByRole('button', { name: 'Fetch' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('User ID NOT found...');
    await userEvent.clear(screen.getByLabelText('Enter user ID'));
    await userEvent.type(screen.getByLabelText('Enter user ID'), 'user0002');
    await userEvent.click(screen.getByRole('button', { name: 'Fetch' }));
    await screen.findByDisplayValue('Lee');
    await userEvent.click(screen.getByRole('button', { name: 'Save & exit' }));
    expect(await screen.findByText('Admin menu')).toBeInTheDocument();
  });

  it('shows no-change and stale-version messages from the service', async () => {
    fetchMock
      .mockReturnValueOnce(json(200, ann))
      .mockReturnValueOnce(json(400, { code: 'NO_CHANGES', message: 'Please modify to update ...' }));
    renderAt('/admin/users/update/USER0002', <UserUpdatePage />, '/admin/users/update/:userId');
    await screen.findByDisplayValue('Lee');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Please modify to update ...');
  });
});

describe('UserDeletePage (COUSR03)', () => {
  const bob = { userId: 'USER0005', firstName: 'Lee', lastName: 'Ting', userType: 'U', version: 3 };

  it('shows the user read-only and deletes after confirmation', async () => {
    fetchMock
      .mockReturnValueOnce(json(200, bob))
      .mockReturnValueOnce(json(200, { message: 'User USER0005 has been deleted ...' }));
    renderAt('/admin/users/delete/USER0005', <UserDeletePage />, '/admin/users/delete/:userId');
    expect(await screen.findByTestId('user-details')).toHaveTextContent('Ting');
    expect(screen.getByText('Press Delete to delete this user ...')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Delete' }));
    const dialog = await screen.findByRole('dialog');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }));
    expect(await screen.findByText('User USER0005 has been deleted ...')).toBeInTheDocument();
    expect(fetchMock.mock.calls[1][1]).toMatchObject({ method: 'DELETE', headers: { 'If-Match': '"3"' } });
  });

  it('shows the last-admin rule from the service', async () => {
    fetchMock
      .mockReturnValueOnce(json(200, { ...bob, userType: 'A' }))
      .mockReturnValueOnce(json(409, { code: 'LAST_ADMIN', message: 'At least one administrator must remain...' }));
    renderAt('/admin/users/delete/USER0005', <UserDeletePage />, '/admin/users/delete/:userId');
    await screen.findByTestId('user-details');
    await userEvent.click(screen.getByRole('button', { name: 'Delete' }));
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Delete' }));
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('At least one administrator must remain...'));
  });
});

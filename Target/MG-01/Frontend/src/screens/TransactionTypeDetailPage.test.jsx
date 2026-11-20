import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import TransactionTypeDetailPage from './TransactionTypeDetailPage.jsx';
import { ApiError, transactionTypesApi } from '../api/transactionTypesApi.js';

vi.mock('../api/transactionTypesApi.js', async (orig) => ({
  ...(await orig()),
  transactionTypesApi: { get: vi.fn(), update: vi.fn(), remove: vi.fn() },
}));

const renderAt = (path) =>
  render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/transaction-types/:typeCode" element={<TransactionTypeDetailPage />} />
        <Route path="/transaction-types/new" element={<p>Add page</p>} />
        <Route path="/transaction-types" element={<p>List page</p>} />
      </Routes>
    </MemoryRouter>,
  );

describe('TransactionTypeDetailPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('disables save until the description changes, then updates after confirmation', async () => {
    vi.mocked(transactionTypesApi.get).mockResolvedValue({ typeCode: '03', description: 'Credit' });
    vi.mocked(transactionTypesApi.update).mockResolvedValue({ typeCode: '03', description: 'Credit note' });
    renderAt('/transaction-types/3');
    const description = await screen.findByLabelText(/Description/);
    const save = screen.getByRole('button', { name: 'Save changes' });
    expect(save).toBeDisabled();
    await userEvent.type(description, ' note');
    await userEvent.click(save);
    await userEvent.click(screen.getAllByRole('button', { name: 'Save changes' })[1]);
    expect(transactionTypesApi.update).toHaveBeenCalledWith('03', 'Credit note');
    expect(await screen.findByText('Changes saved.')).toBeInTheDocument();
  });

  it('offers to create a missing code', async () => {
    vi.mocked(transactionTypesApi.get).mockRejectedValue(
      new ApiError({ status: 404, code: 'TYPE_NOT_FOUND', message: 'No transaction type with code 42.' }),
    );
    renderAt('/transaction-types/42');
    expect(await screen.findByRole('link', { name: 'Create it' })).toHaveAttribute('href', '/transaction-types/new?typeCode=42');
  });

  it('shows the dependent-records error when delete is refused', async () => {
    vi.mocked(transactionTypesApi.get).mockResolvedValue({ typeCode: '01', description: 'Purchase' });
    vi.mocked(transactionTypesApi.remove).mockRejectedValue(
      new ApiError({ status: 409, code: 'HAS_DEPENDENT_RECORDS', message: 'Please delete associated child records first.' }),
    );
    renderAt('/transaction-types/01');
    await userEvent.click(await screen.findByRole('button', { name: 'Delete' }));
    await userEvent.click(screen.getAllByRole('button', { name: 'Delete' })[1]);
    expect(await screen.findByText('Please delete associated child records first.')).toBeInTheDocument();
  });
});

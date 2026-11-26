import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import TransactionTypeAddPage from './TransactionTypeAddPage.jsx';
import { ApiError, transactionTypesApi } from '../api/transactionTypesApi.js';

vi.mock('../api/transactionTypesApi.js', async (orig) => ({
  ...(await orig()),
  transactionTypesApi: { create: vi.fn() },
}));

const renderPage = () =>
  render(
    <MemoryRouter initialEntries={['/transaction-types/new']}>
      <Routes>
        <Route path="/transaction-types/new" element={<TransactionTypeAddPage />} />
        <Route path="/transaction-types" element={<p>List page</p>} />
      </Routes>
    </MemoryRouter>,
  );

describe('TransactionTypeAddPage', () => {
  beforeEach(() => {
    vi.mocked(transactionTypesApi.create).mockReset();
  });

  it('reports both required fields when submitted empty (UI-005)', async () => {
    renderPage();
    await userEvent.click(screen.getByRole('button', { name: 'Create' }));
    expect(screen.getByLabelText(/Type code/)).toHaveAttribute('aria-invalid', 'true');
    expect(screen.getByLabelText(/Description/)).toHaveAttribute('aria-invalid', 'true');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('confirms, normalises the code and returns to the list', async () => {
    vi.mocked(transactionTypesApi.create).mockResolvedValue({ typeCode: '08', description: 'Refund' });
    renderPage();
    await userEvent.type(screen.getByLabelText(/Type code/), '8');
    await userEvent.type(screen.getByLabelText(/Description/), 'Refund');
    await userEvent.click(screen.getByRole('button', { name: 'Create' }));
    await userEvent.click(screen.getAllByRole('button', { name: 'Create' })[1]);
    expect(transactionTypesApi.create).toHaveBeenCalledWith('08', 'Refund');
    expect(await screen.findByText('List page')).toBeInTheDocument();
  });

  it('shows the duplicate error with a link to the existing record', async () => {
    const duplicate = new ApiError({ status: 409, code: 'TYPE_ALREADY_EXISTS', message: 'Transaction type 01 already exists.' });
    vi.mocked(transactionTypesApi.create).mockImplementation(async () => {
      throw duplicate;
    });
    renderPage();
    await userEvent.type(screen.getByLabelText(/Type code/), '01');
    await userEvent.type(screen.getByLabelText(/Description/), 'Purchase');
    await userEvent.click(screen.getByRole('button', { name: 'Create' }));
    await userEvent.click(screen.getAllByRole('button', { name: 'Create' })[1]);
    expect(await screen.findByText('Transaction type 01 already exists.')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'View transaction type 01' })).toHaveAttribute('href', '/transaction-types/01');
  });
});

import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import TransactionTypeListPage from './TransactionTypeListPage.jsx';
import { transactionTypesApi } from '../api/transactionTypesApi.js';

vi.mock('../api/transactionTypesApi.js', async (orig) => ({
  ...(await orig()),
  transactionTypesApi: { list: vi.fn(), update: vi.fn(), remove: vi.fn() },
}));

const PAGE = {
  items: [
    { typeCode: '01', description: 'Purchase' },
    { typeCode: '02', description: 'Payment' },
  ],
  firstCode: '01',
  lastCode: '02',
  hasNext: false,
  hasPrevious: false,
  filtered: false,
};

const renderPage = () => render(<MemoryRouter><TransactionTypeListPage /></MemoryRouter>);

describe('TransactionTypeListPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(transactionTypesApi.list).mockResolvedValue(PAGE);
  });

  it('lists rows and rejects a non-numeric type filter without calling the API', async () => {
    renderPage();
    expect(await screen.findByText('Purchase')).toBeInTheDocument();
    await userEvent.type(screen.getByLabelText('Type code'), 'AB');
    await userEvent.click(screen.getByRole('button', { name: 'Search' }));
    expect(screen.getByLabelText('Type code')).toHaveAttribute('aria-invalid', 'true');
    expect(transactionTypesApi.list).toHaveBeenCalledTimes(1);
  });

  it('saves an inline edit', async () => {
    vi.mocked(transactionTypesApi.update).mockResolvedValue({ typeCode: '02', description: 'Payment received' });
    renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'Edit transaction type 02' }));
    await userEvent.type(screen.getByLabelText('Description for type 02'), ' received');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));
    expect(transactionTypesApi.update).toHaveBeenCalledWith('02', 'Payment received');
    expect(await screen.findByText('Transaction type 02 updated.')).toBeInTheDocument();
    expect(screen.getByText('Payment received')).toBeInTheDocument();
  });

  it('deletes after confirmation and reloads the first page', async () => {
    vi.mocked(transactionTypesApi.remove).mockResolvedValue(null);
    renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'Delete transaction type 01' }));
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Delete' }));
    expect(transactionTypesApi.remove).toHaveBeenCalledWith('01');
    expect(await screen.findByText('Transaction type 01 deleted.')).toBeInTheDocument();
    expect(transactionTypesApi.list).toHaveBeenCalledTimes(2);
  });
});

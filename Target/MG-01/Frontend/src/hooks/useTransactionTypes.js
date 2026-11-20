import { useCallback, useState } from 'react';
import { transactionTypesApi } from '../api/transactionTypesApi.js';

const EMPTY_PAGE = { items: [], firstCode: null, lastCode: null, hasNext: false, hasPrevious: false, filtered: false };

/** Keyset paging over the transaction type list (backend E1); page number is client-held. */
export function useTransactionTypes() {
  const [page, setPage] = useState(EMPTY_PAGE);
  const [pageNo, setPageNo] = useState(1);
  const [filters, setFilters] = useState({ typeCode: '', description: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const fetchPage = useCallback(async (nextFilters, paging, nextPageNo) => {
    setLoading(true);
    setError(null);
    try {
      const data = await transactionTypesApi.list({ ...nextFilters, ...paging });
      setPage(data);
      setFilters(nextFilters);
      setPageNo(data.hasPrevious ? Math.max(nextPageNo, 2) : 1);
      return data;
    } catch (e) {
      setError(e);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  const search = useCallback((nextFilters) => fetchPage(nextFilters, {}, 1), [fetchPage]);
  const next = useCallback(() => fetchPage(filters, { after: page.lastCode }, pageNo + 1), [fetchPage, filters, page.lastCode, pageNo]);
  const previous = useCallback(
    () => fetchPage(filters, { before: page.firstCode }, Math.max(pageNo - 1, 1)),
    [fetchPage, filters, page.firstCode, pageNo],
  );
  const reloadFirst = useCallback(() => fetchPage(filters, {}, 1), [fetchPage, filters]);
  const replaceItem = useCallback(
    (item) => setPage((p) => ({ ...p, items: p.items.map((i) => (i.typeCode === item.typeCode ? item : i)) })),
    [],
  );

  return { page, pageNo, filters, loading, error, setError, search, next, previous, reloadFirst, replaceItem };
}

import { useEffect, useRef, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import Alert from '../components/Alert.jsx';
import ConfirmDialog from '../components/ConfirmDialog.jsx';
import FilterBar from '../components/FilterBar.jsx';
import Pager from '../components/Pager.jsx';
import TransactionTypeTable from '../components/TransactionTypeTable.jsx';
import { useTransactionTypes } from '../hooks/useTransactionTypes.js';
import { transactionTypesApi } from '../api/transactionTypesApi.js';
import { validateDescription, validateTypeFilter } from '../validation/transactionType.js';

const NO_MATCH = 'No transaction types match these filters.';

/** S1 – list, filter, page, inline edit and delete transaction types (legacy COTRTLIC). */
export default function TransactionTypeListPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { page, pageNo, filters, loading, error, setError, search, next, previous, reloadFirst, replaceItem } =
    useTransactionTypes();

  const [draft, setDraft] = useState({ typeCode: '', description: '' });
  const [filterError, setFilterError] = useState(null);
  const [status, setStatus] = useState(location.state?.status ?? null);
  const [editing, setEditing] = useState(null); // { typeCode, original, value, error }
  const [confirmDelete, setConfirmDelete] = useState(null);
  const [busy, setBusy] = useState(false);
  const editInputRef = useRef(null);
  const editButtonRefs = useRef({});

  useEffect(() => {
    search({ typeCode: '', description: '' });
    if (location.state?.status) navigate(location.pathname, { replace: true, state: null });
    // initial load only
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (editing) editInputRef.current?.focus();
  }, [editing?.typeCode]); // eslint-disable-line react-hooks/exhaustive-deps

  const noMatch = !loading && !error && page.filtered && page.items.length === 0;
  const empty = !loading && !error && !page.filtered && page.items.length === 0;

  const onSearch = (e) => {
    e.preventDefault();
    const err = validateTypeFilter(draft.typeCode);
    setFilterError(err);
    setStatus(null);
    setEditing(null);
    if (!err) search({ typeCode: draft.typeCode.trim(), description: draft.description.trim() });
  };

  const onClear = () => {
    setDraft({ typeCode: '', description: '' });
    setFilterError(null);
    setStatus(null);
    setEditing(null);
    search({ typeCode: '', description: '' });
  };

  const startEdit = (item) => {
    setStatus(null);
    setError(null);
    setEditing({ typeCode: item.typeCode, original: item.description, value: item.description, error: null });
  };

  const cancelEdit = () => {
    const code = editing?.typeCode;
    setEditing(null);
    setTimeout(() => editButtonRefs.current[code]?.focus(), 0);
  };

  const saveEdit = async () => {
    const err = validateDescription(editing.value);
    if (err) {
      setEditing({ ...editing, error: err });
      editInputRef.current?.focus();
      return;
    }
    setBusy(true);
    try {
      const updated = await transactionTypesApi.update(editing.typeCode, editing.value.trim());
      replaceItem(updated);
      setStatus(`Transaction type ${updated.typeCode} updated.`);
      cancelEdit();
    } catch (e) {
      if (e.errors?.description) setEditing({ ...editing, error: e.errors.description[0] });
      else setError(e);
    } finally {
      setBusy(false);
    }
  };

  const doDelete = async () => {
    setBusy(true);
    try {
      await transactionTypesApi.remove(confirmDelete.typeCode);
      setStatus(`Transaction type ${confirmDelete.typeCode} deleted.`);
      setConfirmDelete(null);
      reloadFirst();
    } catch (e) {
      setConfirmDelete(null);
      setError(e);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="stack flex flex-col gap-sp-5">
      <div className="flex flex-wrap items-center justify-between gap-sp-3">
        <div>
          <Link className="btn btn-ghost btn-sm px-0" to="/admin">← Back to admin menu</Link>
          <h1 className="m-0 text-xl text-primary-900">Transaction types</h1>
        </div>
        <Link className="btn btn-primary" to="/transaction-types/new">Add transaction type</Link>
      </div>

      <FilterBar
        draft={draft}
        onDraftChange={setDraft}
        filterError={filterError}
        noMatch={noMatch}
        noMatchMessage={NO_MATCH}
        filters={filters}
        loading={loading}
        onSearch={onSearch}
        onClear={onClear}
      />

      <section className="card" aria-labelledby="table-heading">
        <div className="card-header">
          <h2 id="table-heading" className="m-0 text-md">Transaction types</h2>
          <span className="text-muted text-sm">Page {pageNo}</span>
        </div>
        <div className="card-body flex flex-col gap-sp-4">
          <Alert variant="success">{status}</Alert>
          {error && <Alert variant="danger">{error.message}</Alert>}
          {!status && !error && !noMatch && page.items.length > 0 && (
            <Alert variant="info">Select Edit or Delete on a row to change it.</Alert>
          )}
          <TransactionTypeTable
            items={page.items}
            pageNo={pageNo}
            loading={loading}
            empty={empty}
            editing={editing}
            busy={busy}
            editInputRef={editInputRef}
            editButtonRefs={editButtonRefs}
            onStartEdit={startEdit}
            onEditChange={(v) => setEditing({ ...editing, value: v, error: null })}
            onSaveEdit={saveEdit}
            onCancelEdit={cancelEdit}
            onDelete={(item) => {
              setStatus(null);
              setConfirmDelete(item);
            }}
          />
          <Pager
            pageNo={pageNo}
            hasPrevious={page.hasPrevious}
            hasNext={page.hasNext}
            disabled={loading || Boolean(editing)}
            onPrevious={previous}
            onNext={next}
          />
        </div>
      </section>

      {confirmDelete && (
        <ConfirmDialog
          title="Delete transaction type?"
          message={`Delete transaction type ${confirmDelete.typeCode} – ${confirmDelete.description}? This cannot be undone.`}
          confirmLabel="Delete"
          danger
          busy={busy}
          onConfirm={doDelete}
          onCancel={() => {
            setConfirmDelete(null);
            setStatus('Nothing was changed.');
          }}
        />
      )}
    </div>
  );
}

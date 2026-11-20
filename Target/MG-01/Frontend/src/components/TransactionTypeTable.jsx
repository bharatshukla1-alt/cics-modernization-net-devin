import { Link } from 'react-router-dom';
import TextField from './TextField.jsx';
import { isUnchanged, MAX_DESCRIPTION } from '../validation/transactionType.js';

const PAGE_ROWS = 7;

/**
 * Transaction type table with inline description editing.
 * @param {{ items: Array<{typeCode: string, description: string}>, pageNo: number, loading: boolean, empty: boolean,
 *   editing: {typeCode: string, original: string, value: string, error: string|null}|null, busy: boolean,
 *   editInputRef: object, editButtonRefs: object, onStartEdit: Function, onEditChange: Function,
 *   onSaveEdit: Function, onCancelEdit: Function, onDelete: Function }} props
 */
export default function TransactionTypeTable({
  items, pageNo, loading, empty, editing, busy, editInputRef, editButtonRefs,
  onStartEdit, onEditChange, onSaveEdit, onCancelEdit, onDelete,
}) {
  const disableRowActions = Boolean(editing) || busy;
  return (
    <div className="table-wrap">
      <table className="data-table">
        <caption className="visually-hidden">Transaction types, page {pageNo}</caption>
        <thead>
          <tr>
            <th scope="col" className="w-[6rem]">Code</th>
            <th scope="col">Description</th>
            <th scope="col" className="w-[12rem] text-right">Actions</th>
          </tr>
        </thead>
        <tbody aria-busy={loading}>
          {loading &&
            Array.from({ length: PAGE_ROWS }, (_, i) => (
              <tr key={`s${i}`} aria-hidden="true">
                <td colSpan={3}><div className="h-4 w-full animate-pulse rounded-md bg-surface-alt" /></td>
              </tr>
            ))}
          {!loading &&
            items.map((item) => {
              const isEditing = editing?.typeCode === item.typeCode;
              const unchanged = isEditing && isUnchanged(editing.value, editing.original);
              return (
                <tr key={item.typeCode} className={isEditing ? 'outline outline-2 outline-primary-300' : undefined}>
                  <td>
                    <Link className="font-mono" to={`/transaction-types/${item.typeCode}`}>{item.typeCode}</Link>
                  </td>
                  <td>
                    {isEditing ? (
                      <TextField
                        ref={editInputRef}
                        id={`edit-${item.typeCode}`}
                        label={`Description for type ${item.typeCode}`}
                        srOnlyLabel
                        value={editing.value}
                        onChange={onEditChange}
                        maxLength={MAX_DESCRIPTION}
                        error={editing.error}
                        hint={unchanged ? 'No changes to save.' : undefined}
                      />
                    ) : (
                      item.description
                    )}
                  </td>
                  <td className="text-right">
                    {isEditing ? (
                      <span className="inline-flex gap-sp-2">
                        <button type="button" className="btn btn-primary btn-sm" onClick={onSaveEdit} disabled={busy || unchanged}>
                          {busy ? 'Saving…' : 'Save'}
                        </button>
                        <button type="button" className="btn btn-secondary btn-sm" onClick={onCancelEdit} disabled={busy}>
                          Cancel
                        </button>
                      </span>
                    ) : (
                      <span className="inline-flex gap-sp-2">
                        <button
                          ref={(el) => (editButtonRefs.current[item.typeCode] = el)}
                          type="button"
                          className="btn btn-ghost btn-sm"
                          onClick={() => onStartEdit(item)}
                          disabled={disableRowActions}
                          aria-label={`Edit transaction type ${item.typeCode}`}
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          className="btn btn-ghost btn-sm text-danger-700"
                          onClick={() => onDelete(item)}
                          disabled={disableRowActions}
                          aria-label={`Delete transaction type ${item.typeCode}`}
                        >
                          Delete
                        </button>
                      </span>
                    )}
                  </td>
                </tr>
              );
            })}
          {empty && (
            <tr>
              <td colSpan={3} className="text-center text-muted">
                No transaction types yet. <Link to="/transaction-types/new">Add transaction type</Link>
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}

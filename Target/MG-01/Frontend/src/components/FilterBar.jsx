import Alert from './Alert.jsx';
import TextField from './TextField.jsx';
import { MAX_DESCRIPTION } from '../validation/transactionType.js';

/**
 * Type-code / description filter form for the transaction type list.
 * @param {{ draft: {typeCode: string, description: string}, onDraftChange: Function, filterError: string|null,
 *   noMatch: boolean, noMatchMessage: string, filters: {typeCode: string, description: string}, loading: boolean,
 *   onSearch: Function, onClear: Function }} props
 */
export default function FilterBar({ draft, onDraftChange, filterError, noMatch, noMatchMessage, filters, loading, onSearch, onClear }) {
  return (
    <section className="card" aria-labelledby="filters-heading">
      <div className="card-header">
        <h2 id="filters-heading" className="m-0 text-md">Find transaction types</h2>
      </div>
      <form className="card-body" onSubmit={onSearch} noValidate>
        <div className="grid grid-cols-1 gap-x-sp-4 md:grid-cols-12">
          <div className="md:col-span-3">
            <TextField
              id="filter-type-code"
              label="Type code"
              value={draft.typeCode}
              onChange={(v) => onDraftChange({ ...draft, typeCode: v })}
              maxLength={2}
              inputMode="numeric"
              mono
              error={filterError ?? (noMatch && filters.typeCode ? ' ' : null)}
              hint="Two digits, for example 05."
            />
          </div>
          <div className="md:col-span-6">
            <TextField
              id="filter-description"
              label="Description contains"
              value={draft.description}
              onChange={(v) => onDraftChange({ ...draft, description: v })}
              maxLength={MAX_DESCRIPTION}
              error={noMatch && filters.description ? ' ' : null}
              hint="Matches exact letter case."
            />
          </div>
          <div className="flex items-start gap-sp-2 md:col-span-3 md:pt-[1.85rem]">
            <button type="submit" className="btn btn-primary" disabled={loading}>Search</button>
            <button type="button" className="btn btn-secondary" onClick={onClear} disabled={loading}>Clear</button>
          </div>
        </div>
        {noMatch && <Alert variant="danger">{noMatchMessage}</Alert>}
      </form>
    </section>
  );
}

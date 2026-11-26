import { useState } from 'react';

/** "Enter User ID" + Fetch (ENTER) row of COUSR02/COUSR03. */
export default function FetchBar({ initial = '', busy, onFetch }) {
  const [id, setId] = useState(initial);
  return (
    <form className="grid grid-cols-1 gap-x-sp-4 md:grid-cols-12" onSubmit={(e) => { e.preventDefault(); onFetch(id); }}>
      <div className="form-group md:col-span-8">
        <label htmlFor="fetch-user-id">Enter user ID</label>
        <input id="fetch-user-id" type="text" value={id} maxLength={8} onChange={(e) => setId(e.target.value.toUpperCase())} />
      </div>
      <div className="md:col-span-4 md:pt-[1.85rem]">
        <button type="submit" className="btn btn-secondary" disabled={busy}>Fetch</button>
      </div>
    </form>
  );
}

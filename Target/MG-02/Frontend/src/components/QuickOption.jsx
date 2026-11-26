import { useState } from 'react';

/** Keyboard shortcut for the legacy option-number field: accepts 1..count. */
export default function QuickOption({ count, onGo }) {
  const [value, setValue] = useState('');
  const [error, setError] = useState(null);

  const submit = (e) => {
    e.preventDefault();
    const n = Number(value);
    if (!/^\d{1,2}$/.test(value.trim()) || n < 1 || n > count) {
      setError('Enter a valid option number.');
      return;
    }
    setError(null);
    onGo(n);
  };

  return (
    <form onSubmit={submit} noValidate className="flex items-end gap-sp-2">
      <div className="form-group m-0">
        <label htmlFor="quick-option">Go to option</label>
        <input
          id="quick-option"
          inputMode="numeric"
          maxLength={2}
          value={value}
          onChange={(e) => setValue(e.target.value.replace(/[^0-9 ]/g, ''))}
          aria-invalid={error ? 'true' : undefined}
          aria-describedby={error ? 'quick-option-error' : undefined}
          className={`w-[5rem] ${error ? 'is-invalid' : ''}`.trim()}
        />
        {error && (
          <span id="quick-option-error" className="form-error">
            {error}
          </span>
        )}
      </div>
      <button type="submit" className="btn btn-secondary">
        Go
      </button>
    </form>
  );
}

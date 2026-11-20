/** Labelled input used by the MG-03 forms (COUSR01/COUSR02 fields). */
export function Field({ id, label, value, onChange, maxLength, error, hint, type = 'text', readOnly, inputRef, upper }) {
  const describedBy = [hint ? `${id}-hint` : null, error ? `${id}-error` : null].filter(Boolean).join(' ') || undefined;
  return (
    <div className="form-group">
      <label htmlFor={id}>{label}</label>
      <input
        ref={inputRef}
        id={id}
        type={type}
        value={value}
        maxLength={maxLength}
        readOnly={readOnly}
        autoComplete={type === 'password' ? 'new-password' : 'off'}
        onChange={(e) => onChange?.(upper ? e.target.value.toUpperCase() : e.target.value)}
        aria-invalid={error ? 'true' : undefined}
        aria-describedby={describedBy}
        className={`${error ? 'is-invalid' : ''} ${readOnly ? 'bg-surface-alt' : ''}`.trim()}
      />
      {hint && <span id={`${id}-hint`} className="form-hint">{hint}</span>}
      {error && <span id={`${id}-error`} className="form-error">{error}</span>}
    </div>
  );
}

export function TypeSelect({ id, value, onChange, error, inputRef, disabled }) {
  return (
    <div className="form-group">
      <label htmlFor={id}>User type</label>
      <select
        ref={inputRef}
        id={id}
        value={value}
        disabled={disabled}
        onChange={(e) => onChange(e.target.value)}
        aria-invalid={error ? 'true' : undefined}
        aria-describedby={error ? `${id}-error` : undefined}
      >
        <option value="">Select…</option>
        <option value="A">Admin (A)</option>
        <option value="U">User (U)</option>
      </select>
      {error && <span id={`${id}-error`} className="form-error">{error}</span>}
    </div>
  );
}

export const typeLabel = (t) => (t === 'A' ? 'Admin' : 'User');

/** COUSR01C/COUSR02C PROCESS-ENTER-KEY: first empty field in legacy order, or null. */
export function firstMissing(form, order) {
  const messages = {
    firstName: 'First Name can NOT be empty...',
    lastName: 'Last Name can NOT be empty...',
    userId: 'User ID can NOT be empty...',
    password: 'Password can NOT be empty...',
    userType: 'User Type can NOT be empty...',
  };
  const field = order.find((f) => !String(form[f] ?? '').trim());
  return field ? { field, message: messages[field] } : null;
}

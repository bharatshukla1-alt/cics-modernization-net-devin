import { forwardRef } from 'react';

const TextField = forwardRef(function TextField(
  { id, label, value, onChange, onBlur, error, hint, required, maxLength, readOnly, mono, inputMode, autoFocus, srOnlyLabel },
  ref,
) {
  const describedBy = [hint ? `${id}-hint` : null, error ? `${id}-error` : null].filter(Boolean).join(' ') || undefined;
  return (
    <div className="form-group">
      <label htmlFor={id} className={srOnlyLabel ? 'visually-hidden' : undefined}>
        {label}
        {required && <span className="text-muted font-normal"> (required)</span>}
      </label>
      <input
        ref={ref}
        id={id}
        type="text"
        value={value}
        onChange={(e) => onChange?.(e.target.value)}
        onBlur={onBlur}
        maxLength={maxLength}
        readOnly={readOnly}
        required={required}
        inputMode={inputMode}
        autoFocus={autoFocus}
        aria-invalid={error ? 'true' : undefined}
        aria-describedby={describedBy}
        className={`${error ? 'is-invalid' : ''} ${mono ? 'font-mono' : ''} ${readOnly ? 'bg-surface-alt' : ''}`.trim()}
      />
      {hint && (
        <span id={`${id}-hint`} className="form-hint">
          {hint}
        </span>
      )}
      {error && (
        <span id={`${id}-error`} className="form-error">
          {error}
        </span>
      )}
    </div>
  );
});

export default TextField;

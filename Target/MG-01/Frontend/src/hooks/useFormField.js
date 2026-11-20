import { useState } from 'react';

/**
 * State and validation for one form field.
 * Blur validation is skipped when focus moves to a submit button, so submit-time
 * validation can report every invalid field at once (UI-005).
 * @param {string} initial initial value
 * @param {(value: string) => string|null} validator returns an error message or null
 * @param {{ clearOnChange?: boolean }} [options] clear the error while the user types
 */
export function useFormField(initial, validator, { clearOnChange = false } = {}) {
  const [value, setValue] = useState(initial);
  const [error, setError] = useState(null);

  const onChange = (next) => {
    setValue(next);
    if (clearOnChange) setError(null);
  };

  const onBlur = (e) => {
    if (e.relatedTarget?.type !== 'submit') setError(validator(value));
  };

  const validate = () => {
    const next = validator(value);
    setError(next);
    return next;
  };

  return { value, setValue, onChange, error, setError, onBlur, validate };
}

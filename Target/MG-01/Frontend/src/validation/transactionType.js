// Mirrors backend spec §5 / UI spec §6 messages.
export const MAX_DESCRIPTION = 50;
const DIGITS = /^\d{1,2}$/;
const ALNUM_SPACES = /^[A-Za-z0-9 ]+$/;

export function normalizeCode(raw) {
  const value = (raw ?? '').trim();
  return DIGITS.test(value) ? String(Number(value)).padStart(2, '0') : value;
}

export function validateTypeCode(raw) {
  const value = (raw ?? '').trim();
  if (!value) return 'Type code is required.';
  if (!DIGITS.test(value)) return 'Type code must be numeric.';
  if (Number(value) === 0) return 'Type code must not be zero.';
  return null;
}

export function validateTypeFilter(raw) {
  const value = (raw ?? '').trim();
  if (!value) return null;
  return DIGITS.test(value) ? null : 'Type code must be a 2-digit number.';
}

export function validateDescription(raw) {
  const value = (raw ?? '').trim();
  if (!value) return 'Description is required.';
  if (value.length > MAX_DESCRIPTION) return 'Description can be at most 50 characters.';
  if (!ALNUM_SPACES.test(value)) return 'Description can contain only letters, numbers and spaces.';
  return null;
}

/** Legacy change check: equal ignoring case and surrounding spaces means "no change". */
export function isUnchanged(next, stored) {
  return (next ?? '').trim().toUpperCase() === (stored ?? '').trim().toUpperCase();
}

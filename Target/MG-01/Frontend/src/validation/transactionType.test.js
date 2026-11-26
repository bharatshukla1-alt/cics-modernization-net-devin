import { describe, expect, it } from 'vitest';
import {
  isUnchanged, normalizeCode, validateDescription, validateTypeCode, validateTypeFilter,
} from './transactionType.js';

describe('type code', () => {
  it.each([
    ['', 'Type code is required.'],
    ['AB', 'Type code must be numeric.'],
    ['123', 'Type code must be numeric.'],
    ['00', 'Type code must not be zero.'],
    ['5', null],
  ])('validateTypeCode(%s)', (value, message) => expect(validateTypeCode(value)).toBe(message));

  it('pads single digits', () => expect(normalizeCode('5')).toBe('05'));

  it.each([
    ['', null],
    ['00', null],
    ['A1', 'Type code must be a 2-digit number.'],
  ])('validateTypeFilter(%s)', (value, message) => expect(validateTypeFilter(value)).toBe(message));
});

describe('description', () => {
  it.each([
    ['', 'Description is required.'],
    ['   ', 'Description is required.'],
    ['Purchase-Online', 'Description can contain only letters, numbers and spaces.'],
    ['A'.repeat(51), 'Description can be at most 50 characters.'],
    ['Purchase Online 2', null],
  ])('validateDescription(%s)', (value, message) => expect(validateDescription(value)).toBe(message));

  it('treats case and surrounding spaces as no change', () => {
    expect(isUnchanged(' purchase ', 'PURCHASE')).toBe(true);
    expect(isUnchanged('Purchases', 'PURCHASE')).toBe(false);
  });
});

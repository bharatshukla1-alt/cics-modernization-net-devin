import { describe, expect, it } from 'vitest';
import { safeReturnTo } from './oidc.js';

describe('safeReturnTo', () => {
  it.each([
    ['/transaction-types/05', '/transaction-types/05'],
    ['//evil.example', '/transaction-types'],
    ['https://evil.example', '/transaction-types'],
    ['/\\evil.example', '/transaction-types'],
    [undefined, '/transaction-types'],
  ])('%s -> %s', (input, expected) => expect(safeReturnTo(input)).toBe(expected));
});

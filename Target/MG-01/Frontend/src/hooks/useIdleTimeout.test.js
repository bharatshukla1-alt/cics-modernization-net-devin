import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useIdleTimeout } from './useIdleTimeout.js';

describe('useIdleTimeout', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('warns before the limit, can be extended, and signs out at the limit', () => {
    const onSignOut = vi.fn();
    const { result } = renderHook(() => useIdleTimeout(1000, 200, onSignOut));
    act(() => vi.advanceTimersByTime(850));
    expect(result.current.warning).toBe(true);
    act(() => result.current.stayActive());
    expect(result.current.warning).toBe(false);
    act(() => vi.advanceTimersByTime(1000));
    expect(onSignOut).toHaveBeenCalledOnce();
    expect(result.current.signedOut).toBe(true);
  });

  it('signs out immediately on request', () => {
    const onSignOut = vi.fn();
    const { result } = renderHook(() => useIdleTimeout(1000, 200, onSignOut));
    act(() => result.current.stayActive(true));
    expect(onSignOut).toHaveBeenCalledOnce();
    expect(result.current.signedOut).toBe(true);
  });
});

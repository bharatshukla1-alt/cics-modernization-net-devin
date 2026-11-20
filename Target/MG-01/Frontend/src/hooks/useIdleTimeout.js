import { useCallback, useEffect, useRef, useState } from 'react';

const EVENTS = ['mousedown', 'keydown', 'touchstart', 'scroll'];

/** Warns before the idle limit, then signs out and clears client state (UI spec §10). */
export function useIdleTimeout(limitMs, warningMs, onSignOut) {
  const [warning, setWarning] = useState(false);
  const [signedOut, setSignedOut] = useState(false);
  const timers = useRef([]);

  const schedule = useCallback(() => {
    timers.current.forEach(clearTimeout);
    timers.current = [
      setTimeout(() => setWarning(true), limitMs - warningMs),
      setTimeout(() => {
        onSignOut();
        setSignedOut(true);
      }, limitMs),
    ];
  }, [limitMs, warningMs, onSignOut]);

  const stayActive = useCallback(
    (signOutNow = false) => {
      setWarning(false);
      if (signOutNow === true) {
        timers.current.forEach(clearTimeout);
        onSignOut();
        setSignedOut(true);
        return;
      }
      schedule();
    },
    [schedule, onSignOut],
  );

  useEffect(() => {
    schedule();
    const reset = () => {
      if (!warning) schedule();
    };
    EVENTS.forEach((e) => window.addEventListener(e, reset, { passive: true }));
    return () => {
      timers.current.forEach(clearTimeout);
      EVENTS.forEach((e) => window.removeEventListener(e, reset));
    };
  }, [schedule, warning]);

  return { warning, signedOut, stayActive };
}

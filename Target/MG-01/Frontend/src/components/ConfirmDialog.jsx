import { useEffect, useId, useRef } from 'react';

/** Accessible modal confirmation (replaces the legacy second key press). */
export default function ConfirmDialog({
  title, message, confirmLabel = 'Confirm', cancelLabel = 'Cancel', danger = false, busy = false, onConfirm, onCancel,
}) {
  const titleId = useId();
  const messageId = useId();
  const dialogRef = useRef(null);
  const confirmRef = useRef(null);

  useEffect(() => {
    const previous = document.activeElement;
    confirmRef.current?.focus();
    return () => previous?.focus?.();
  }, []);

  const onKeyDown = (e) => {
    if (e.key === 'Escape') {
      e.preventDefault();
      onCancel();
    }
    if (e.key === 'Tab') {
      const focusable = dialogRef.current.querySelectorAll('button:not([disabled])');
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-sp-4">
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={messageId}
        className="card fade-in w-full max-w-form shadow-lg"
        onKeyDown={onKeyDown}
      >
        <div className="card-header">
          <h2 id={titleId} className="m-0 text-md">{title}</h2>
        </div>
        <div className="card-body">
          <p id={messageId} className="m-0">{message}</p>
        </div>
        <div className="card-footer flex justify-end gap-sp-3">
          <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={busy}>
            {cancelLabel}
          </button>
          <button
            ref={confirmRef}
            type="button"
            className={`btn ${danger ? 'btn-danger' : 'btn-primary'}`}
            onClick={onConfirm}
            disabled={busy}
          >
            {busy ? 'Working…' : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}

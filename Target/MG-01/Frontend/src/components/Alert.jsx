const VARIANTS = { info: 'alert--info', success: 'alert--success', danger: 'alert--danger', warning: 'alert--warning' };

export default function Alert({ variant = 'info', children, id }) {
  if (!children) return null;
  const isError = variant === 'danger';
  return (
    <div
      id={id}
      className={`alert ${VARIANTS[variant]} fade-in`}
      role={isError ? 'alert' : 'status'}
      aria-live={isError ? 'assertive' : 'polite'}
    >
      <span>{children}</span>
    </div>
  );
}

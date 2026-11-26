/**
 * Previous / Next keyset paging controls.
 * @param {{ pageNo: number, hasPrevious: boolean, hasNext: boolean, disabled: boolean, onPrevious: Function, onNext: Function }} props
 */
export default function Pager({ pageNo, hasPrevious, hasNext, disabled, onPrevious, onNext }) {
  return (
    <div className="flex items-center justify-between">
      <button type="button" className="btn btn-secondary btn-sm" onClick={onPrevious} disabled={disabled || !hasPrevious}>
        Previous
      </button>
      <span className="text-muted text-sm">Page {pageNo}</span>
      <button type="button" className="btn btn-secondary btn-sm" onClick={onNext} disabled={disabled || !hasNext}>
        Next
      </button>
    </div>
  );
}

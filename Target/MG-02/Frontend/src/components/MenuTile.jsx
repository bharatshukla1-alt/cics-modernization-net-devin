import { MENU_STATUS } from '../api/menuStatus.js';

const CHIPS = {
  [MENU_STATUS.NOT_INSTALLED]: { label: 'Not available yet', className: 'badge badge--neutral' },
  [MENU_STATUS.COMING_SOON]: { label: 'Coming soon', className: 'badge badge--info' },
  [MENU_STATUS.ADMIN_ONLY]: { label: 'Admin only', className: 'badge badge--warning' },
};

/** One menu option as a clickable tile. */
export default function MenuTile({ option, onSelect }) {
  const chip = CHIPS[option.status];
  return (
    <button
      type="button"
      className="card text-left w-full h-full cursor-pointer hover:shadow-lg focus-visible:shadow-lg"
      onClick={() => onSelect(option)}
    >
      <span className="card-body flex items-start gap-sp-3">
        <span className="badge badge--accent" aria-hidden="true">
          {option.number}
        </span>
        <span className="flex flex-col gap-sp-1">
          <span className="font-semibold">{option.name}</span>
          {chip && <span className={chip.className}>{chip.label}</span>}
        </span>
      </span>
    </button>
  );
}

import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Alert from '@mg01/components/Alert.jsx';
import { shellApi } from '../api/shellApi.js';
import MenuTile from '../components/MenuTile.jsx';
import QuickOption from '../components/QuickOption.jsx';
import { MENU_STATUS } from '../api/menuStatus.js';

const STATUS_MESSAGES = {
  [MENU_STATUS.NOT_INSTALLED]: (name) => `This option ${name} is not installed yet.`,
  [MENU_STATUS.COMING_SOON]: (name) => `This option ${name} is coming soon.`,
  [MENU_STATUS.ADMIN_ONLY]: () => 'No access - admin only option.',
};

/** Customer services ("main") or Administration ("admin") menu. */
export default function MenuPage({ kind, onUnauthorized }) {
  const navigate = useNavigate();
  const [menu, setMenu] = useState(null);
  const [error, setError] = useState(null);
  const [info, setInfo] = useState(null);

  useEffect(() => {
    let active = true;
    setMenu(null);
    setError(null);
    shellApi
      .menu(kind)
      .then((m) => active && setMenu(m))
      .catch((err) => {
        if (!active) return;
        if (err.status === 401) onUnauthorized();
        else setError(err.message);
      });
    return () => {
      active = false;
    };
  }, [kind, onUnauthorized]);

  const select = (option) => {
    if (option.status === MENU_STATUS.AVAILABLE && option.route) {
      navigate(option.route);
      return;
    }
    setInfo((STATUS_MESSAGES[option.status] ?? STATUS_MESSAGES[MENU_STATUS.NOT_INSTALLED])(option.name));
  };

  const goTo = (n) => {
    const option = menu?.options.find((o) => o.number === n);
    if (option) select(option);
  };

  if (error) return <Alert variant="danger">{error}</Alert>;
  if (!menu) return <p className="text-muted" role="status">Loading menu…</p>;

  return (
    <section aria-labelledby="menu-title" className="flex flex-col gap-sp-4">
      <div className="flex flex-between items-end flex-wrap gap-sp-3">
        <h1 id="menu-title" className="text-xl m-0">
          {menu.title}
        </h1>
        <QuickOption count={menu.options.length} onGo={goTo} />
      </div>
      <Alert variant="info">{info}</Alert>
      <ul className="list-none p-0 m-0 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-sp-4">
        {menu.options.map((o) => (
          <li key={o.number}>
            <MenuTile option={o} onSelect={select} />
          </li>
        ))}
      </ul>
    </section>
  );
}

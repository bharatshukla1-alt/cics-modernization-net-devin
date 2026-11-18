import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App.jsx';
import keycloak, { initKeycloak, login } from './auth/keycloak';
import { setSessionExpiredHandler } from './services/http';
import './styles/bfsi-theme.css';
import './styles/app.css';

const root = ReactDOM.createRoot(document.getElementById('root'));

function renderApp() {
  root.render(
    <React.StrictMode>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </React.StrictMode>
  );
}

initKeycloak()
  .then((authenticated) => {
    if (!authenticated) {
      login();
      return;
    }
    setSessionExpiredHandler(() => {
      keycloak.clearToken();
      login();
    });
    renderApp();
  })
  .catch(() => {
    root.render(<p role="alert">Unable to reach the sign-in service. Please try again later.</p>);
  });

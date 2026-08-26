import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const navItems = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/contacts', label: 'Contacts' },
  { to: '/templates', label: 'Templates' },
  { to: '/senders', label: 'Senders' },
  { to: '/campaigns', label: 'Campaigns' },
];

export function Layout() {
  const { user, logout } = useAuth();

  return (
    <div className="app">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-icon">LV</span>
          <div>
            <h1>LexVersus</h1>
            <p>Email Marketing</p>
          </div>
        </div>
        <nav>
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-footer">
          <div className="user-info">
            <span className="user-name">{user?.fullName}</span>
            <span className="user-email">{user?.email}</span>
          </div>
          <button className="btn small" onClick={logout}>Sign Out</button>
        </div>
      </aside>
      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}

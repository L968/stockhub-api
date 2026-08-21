import { Activity, Clock3, LayoutDashboard, LogOut, Menu, X } from 'lucide-react';
import { useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { Brand } from '../components/Brand';
import { useAuth } from '../features/auth/AuthContext';

const links = [
  { to: '/', label: 'Overview', icon: LayoutDashboard, end: true },
  { to: '/market', label: 'Market', icon: Activity, end: false },
  { to: '/activity', label: 'Activity', icon: Clock3, end: false },
] as const;

export function AppShell() {
  const [menuOpen, setMenuOpen] = useState(false);
  const { user, signOut } = useAuth();

  const navigation = links.map(({ to, label, icon: Icon, end }) => (
    <NavLink
      key={to}
      to={to}
      end={end}
      onClick={() => setMenuOpen(false)}
      className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}
    >
      <Icon size={17} aria-hidden="true" />
      {label}
    </NavLink>
  ));

  return (
    <div className="min-h-screen bg-ink text-slate-200">
      <header className="sticky top-0 z-40 border-b border-line/80 bg-ink/90 backdrop-blur-xl">
        <div className="mx-auto flex h-16 max-w-[1500px] items-center justify-between px-4 sm:px-6">
          <Brand />
          <div className="flex items-center gap-3">
            <div className="hidden text-right sm:block">
              <p className="text-sm font-medium text-slate-200">{user?.fullName}</p>
              <p className="text-xs text-slate-500">{user?.email}</p>
            </div>
            <button className="icon-button hidden sm:grid" onClick={signOut} title="Sign out" type="button">
              <LogOut size={17} />
            </button>
            <button
              className="icon-button sm:hidden"
              onClick={() => setMenuOpen((value) => !value)}
              aria-label="Toggle navigation"
              aria-expanded={menuOpen}
              type="button"
            >
              {menuOpen ? <X size={19} /> : <Menu size={19} />}
            </button>
          </div>
        </div>
      </header>

      {menuOpen && (
        <div className="fixed inset-x-0 top-16 z-30 border-b border-line bg-panel p-4 shadow-2xl sm:hidden">
          <nav className="space-y-1">{navigation}</nav>
          <button className="nav-link mt-2 w-full" onClick={signOut} type="button">
            <LogOut size={17} /> Sign out
          </button>
        </div>
      )}

      <div className="mx-auto flex max-w-[1500px]">
        <aside className="sticky top-16 hidden h-[calc(100vh-4rem)] w-56 shrink-0 border-r border-line px-4 py-6 sm:block">
          <nav className="space-y-1">{navigation}</nav>
          <div className="absolute bottom-6 left-4 right-4 rounded-xl border border-line bg-panel/60 p-3">
            <p className="font-mono text-[10px] uppercase tracking-widest text-lime">Simulation</p>
            <p className="mt-1 text-xs leading-relaxed text-slate-500">Portfolio environment. No real money involved.</p>
          </div>
        </aside>
        <main className="min-w-0 flex-1 px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

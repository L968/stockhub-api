import { useState } from 'react';
import { useAuth } from './contexts/AuthContext';
import Header from './components/Layout/Header';
import Dashboard from './pages/Dashboard';
import Trading from './pages/Trading';
import History from './pages/History';
import Auth from './pages/Auth';
import { LayoutDashboard, TrendingUp, History as HistoryIcon } from 'lucide-react';

type Page = 'dashboard' | 'trading' | 'history';

function App() {
  const { user, loading } = useAuth();
  const [currentPage, setCurrentPage] = useState<Page>('dashboard');

  if (loading) {
    return (
      <div className="min-h-screen bg-slate-900 flex items-center justify-center">
        <div className="text-center">
          <div className="w-16 h-16 border-4 border-cyan-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
          <p className="text-slate-300">Loading...</p>
        </div>
      </div>
    );
  }

  if (!user) {
    return <Auth />;
  }

  return (
    <div className="min-h-screen bg-slate-900">
      <Header />

      <div className="flex">
        <aside className="w-64 bg-slate-800 border-r border-slate-700 min-h-[calc(100vh-4rem)] sticky top-16">
          <nav className="p-4 space-y-2">
            <button
              onClick={() => setCurrentPage('dashboard')}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg font-medium transition ${
                currentPage === 'dashboard'
                  ? 'bg-cyan-500/20 text-cyan-400 border border-cyan-500/30'
                  : 'text-slate-300 hover:bg-slate-700/50 hover:text-slate-100'
              }`}
            >
              <LayoutDashboard className="w-5 h-5" />
              Dashboard
            </button>

            <button
              onClick={() => setCurrentPage('trading')}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg font-medium transition ${
                currentPage === 'trading'
                  ? 'bg-cyan-500/20 text-cyan-400 border border-cyan-500/30'
                  : 'text-slate-300 hover:bg-slate-700/50 hover:text-slate-100'
              }`}
            >
              <TrendingUp className="w-5 h-5" />
              Trading
            </button>

            <button
              onClick={() => setCurrentPage('history')}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg font-medium transition ${
                currentPage === 'history'
                  ? 'bg-cyan-500/20 text-cyan-400 border border-cyan-500/30'
                  : 'text-slate-300 hover:bg-slate-700/50 hover:text-slate-100'
              }`}
            >
              <HistoryIcon className="w-5 h-5" />
              History
            </button>
          </nav>
        </aside>

        <main className="flex-1">
          {currentPage === 'dashboard' && <Dashboard />}
          {currentPage === 'trading' && <Trading />}
          {currentPage === 'history' && <History />}
        </main>
      </div>
    </div>
  );
}

export default App;

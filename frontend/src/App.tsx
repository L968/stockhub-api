import { Navigate, Route, Routes } from 'react-router-dom';
import { AppShell } from './app/AppShell';
import { ActivityPage } from './features/activity/ActivityPage';
import { AuthPage } from './features/auth/AuthPage';
import { useAuth } from './features/auth/AuthContext';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { MarketPage } from './features/market/MarketPage';

export default function App() {
  const { user } = useAuth();
  if (!user) return <AuthPage />;

  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route index element={<DashboardPage />} />
        <Route path="market" element={<MarketPage />} />
        <Route path="market/:symbol" element={<MarketPage />} />
        <Route path="activity" element={<ActivityPage />} />
        <Route path="*" element={<Navigate replace to="/" />} />
      </Route>
    </Routes>
  );
}

import { useState, useEffect } from 'react';
import { portfolioService, orderService, tradeService } from '../services/api';
import PortfolioCard from '../components/Dashboard/PortfolioCard';
import RecentOrders from '../components/Dashboard/RecentOrders';
import RecentTrades from '../components/Dashboard/RecentTrades';
import type { PortfolioResponse, Order, Trade } from '../types';

export default function Dashboard() {
  const [portfolio, setPortfolio] = useState<PortfolioResponse>({ totalValue: 0, positions: [] });
  const [orders, setOrders] = useState<Order[]>([]);
  const [trades, setTrades] = useState<Trade[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      const [portfolioData, ordersData, tradesData] = await Promise.all([
        portfolioService.getMyPortfolio(),
        orderService.getMyOrders(1, 5),
        tradeService.getMyTrades(1, 5),
      ]);

      setPortfolio(portfolioData);
      setOrders(ordersData.items);
      setTrades(tradesData.items);
    } catch (error) {
      console.error('Failed to load dashboard data:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="animate-pulse space-y-6">
          <div className="h-64 bg-slate-800 rounded-xl border border-slate-700"></div>
          <div className="grid md:grid-cols-2 gap-6">
            <div className="h-96 bg-slate-800 rounded-xl border border-slate-700"></div>
            <div className="h-96 bg-slate-800 rounded-xl border border-slate-700"></div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <h1 className="text-3xl font-bold text-slate-100 mb-8">Dashboard</h1>

      <div className="space-y-6">
        <PortfolioCard portfolio={portfolio} />

        <div className="grid md:grid-cols-2 gap-6">
          <RecentOrders orders={orders} />
          <RecentTrades trades={trades} />
        </div>
      </div>
    </div>
  );
}

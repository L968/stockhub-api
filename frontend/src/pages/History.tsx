import { useState } from 'react';
import TradeHistory from '../components/History/TradeHistory';
import OrderHistory from '../components/History/OrderHistory';

export default function History() {
  const [activeTab, setActiveTab] = useState<'trades' | 'orders'>('trades');

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <h1 className="text-3xl font-bold text-slate-100 mb-8">History</h1>

      <div className="mb-6">
        <div className="border-b border-slate-700">
          <nav className="flex gap-8">
            <button
              onClick={() => setActiveTab('trades')}
              className={`pb-4 px-1 border-b-2 font-medium text-sm transition ${
                activeTab === 'trades'
                  ? 'border-cyan-500 text-cyan-400'
                  : 'border-transparent text-slate-400 hover:text-slate-200 hover:border-slate-600'
              }`}
            >
              Trades
            </button>
            <button
              onClick={() => setActiveTab('orders')}
              className={`pb-4 px-1 border-b-2 font-medium text-sm transition ${
                activeTab === 'orders'
                  ? 'border-cyan-500 text-cyan-400'
                  : 'border-transparent text-slate-400 hover:text-slate-200 hover:border-slate-600'
              }`}
            >
              Orders
            </button>
          </nav>
        </div>
      </div>

      {activeTab === 'trades' ? <TradeHistory /> : <OrderHistory />}
    </div>
  );
}

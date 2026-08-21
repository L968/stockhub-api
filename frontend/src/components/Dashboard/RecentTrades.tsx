import { Activity } from 'lucide-react';
import type { Trade } from '../../types';

interface RecentTradesProps {
  trades: Trade[];
}

export default function RecentTrades({ trades }: RecentTradesProps) {
  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const formatDate = (date: string) => {
    if (!date) return '-';
    try {
      const dateObj = new Date(date);
      if (isNaN(dateObj.getTime())) {
        return '-';
      }
      return dateObj.toLocaleString('pt-BR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return '-';
    }
  };

  return (
    <div className="bg-slate-800 rounded-xl shadow-lg border border-slate-700 p-6">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-bold text-slate-100">Recent Trades</h2>
        <Activity className="w-5 h-5 text-slate-400" />
      </div>

      {trades.length === 0 ? (
        <div className="text-center py-8 text-slate-400">
          <p>No trades yet</p>
          <p className="text-sm mt-1">Your executed trades will appear here</p>
        </div>
      ) : (
        <div className="space-y-3">
          {trades.map((trade) => (
            <div
              key={trade.id}
              className="flex items-center justify-between p-4 bg-slate-700/50 rounded-lg hover:bg-slate-700 border border-slate-600/50 transition"
            >
              <div className="flex-1">
                <div className="flex items-center gap-2 mb-1">
                  <h3 className="font-semibold text-slate-100">{trade.symbol}</h3>
                  <span
                    className={`text-xs font-medium px-2 py-0.5 rounded ${
                      trade.side === 'BUY' || trade.side === 'Buy'
                        ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                        : 'bg-rose-500/20 text-rose-400 border border-rose-500/30'
                    }`}
                  >
                    {trade.side}
                  </span>
                </div>
                <p className="text-sm text-slate-300">
                  {trade.quantity} @ {formatCurrency(trade.price)}
                </p>
                <p className="text-xs text-slate-400 mt-1">{formatDate(trade.executedAt)}</p>
              </div>
              <div className="text-right">
                <p className="font-semibold text-slate-100">
                  {formatCurrency(trade.quantity * trade.price)}
                </p>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

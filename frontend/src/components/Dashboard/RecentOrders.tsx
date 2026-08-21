import { Clock } from 'lucide-react';
import type { Order } from '../../types';

interface RecentOrdersProps {
  orders: Order[];
}

export default function RecentOrders({ orders }: RecentOrdersProps) {
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

  const getStatusBadge = (status: string) => {
    const statusLower = status.toLowerCase();
    const styles: Record<string, string> = {
      pending: 'bg-yellow-500/20 text-yellow-400 border border-yellow-500/30',
      partiallyfilled: 'bg-cyan-500/20 text-cyan-400 border border-cyan-500/30',
      filled: 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30',
      cancelled: 'bg-slate-500/20 text-slate-400 border border-slate-500/30',
    };

    const style = styles[statusLower] || 'bg-slate-500/20 text-slate-400 border border-slate-500/30';

    return (
      <span className={`px-2 py-1 text-xs font-medium rounded-full ${style}`}>
        {status}
      </span>
    );
  };

  return (
    <div className="bg-slate-800 rounded-xl shadow-lg border border-slate-700 p-6">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-bold text-slate-100">Recent Orders</h2>
        <Clock className="w-5 h-5 text-slate-400" />
      </div>

      {orders.length === 0 ? (
        <div className="text-center py-8 text-slate-400">
          <p>No orders yet</p>
          <p className="text-sm mt-1">Place your first order to get started</p>
        </div>
      ) : (
        <div className="space-y-3">
          {orders.map((order) => (
            <div
              key={order.id}
              className="flex items-center justify-between p-4 bg-slate-700/50 rounded-lg hover:bg-slate-700 border border-slate-600/50 transition"
            >
              <div className="flex-1">
                <div className="flex items-center gap-2 mb-1">
                  <h3 className="font-semibold text-slate-100">{order.stock.symbol}</h3>
                  <span
                    className={`text-xs font-medium px-2 py-0.5 rounded ${
                      order.side === 'Buy' || order.side === 'BUY'
                        ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                        : 'bg-rose-500/20 text-rose-400 border border-rose-500/30'
                    }`}
                  >
                    {order.side.toUpperCase()}
                  </span>
                  {getStatusBadge(order.status)}
                </div>
                <p className="text-sm text-slate-300">
                  {order.quantity} @ {formatCurrency(order.price)}
                </p>
                <p className="text-xs text-slate-400 mt-1">{formatDate(order.createdAtUtc)}</p>
              </div>
              <div className="text-right">
                <p className="font-semibold text-slate-100">
                  {formatCurrency(order.quantity * order.price)}
                </p>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

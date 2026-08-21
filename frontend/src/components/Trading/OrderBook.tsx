import { useState, useEffect } from 'react';
import { BookOpen } from 'lucide-react';
import { stockService } from '../../services/api';
import type { OrderBook as OrderBookType } from '../../types';

interface OrderBookProps {
  symbol: string;
}

export default function OrderBook({ symbol }: OrderBookProps) {
  const [orderBook, setOrderBook] = useState<OrderBookType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchOrderBook = async () => {
      setLoading(true);
      setError('');
      try {
        const data = await stockService.getOrderBook(symbol);
        setOrderBook(data);
      } catch (err) {
        setError('Failed to load order book');
        console.error(err);
      } finally {
        setLoading(false);
      }
    };

    if (symbol) {
      fetchOrderBook();
    }
  }, [symbol]);

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  if (loading) {
    return (
      <div className="bg-slate-800 rounded-xl shadow-lg border border-slate-700 p-6">
        <div className="animate-pulse">
          <div className="h-6 bg-slate-700 rounded w-1/3 mb-4"></div>
          <div className="space-y-2">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="h-12 bg-slate-700 rounded"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (error || !orderBook) {
    return (
      <div className="bg-slate-800 rounded-xl shadow-lg border border-slate-700 p-6">
        <div className="text-center text-slate-400 py-8">
          <BookOpen className="w-12 h-12 mx-auto mb-2 text-slate-500" />
          <p>{error || 'Order book not available'}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-slate-800 rounded-xl shadow-lg border border-slate-700 p-6">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-bold text-slate-100">Order Book</h2>
        <span className="text-sm text-slate-400">{symbol}</span>
      </div>

      <div className="grid grid-cols-2 gap-6">
        <div>
          <h3 className="text-sm font-semibold text-rose-400 mb-3">Sell Orders</h3>
          <div className="space-y-2">
            {orderBook.asks.length === 0 ? (
              <p className="text-sm text-slate-400">No sell orders</p>
            ) : (
              orderBook.asks.slice(0, 10).map((ask, index) => (
                <div
                  key={index}
                  className="flex justify-between items-center p-2 bg-rose-500/10 rounded hover:bg-rose-500/20 border border-rose-500/20 transition"
                >
                  <span className="text-sm font-medium text-slate-100">
                    {formatCurrency(ask.price)}
                  </span>
                  <span className="text-sm text-slate-300">{ask.quantity}</span>
                </div>
              ))
            )}
          </div>
        </div>

        <div>
          <h3 className="text-sm font-semibold text-emerald-400 mb-3">Buy Orders</h3>
          <div className="space-y-2">
            {orderBook.bids.length === 0 ? (
              <p className="text-sm text-slate-400">No buy orders</p>
            ) : (
              orderBook.bids.slice(0, 10).map((bid, index) => (
                <div
                  key={index}
                  className="flex justify-between items-center p-2 bg-emerald-500/10 rounded hover:bg-emerald-500/20 border border-emerald-500/20 transition"
                >
                  <span className="text-sm font-medium text-slate-100">
                    {formatCurrency(bid.price)}
                  </span>
                  <span className="text-sm text-slate-300">{bid.quantity}</span>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

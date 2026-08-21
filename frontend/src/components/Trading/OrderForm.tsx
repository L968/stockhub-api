import { useState } from 'react';
import { TrendingUp, TrendingDown } from 'lucide-react';
import { orderService } from '../../services/api';
import { OrderSide } from '../../types';
import type { Stock } from '../../types';

interface OrderFormProps {
  stock: Stock;
  onOrderPlaced: () => void;
}

export default function OrderForm({ stock, onOrderPlaced }: OrderFormProps) {
  const [side, setSide] = useState<OrderSide>(OrderSide.Buy);
  const [price, setPrice] = useState('');
  const [quantity, setQuantity] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setLoading(true);

    try {
      await orderService.create({
        stockId: stock.id,
        side,
        price: parseFloat(price),
        quantity: parseInt(quantity),
      });

      setSuccess('Order placed successfully!');
      setPrice('');
      setQuantity('');
      onOrderPlaced();

      setTimeout(() => setSuccess(''), 3000);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to place order');
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const totalValue = price && quantity ? parseFloat(price) * parseInt(quantity) : 0;

  return (
    <div className="bg-slate-800 rounded-xl shadow-lg border border-slate-700 p-6">
      <h2 className="text-xl font-bold text-slate-100 mb-6">Place Order</h2>

      <div className="mb-6">
        <div className="flex items-center gap-2 mb-2">
          <h3 className="text-lg font-semibold text-slate-100">{stock.symbol}</h3>
          <span className="text-sm text-slate-400">{stock.name}</span>
        </div>
        {stock.sector && <p className="text-sm text-slate-500">{stock.sector}</p>}
      </div>

      {error && (
        <div className="mb-4 p-3 bg-rose-500/20 border border-rose-500/30 text-rose-400 rounded-lg text-sm">
          {error}
        </div>
      )}

      {success && (
        <div className="mb-4 p-3 bg-emerald-500/20 border border-emerald-500/30 text-emerald-400 rounded-lg text-sm">
          {success}
        </div>
      )}

      <div className="flex gap-2 mb-6">
        <button
          type="button"
          onClick={() => setSide(OrderSide.Buy)}
          className={`flex-1 py-3 rounded-lg font-medium transition ${
            side === OrderSide.Buy
              ? 'bg-emerald-500 text-white shadow-lg shadow-emerald-500/20'
              : 'bg-slate-700 text-slate-300 hover:bg-slate-600 border border-slate-600'
          }`}
        >
          <div className="flex items-center justify-center gap-2">
            <TrendingUp className="w-4 h-4" />
            Buy
          </div>
        </button>
        <button
          type="button"
          onClick={() => setSide(OrderSide.Sell)}
          className={`flex-1 py-3 rounded-lg font-medium transition ${
            side === OrderSide.Sell
              ? 'bg-rose-500 text-white shadow-lg shadow-rose-500/20'
              : 'bg-slate-700 text-slate-300 hover:bg-slate-600 border border-slate-600'
          }`}
        >
          <div className="flex items-center justify-center gap-2">
            <TrendingDown className="w-4 h-4" />
            Sell
          </div>
        </button>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label htmlFor="price" className="block text-sm font-medium text-slate-300 mb-1">
            Price per Share
          </label>
          <input
            id="price"
            type="number"
            step="0.01"
            min="0.01"
            value={price}
            onChange={(e) => setPrice(e.target.value)}
            required
            className="w-full px-4 py-2 bg-slate-700 border border-slate-600 text-slate-100 rounded-lg focus:ring-2 focus:ring-cyan-500 focus:border-cyan-500 transition"
            placeholder="0.00"
          />
        </div>

        <div>
          <label htmlFor="quantity" className="block text-sm font-medium text-slate-300 mb-1">
            Quantity
          </label>
          <input
            id="quantity"
            type="number"
            min="1"
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
            required
            className="w-full px-4 py-2 bg-slate-700 border border-slate-600 text-slate-100 rounded-lg focus:ring-2 focus:ring-cyan-500 focus:border-cyan-500 transition"
            placeholder="0"
          />
        </div>

        {totalValue > 0 && (
          <div className="p-4 bg-slate-700/50 rounded-lg border border-slate-600/50">
            <div className="flex justify-between items-center">
              <span className="text-sm text-slate-300">Total Value</span>
              <span className="text-lg font-bold text-cyan-400">
                {formatCurrency(totalValue)}
              </span>
            </div>
          </div>
        )}

        <button
          type="submit"
          disabled={loading}
          className={`w-full py-3 rounded-lg font-medium transition ${
            side === OrderSide.Buy
              ? 'bg-emerald-500 hover:bg-emerald-600 text-white shadow-lg shadow-emerald-500/20'
              : 'bg-rose-500 hover:bg-rose-600 text-white shadow-lg shadow-rose-500/20'
          } disabled:opacity-50 disabled:cursor-not-allowed`}
        >
          {loading ? 'Placing Order...' : `Place ${side === OrderSide.Buy ? 'Buy' : 'Sell'} Order`}
        </button>
      </form>
    </div>
  );
}

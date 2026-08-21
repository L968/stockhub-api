import { useState } from 'react';
import StockSearch from '../components/Trading/StockSearch';
import OrderBook from '../components/Trading/OrderBook';
import OrderForm from '../components/Trading/OrderForm';
import type { Stock } from '../types';

export default function Trading() {
  const [selectedStock, setSelectedStock] = useState<Stock | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);

  const handleOrderPlaced = () => {
    setRefreshKey(prev => prev + 1);
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <h1 className="text-3xl font-bold text-slate-100 mb-8">Trading</h1>

      <div className="mb-8">
        <StockSearch onSelectStock={setSelectedStock} />
      </div>

      {selectedStock ? (
        <div className="grid lg:grid-cols-2 gap-6">
          <div className="space-y-6">
            <OrderForm stock={selectedStock} onOrderPlaced={handleOrderPlaced} />
          </div>
          <div>
            <OrderBook symbol={selectedStock.symbol} key={refreshKey} />
          </div>
        </div>
      ) : (
        <div className="bg-slate-800 rounded-xl shadow-lg border border-slate-700 p-12 text-center">
          <p className="text-slate-300 text-lg">Search for a stock to start trading</p>
          <p className="text-slate-500 text-sm mt-2">
            Use the search bar above to find stocks
          </p>
        </div>
      )}
    </div>
  );
}

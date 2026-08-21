import { useState, useEffect } from 'react';
import { Search } from 'lucide-react';
import { stockService } from '../../services/api';
import type { Stock } from '../../types';

interface StockSearchProps {
  onSelectStock: (stock: Stock) => void;
}

export default function StockSearch({ onSelectStock }: StockSearchProps) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<Stock[]>([]);
  const [loading, setLoading] = useState(false);
  const [showResults, setShowResults] = useState(false);

  useEffect(() => {
    const searchStocks = async () => {
      if (query.length < 2) {
        setResults([]);
        return;
      }

      setLoading(true);
      try {
        const stocks = await stockService.search(query);
        setResults(stocks);
        setShowResults(true);
      } catch (error) {
        console.error('Search error:', error);
      } finally {
        setLoading(false);
      }
    };

    const debounce = setTimeout(searchStocks, 300);
    return () => clearTimeout(debounce);
  }, [query]);

  const handleSelect = (stock: Stock) => {
    onSelectStock(stock);
    setQuery('');
    setShowResults(false);
  };

  return (
    <div className="relative">
      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-slate-400 w-5 h-5" />
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onFocus={() => query.length >= 2 && setShowResults(true)}
          onBlur={() => setTimeout(() => setShowResults(false), 200)}
          placeholder="Search stocks..."
          className="w-full pl-10 pr-4 py-3 bg-slate-800 border border-slate-600 text-slate-100 rounded-lg focus:ring-2 focus:ring-cyan-500 focus:border-cyan-500 transition"
        />
      </div>

      {showResults && (
        <div className="absolute z-10 w-full mt-2 bg-slate-800 border border-slate-700 rounded-lg shadow-2xl max-h-96 overflow-y-auto">
          {loading ? (
            <div className="p-4 text-center text-slate-400">Searching...</div>
          ) : results.length === 0 ? (
            <div className="p-4 text-center text-slate-400">No stocks found</div>
          ) : (
            <div className="py-2">
              {results.map((stock) => (
                <button
                  key={stock.id}
                  onClick={() => handleSelect(stock)}
                  className="w-full px-4 py-3 text-left hover:bg-slate-700 transition"
                >
                  <div className="font-semibold text-slate-100">{stock.symbol}</div>
                  <div className="text-sm text-slate-400">{stock.name}</div>
                </button>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

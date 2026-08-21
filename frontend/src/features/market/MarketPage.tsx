import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, Search, TrendingDown, TrendingUp } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ErrorBlock, LoadingBlock } from '../../components/Feedback';
import { api } from '../../lib/api';
import { formatCurrency, formatNumber } from '../../lib/format';
import type { Stock } from '../../types/api';
import { OrderBookPanel } from './OrderBookPanel';
import { OrderTicket } from './OrderTicket';

export function MarketPage() {
  const { symbol } = useParams();
  const stocks = useQuery({ queryKey: ['stocks'], queryFn: api.stocks.all });
  const selected = stocks.data?.find((stock) => stock.symbol.toLowerCase() === symbol?.toLowerCase());

  if (stocks.isLoading) return <LoadingBlock label="Opening the market" />;
  if (stocks.isError) return <ErrorBlock message={stocks.error.message} retry={() => void stocks.refetch()} />;

  if (!symbol) return <MarketOverview stocks={stocks.data ?? []} />;
  if (!selected) return <ErrorBlock message={`Asset ${symbol.toUpperCase()} was not found.`} />;

  return <TradingWorkspace stock={selected} />;
}

function MarketOverview({ stocks }: { stocks: Stock[] }) {
  const [search, setSearch] = useState('');
  const visible = useMemo(() => {
    const term = search.trim().toLowerCase();
    return stocks.filter((stock) => !term || stock.symbol.toLowerCase().includes(term) || stock.name.toLowerCase().includes(term));
  }, [search, stocks]);

  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow">Market</p>
        <h1 className="page-title">Choose an asset to trade.</h1>
        <p className="mt-2 max-w-2xl text-sm text-slate-500">Prices reflect the latest execution processed by the matching engine.</p>
      </div>

      <div className="panel overflow-hidden">
        <div className="panel-heading gap-4">
          <div><h2 className="section-title">Listed assets</h2><p className="mt-1 text-xs text-slate-500">{stocks.length} symbols available</p></div>
          <label className="relative w-full max-w-xs">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-600" size={16} />
            <span className="sr-only">Search assets</span>
            <input className="field py-2 pl-9" onChange={(event) => setSearch(event.target.value)} placeholder="Search symbol or company" value={search} />
          </label>
        </div>
        <div className="overflow-x-auto">
          <table className="data-table">
            <thead><tr><th>Asset</th><th>Last price</th><th>Change</th><th>Day range</th><th>Volume</th><th><span className="sr-only">Action</span></th></tr></thead>
            <tbody>
              {visible.map((stock) => <StockRow key={stock.id} stock={stock} />)}
            </tbody>
          </table>
        </div>
        {visible.length === 0 && <p className="p-10 text-center text-sm text-slate-500">No assets match “{search}”.</p>}
      </div>
    </div>
  );
}

function StockRow({ stock }: { stock: Stock }) {
  const positive = stock.changePercent >= 0;
  return (
    <tr>
      <td><Link className="group" to={`/market/${stock.symbol}`}><span className="symbol-link">{stock.symbol}</span><span className="mt-1 block text-xs text-slate-500 group-hover:text-slate-400">{stock.name}</span></Link></td>
      <td className="font-mono text-white">{formatCurrency(stock.lastPrice)}</td>
      <td><span className={`inline-flex items-center gap-1 font-mono text-sm ${positive ? 'text-emerald-400' : 'text-rose-400'}`}>{positive ? <TrendingUp size={14} /> : <TrendingDown size={14} />}{positive ? '+' : ''}{stock.changePercent.toFixed(2)}%</span></td>
      <td className="font-mono text-xs">{formatCurrency(stock.minPrice)} — {formatCurrency(stock.maxPrice)}</td>
      <td className="font-mono">{formatNumber(stock.volume)}</td>
      <td className="text-right"><Link className="button-ghost" to={`/market/${stock.symbol}`}>Trade</Link></td>
    </tr>
  );
}

function TradingWorkspace({ stock }: { stock: Stock }) {
  const [selectedPrice, setSelectedPrice] = useState<number>();
  const positive = stock.changePercent >= 0;

  return (
    <div className="space-y-6">
      <div>
        <Link className="mb-4 inline-flex items-center gap-1 text-xs text-slate-500 hover:text-slate-300" to="/market"><ArrowLeft size={14} /> All assets</Link>
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div><p className="eyebrow">{stock.name}</p><h1 className="page-title">{stock.symbol}</h1></div>
          <div className="text-right"><p className="font-mono text-3xl font-medium text-white">{formatCurrency(stock.lastPrice)}</p><p className={`mt-1 font-mono text-sm ${positive ? 'text-emerald-400' : 'text-rose-400'}`}>{positive ? '+' : ''}{stock.changePercent.toFixed(2)}% today</p></div>
        </div>
      </div>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.4fr)_minmax(320px,0.6fr)]">
        <OrderBookPanel onSelectPrice={setSelectedPrice} symbol={stock.symbol} />
        <OrderTicket key={stock.id} selectedPrice={selectedPrice} stock={stock} />
      </div>
    </div>
  );
}

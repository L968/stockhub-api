import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, ChevronRight, Search, TrendingDown, TrendingUp } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ErrorBlock, LoadingBlock } from '../../components/Feedback';
import { api } from '../../lib/api';
import { formatCompactNumber, formatCurrency, formatPercent, formatPrice } from '../../lib/format';
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
    <div className="space-y-5">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="eyebrow">Markets</p>
          <h1 className="page-title">Stocks</h1>
          <p className="mt-2 max-w-2xl text-sm text-slate-400">Live snapshots from the StockHub matching engine.</p>
        </div>
        <p className="rounded-full border border-line bg-panel px-3 py-1.5 text-xs font-medium text-slate-400">
          {stocks.length} assets listed
        </p>
      </div>

      <div className="panel overflow-hidden">
        <div className="panel-heading gap-4">
          <div><h2 className="section-title">Market overview</h2><p className="mt-1 text-xs text-slate-500">Select a stock to open its order book</p></div>
          <label className="relative w-full max-w-sm">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" size={16} />
            <span className="sr-only">Search assets</span>
            <input className="field mt-0 py-2.5 pl-9" onChange={(event) => setSearch(event.target.value)} placeholder="Search by symbol or company" value={search} />
          </label>
        </div>
        <div className="overflow-x-auto">
          <table className="data-table market-table">
            <thead><tr><th>Stock</th><th className="text-right">Last price</th><th className="text-right">Today</th><th>Day range</th><th className="text-right">Volume</th><th><span className="sr-only">Open market</span></th></tr></thead>
            <tbody>{visible.map((stock) => <StockRow key={stock.id} stock={stock} />)}</tbody>
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
    <tr className="group">
      <td>
        <Link className="flex items-center gap-3" to={`/market/${stock.symbol}`}>
          <span className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-slate-800 text-xs font-bold text-slate-200">{stock.symbol.slice(0, 2)}</span>
          <span><strong className="block text-sm text-white">{stock.symbol}</strong><span className="mt-0.5 block text-xs text-slate-500 group-hover:text-slate-400">{stock.name}</span></span>
        </Link>
      </td>
      <td className="number text-right text-[15px] font-semibold text-white">{formatCurrency(stock.lastPrice)}</td>
      <td className="text-right"><span className={`inline-flex items-center justify-end gap-1 number text-sm font-medium ${positive ? 'text-emerald-400' : 'text-rose-400'}`}>{positive ? <TrendingUp size={14} /> : <TrendingDown size={14} />}{formatPercent(stock.changePercent)}</span></td>
      <td><DayRange stock={stock} /></td>
      <td className="number text-right text-sm text-slate-300" title={stock.volume.toLocaleString('en-US')}>{formatCompactNumber(stock.volume)}</td>
      <td className="w-12 text-right"><Link aria-label={`Trade ${stock.symbol}`} className="inline-grid h-9 w-9 place-items-center rounded-full text-slate-500 transition group-hover:bg-slate-800 group-hover:text-white" to={`/market/${stock.symbol}`}><ChevronRight size={17} /></Link></td>
    </tr>
  );
}

function DayRange({ stock }: { stock: Stock }) {
  const range = stock.maxPrice - stock.minPrice;
  const position = range > 0 ? Math.min(100, Math.max(0, ((stock.lastPrice - stock.minPrice) / range) * 100)) : 50;

  return (
    <div className="min-w-44 max-w-56">
      <div className="relative h-1.5 rounded-full bg-slate-800">
        <span className="absolute inset-y-0 left-0 rounded-full bg-slate-600" style={{ width: `${position}%` }} />
        <span className="absolute top-1/2 h-2.5 w-2.5 -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-panel bg-slate-300" style={{ left: `${position}%` }} />
      </div>
      <div className="mt-2 flex justify-between number text-[11px] text-slate-500"><span>${formatPrice(stock.minPrice)}</span><span>${formatPrice(stock.maxPrice)}</span></div>
    </div>
  );
}

function TradingWorkspace({ stock }: { stock: Stock }) {
  const [selectedPrice, setSelectedPrice] = useState<number>();
  const positive = stock.changePercent >= 0;
  const account = useQuery({ queryKey: ['me'], queryFn: api.auth.me });
  const portfolio = useQuery({ queryKey: ['portfolio'], queryFn: api.portfolio.mine });
  const availableShares = portfolio.data?.positions.find((position) => position.symbol === stock.symbol)?.quantity ?? 0;

  return (
    <div className="space-y-4">
      <Link className="inline-flex items-center gap-1.5 text-sm text-slate-500 transition hover:text-slate-200" to="/market"><ArrowLeft size={15} /> Markets</Link>

      <section className="panel grid gap-6 p-5 lg:grid-cols-[minmax(230px,1fr)_auto] lg:items-center">
        <div className="flex items-center gap-4">
          <span className="grid h-12 w-12 place-items-center rounded-full bg-slate-800 text-sm font-bold text-white">{stock.symbol.slice(0, 2)}</span>
          <div><div className="flex items-center gap-2"><h1 className="text-2xl font-semibold text-white">{stock.symbol}</h1><span className="rounded bg-slate-800 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-slate-400">Stock</span></div><p className="mt-1 text-sm text-slate-400">{stock.name}</p></div>
        </div>
        <div className="grid grid-cols-2 gap-x-8 gap-y-4 sm:grid-cols-4 lg:min-w-[650px]">
          <div><p className="market-label">Last price</p><p className="number mt-1 text-2xl font-semibold text-white">{formatCurrency(stock.lastPrice)}</p></div>
          <div><p className="market-label">Today</p><p className={`number mt-1 text-base font-semibold ${positive ? 'text-emerald-400' : 'text-rose-400'}`}>{formatPercent(stock.changePercent)}</p></div>
          <div><p className="market-label">Day range</p><p className="number mt-1 whitespace-nowrap text-sm font-medium text-slate-200">${formatPrice(stock.minPrice)} <span className="text-slate-600">–</span> ${formatPrice(stock.maxPrice)}</p></div>
          <div><p className="market-label">Volume</p><p className="number mt-1 text-sm font-medium text-slate-200">{formatCompactNumber(stock.volume)}</p></div>
        </div>
      </section>

      <div className="grid gap-4 xl:grid-cols-[minmax(0,1.45fr)_minmax(340px,0.55fr)]">
        <OrderBookPanel onSelectPrice={setSelectedPrice} symbol={stock.symbol} />
        <OrderTicket availableCash={account.data?.currentBalance} availableShares={availableShares} key={stock.id} selectedPrice={selectedPrice} stock={stock} />
      </div>
    </div>
  );
}

import { useQuery } from '@tanstack/react-query';
import { Radio } from 'lucide-react';
import { ErrorBlock, LoadingBlock } from '../../components/Feedback';
import { api } from '../../lib/api';
import { formatCurrency, formatDate, formatNumber, formatPrice } from '../../lib/format';
import type { OrderBookLevel } from '../../types/api';

export function OrderBookPanel({ symbol, onSelectPrice }: { symbol: string; onSelectPrice: (price: number) => void }) {
  const book = useQuery({
    queryKey: ['order-book', symbol],
    queryFn: () => api.stocks.orderBook(symbol),
    refetchInterval: 2_500,
  });

  if (book.isLoading) return <LoadingBlock label="Loading order book" />;
  if (book.isError) return <ErrorBlock message={book.error.message} retry={() => void book.refetch()} />;

  const bids = book.data?.bids.slice(0, 12) ?? [];
  const asks = book.data?.asks.slice(0, 12) ?? [];
  const maxQuantity = Math.max(1, ...bids.map((level) => level.quantity), ...asks.map((level) => level.quantity));
  const bestBid = bids[0]?.price;
  const bestAsk = asks[0]?.price;
  const spread = bestBid !== undefined && bestAsk !== undefined ? bestAsk - bestBid : null;
  const midpoint = bestBid !== undefined && bestAsk !== undefined ? (bestBid + bestAsk) / 2 : null;
  const spreadPercent = spread !== null && midpoint ? (spread / midpoint) * 100 : null;

  return (
    <section className="panel min-h-[500px] overflow-hidden">
      <div className="panel-heading gap-4">
        <div><p className="eyebrow">Market depth</p><h2 className="section-title">Order book</h2><p className="mt-1 text-xs text-slate-500">Click any price to fill the limit order ticket.</p></div>
        <div className="text-right"><span className="inline-flex items-center gap-1.5 text-xs font-medium text-emerald-400"><Radio className="animate-pulse" size={12} /> Live</span><p className="mt-1 number text-[11px] text-slate-500">{book.data?.updatedAtUtc ? formatDate(book.data.updatedAtUtc) : 'Waiting for update'}</p></div>
      </div>

      <div className="grid grid-cols-3 border-b border-line bg-ink/35">
        <MarketQuote label="Best bid" tone="bid" value={bestBid} />
        <MarketQuote label="Spread" subvalue={spreadPercent === null ? undefined : `${spreadPercent.toFixed(2)}%`} value={spread ?? undefined} />
        <MarketQuote label="Best ask" tone="ask" value={bestAsk} />
      </div>

      {bids.length === 0 && asks.length === 0 ? (
        <p className="grid min-h-72 place-items-center p-12 text-center text-sm text-slate-500">The book is empty. Place the first order.</p>
      ) : (
        <div className="grid md:grid-cols-2">
          <BookSide levels={bids} maxQuantity={maxQuantity} onSelect={onSelectPrice} side="bid" />
          <BookSide levels={asks} maxQuantity={maxQuantity} onSelect={onSelectPrice} side="ask" />
        </div>
      )}
    </section>
  );
}

function MarketQuote({ label, value, subvalue, tone }: {
  label: string;
  value?: number;
  subvalue?: string;
  tone?: 'bid' | 'ask';
}) {
  const color = tone === 'bid' ? 'text-emerald-400' : tone === 'ask' ? 'text-rose-400' : 'text-slate-100';
  return (
    <div className="border-r border-line px-4 py-3 last:border-r-0">
      <p className="market-label">{label}</p>
      <div className="mt-1 flex items-baseline gap-2"><strong className={`number text-sm font-semibold ${color}`}>{value === undefined ? '—' : formatCurrency(value)}</strong>{subvalue && <span className="number text-[10px] text-slate-500">{subvalue}</span>}</div>
    </div>
  );
}

function BookSide({ levels, side, maxQuantity, onSelect }: {
  levels: OrderBookLevel[];
  side: 'bid' | 'ask';
  maxQuantity: number;
  onSelect: (price: number) => void;
}) {
  const bid = side === 'bid';
  return (
    <div className="border-b border-line md:border-b-0 md:border-r md:last:border-r-0">
      <div className="flex items-center justify-between border-b border-line px-4 py-3">
        <div><p className={`text-sm font-semibold ${bid ? 'text-emerald-400' : 'text-rose-400'}`}>{bid ? 'Bids' : 'Asks'}</p><p className="mt-0.5 text-[11px] text-slate-500">{bid ? 'Buy orders' : 'Sell orders'}</p></div>
        <span className="rounded-full bg-slate-800 px-2 py-1 text-[10px] font-semibold text-slate-400">{levels.length} levels</span>
      </div>
      <div className="grid grid-cols-[1fr_0.8fr_0.55fr] border-b border-line bg-ink/25 px-4 py-2 market-label"><span>Price (USD)</span><span className="text-right">Shares</span><span className="text-right">Orders</span></div>
      <div>
        {levels.map((level, index) => (
          <BookRow key={`${side}-${level.price}-${index}`} level={level} maxQuantity={maxQuantity} onSelect={onSelect} side={side} />
        ))}
        {levels.length === 0 && <p className="px-4 py-12 text-center text-sm text-slate-500">No {bid ? 'bids' : 'asks'} yet.</p>}
      </div>
    </div>
  );
}

function BookRow({ level, side, maxQuantity, onSelect }: {
  level: OrderBookLevel;
  side: 'bid' | 'ask';
  maxQuantity: number;
  onSelect: (price: number) => void;
}) {
  const width = `${Math.max(3, (level.quantity / maxQuantity) * 100)}%`;
  const bid = side === 'bid';
  return (
    <button className="group relative grid w-full grid-cols-[1fr_0.8fr_0.55fr] overflow-hidden px-4 py-3 text-left transition hover:bg-white/[0.035]" onClick={() => onSelect(level.price)} title={`Use $${formatPrice(level.price)} as limit price`} type="button">
      <span className={`absolute inset-y-1 right-0 rounded-l opacity-[0.08] ${bid ? 'bg-emerald-400' : 'bg-rose-400'}`} style={{ width }} />
      <span className={`number relative text-sm font-medium ${bid ? 'text-emerald-400' : 'text-rose-400'}`}>${formatPrice(level.price)}</span>
      <span className="number relative text-right text-sm text-slate-300">{formatNumber(level.quantity)}</span>
      <span className="number relative text-right text-sm text-slate-500">{level.orderCount}</span>
    </button>
  );
}

import { useQuery } from '@tanstack/react-query';
import { Radio } from 'lucide-react';
import { api } from '../../lib/api';
import { formatCurrency, formatDate } from '../../lib/format';
import type { OrderBookLevel } from '../../types/api';
import { ErrorBlock, LoadingBlock } from '../../components/Feedback';

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
  const spread = bids[0] && asks[0] ? asks[0].price - bids[0].price : null;

  return (
    <section className="panel overflow-hidden">
      <div className="panel-heading">
        <div><p className="eyebrow">Live depth</p><h2 className="section-title">Order book</h2></div>
        <div className="text-right"><span className="inline-flex items-center gap-1.5 text-xs text-emerald-400"><Radio className="animate-pulse" size={12} /> Live</span><p className="mt-1 font-mono text-[10px] text-slate-600">{book.data?.updatedAtUtc ? formatDate(book.data.updatedAtUtc) : 'Waiting for update'}</p></div>
      </div>
      <div className="grid grid-cols-2 border-b border-line bg-ink/30 px-4 py-2 font-mono text-[10px] uppercase tracking-wider text-slate-600"><span>Price</span><span className="grid grid-cols-2 text-right"><span>Orders</span><span>Qty</span></span></div>
      <div className="max-h-[510px] overflow-y-auto">
        {[...asks].reverse().map((level, index) => <BookRow key={`ask-${level.price}-${index}`} level={level} maxQuantity={maxQuantity} onSelect={onSelectPrice} side="ask" />)}
        <div className="flex items-center justify-between border-y border-line bg-ink/60 px-4 py-3"><span className="font-mono text-[10px] uppercase tracking-wider text-slate-600">Spread</span><span className="font-mono text-xs text-slate-400">{spread === null ? '—' : formatCurrency(spread)}</span></div>
        {bids.map((level, index) => <BookRow key={`bid-${level.price}-${index}`} level={level} maxQuantity={maxQuantity} onSelect={onSelectPrice} side="bid" />)}
        {bids.length === 0 && asks.length === 0 && <p className="p-12 text-center text-sm text-slate-500">The book is empty. Place the first order.</p>}
      </div>
    </section>
  );
}

function BookRow({ level, side, maxQuantity, onSelect }: { level: OrderBookLevel; side: 'bid' | 'ask'; maxQuantity: number; onSelect: (price: number) => void }) {
  const width = `${Math.max(4, (level.quantity / maxQuantity) * 100)}%`;
  return (
    <button className="relative grid w-full grid-cols-2 px-4 py-2.5 text-left font-mono text-xs hover:bg-white/[0.03]" onClick={() => onSelect(level.price)} title="Use this price" type="button">
      <span className={`absolute inset-y-0 right-0 opacity-[0.08] ${side === 'bid' ? 'bg-emerald-400' : 'bg-rose-400'}`} style={{ width }} />
      <span className={side === 'bid' ? 'text-emerald-400' : 'text-rose-400'}>{formatCurrency(level.price)}</span>
      <span className="relative grid grid-cols-2 text-right text-slate-400"><span>{level.orderCount}</span><span>{level.quantity}</span></span>
    </button>
  );
}

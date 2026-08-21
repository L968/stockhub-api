import { useMutation, useQueryClient } from '@tanstack/react-query';
import { CheckCircle2 } from 'lucide-react';
import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { api } from '../../lib/api';
import { formatCurrency, formatNumber } from '../../lib/format';
import { OrderSide, type Stock } from '../../types/api';

type OrderTicketProps = {
  stock: Stock;
  selectedPrice?: number;
  availableCash?: number;
  availableShares?: number;
};

export function OrderTicket({ stock, selectedPrice, availableCash, availableShares = 0 }: OrderTicketProps) {
  const [side, setSide] = useState<OrderSide>(OrderSide.Buy);
  const [price, setPrice] = useState(stock.lastPrice > 0 ? stock.lastPrice.toFixed(2) : '');
  const [quantity, setQuantity] = useState('1');
  const queryClient = useQueryClient();

  useEffect(() => {
    if (selectedPrice !== undefined) setPrice(selectedPrice.toFixed(2));
  }, [selectedPrice]);

  const numericPrice = Number(price);
  const numericQuantity = Number(quantity);
  const total = useMemo(() => numericPrice * numericQuantity || 0, [numericPrice, numericQuantity]);
  const validNumbers = numericPrice > 0 && Number.isInteger(numericQuantity) && numericQuantity > 0;
  const exceedsCash = side === OrderSide.Buy && availableCash !== undefined && total > availableCash;
  const exceedsShares = side === OrderSide.Sell && numericQuantity > availableShares;
  const valid = validNumbers && !exceedsCash && !exceedsShares;

  const placeOrder = useMutation({
    mutationFn: api.orders.create,
    onSuccess: async () => {
      setQuantity('1');
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['order-book', stock.symbol] }),
        queryClient.invalidateQueries({ queryKey: ['orders'] }),
        queryClient.invalidateQueries({ queryKey: ['portfolio'] }),
        queryClient.invalidateQueries({ queryKey: ['trades'] }),
        queryClient.invalidateQueries({ queryKey: ['me'] }),
      ]);
    },
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    if (!valid) return;
    placeOrder.mutate({ stockId: stock.id, side, price: numericPrice, quantity: numericQuantity });
  }

  const buying = side === OrderSide.Buy;

  return (
    <section className="panel h-fit overflow-hidden xl:sticky xl:top-20">
      <div className="panel-heading"><div><p className="eyebrow">Limit order</p><h2 className="section-title">Place order</h2></div><span className="rounded bg-slate-800 px-2 py-1 text-xs font-semibold text-slate-300">{stock.symbol}</span></div>
      <form className="space-y-5 p-5" onSubmit={submit}>
        <div className="grid grid-cols-2 rounded-lg bg-ink p-1" role="tablist" aria-label="Order side">
          <button aria-selected={buying} className={`side-button ${buying ? 'side-button-buy' : ''}`} onClick={() => setSide(OrderSide.Buy)} role="tab" type="button">Buy</button>
          <button aria-selected={!buying} className={`side-button ${!buying ? 'side-button-sell' : ''}`} onClick={() => setSide(OrderSide.Sell)} role="tab" type="button">Sell</button>
        </div>

        <div className="flex items-center justify-between text-xs"><span className="text-slate-500">Available to {buying ? 'spend' : 'sell'}</span><strong className="number font-medium text-slate-200">{buying ? (availableCash === undefined ? '—' : formatCurrency(availableCash)) : `${formatNumber(availableShares)} ${stock.symbol}`}</strong></div>

        <div>
          <label className="field-label" htmlFor="limit-price">Limit price</label>
          <span className="relative block"><input id="limit-price" className="field number pr-14 text-base" inputMode="decimal" min="0.01" onChange={(event) => setPrice(event.target.value)} required step="0.01" type="number" value={price} /><span className="pointer-events-none absolute bottom-3.5 right-3 text-xs font-semibold text-slate-500">USD</span></span>
        </div>
        <div>
          <label className="field-label" htmlFor="order-quantity">Quantity</label>
          <span className="relative block"><input id="order-quantity" className="field number pr-16 text-base" inputMode="numeric" min="1" onChange={(event) => setQuantity(event.target.value)} required step="1" type="number" value={quantity} /><span className="pointer-events-none absolute bottom-3.5 right-3 text-xs font-semibold text-slate-500">SHARES</span></span>
        </div>

        <div className="space-y-3 rounded-xl border border-line bg-ink/50 p-4">
          <div className="flex justify-between text-sm text-slate-500"><span>Order type</span><span className="font-medium text-slate-300">Limit</span></div>
          <div className="flex justify-between text-sm text-slate-500"><span>{validNumbers ? `${formatNumber(numericQuantity)} × ${formatCurrency(numericPrice)}` : 'Order value'}</span><strong className="number text-base font-semibold text-white">{formatCurrency(total)}</strong></div>
        </div>

        {exceedsCash && <p className="text-sm text-amber-300">This order exceeds your available cash.</p>}
        {exceedsShares && <p className="text-sm text-amber-300">You do not have enough {stock.symbol} shares.</p>}
        {placeOrder.isError && <p className="rounded-lg border border-rose-500/30 bg-rose-500/10 p-3 text-sm text-rose-300" role="alert">{placeOrder.error.message}</p>}
        {placeOrder.isSuccess && <p className="flex items-center gap-2 rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-3 text-sm text-emerald-300" role="status"><CheckCircle2 size={16} /> Order accepted and sent to matching.</p>}

        <button className={buying ? 'button-buy w-full' : 'button-sell w-full'} disabled={!valid || placeOrder.isPending} type="submit">
          {placeOrder.isPending ? 'Sending order…' : `${buying ? 'Buy' : 'Sell'} ${stock.symbol}`}
        </button>
        <p className="text-center text-[11px] leading-relaxed text-slate-500">Limit orders execute at your price or better when matching liquidity is available.</p>
      </form>
    </section>
  );
}

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowDownToLine, ArrowUpFromLine, CheckCircle2 } from 'lucide-react';
import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { api } from '../../lib/api';
import { formatCurrency } from '../../lib/format';
import { OrderSide, type Stock } from '../../types/api';

export function OrderTicket({ stock, selectedPrice }: { stock: Stock; selectedPrice?: number }) {
  const [side, setSide] = useState<OrderSide>(OrderSide.Buy);
  const [price, setPrice] = useState(stock.lastPrice > 0 ? String(stock.lastPrice) : '');
  const [quantity, setQuantity] = useState('1');
  const queryClient = useQueryClient();

  useEffect(() => {
    if (selectedPrice !== undefined) setPrice(String(selectedPrice));
  }, [selectedPrice]);

  const numericPrice = Number(price);
  const numericQuantity = Number(quantity);
  const total = useMemo(() => numericPrice * numericQuantity || 0, [numericPrice, numericQuantity]);
  const valid = numericPrice > 0 && Number.isInteger(numericQuantity) && numericQuantity > 0;

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

  return (
    <section className="panel h-fit overflow-hidden xl:sticky xl:top-24">
      <div className="panel-heading"><div><p className="eyebrow">Limit order</p><h2 className="section-title">Order ticket</h2></div><span className="font-mono text-xs text-slate-500">{stock.symbol}</span></div>
      <form className="space-y-5 p-5" onSubmit={submit}>
        <div className="grid grid-cols-2 gap-2 rounded-xl bg-ink p-1">
          <button className={`side-button ${side === OrderSide.Buy ? 'side-button-buy' : ''}`} onClick={() => setSide(OrderSide.Buy)} type="button"><ArrowDownToLine size={15} /> Buy</button>
          <button className={`side-button ${side === OrderSide.Sell ? 'side-button-sell' : ''}`} onClick={() => setSide(OrderSide.Sell)} type="button"><ArrowUpFromLine size={15} /> Sell</button>
        </div>
        <label className="field-label">Limit price<input className="field font-mono" min="0.01" onChange={(event) => setPrice(event.target.value)} required step="0.01" type="number" value={price} /></label>
        <label className="field-label">Quantity<input className="field font-mono" min="1" onChange={(event) => setQuantity(event.target.value)} required step="1" type="number" value={quantity} /></label>
        <div className="rounded-xl border border-line bg-ink/60 p-4"><div className="flex justify-between text-sm text-slate-500"><span>Estimated total</span><strong className="font-mono font-medium text-white">{formatCurrency(total)}</strong></div></div>

        {placeOrder.isError && <p className="rounded-lg border border-rose-500/30 bg-rose-500/10 p-3 text-sm text-rose-300" role="alert">{placeOrder.error.message}</p>}
        {placeOrder.isSuccess && <p className="flex items-center gap-2 rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-3 text-sm text-emerald-300" role="status"><CheckCircle2 size={16} /> Order accepted for processing.</p>}

        <button className={side === OrderSide.Buy ? 'button-buy w-full' : 'button-sell w-full'} disabled={!valid || placeOrder.isPending} type="submit">
          {placeOrder.isPending ? 'Sending order…' : `${side === OrderSide.Buy ? 'Buy' : 'Sell'} ${stock.symbol}`}
        </button>
        <p className="text-center text-[11px] leading-relaxed text-slate-600">Orders are processed asynchronously. The book refreshes automatically.</p>
      </form>
    </section>
  );
}

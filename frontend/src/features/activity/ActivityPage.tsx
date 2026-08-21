import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Clock3, ListOrdered } from 'lucide-react';
import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { EmptyState, ErrorBlock, LoadingBlock } from '../../components/Feedback';
import { SideBadge, StatusBadge } from '../../components/MarketBadges';
import { Pagination } from '../../components/Pagination';
import { api } from '../../lib/api';
import { formatCurrency, formatDate } from '../../lib/format';

type Tab = 'orders' | 'trades';

export function ActivityPage() {
  const [params, setParams] = useSearchParams();
  const tab: Tab = params.get('tab') === 'orders' ? 'orders' : 'trades';
  const [orderPage, setOrderPage] = useState(1);
  const [tradePage, setTradePage] = useState(1);

  function selectTab(next: Tab) {
    setParams({ tab: next }, { replace: true });
  }

  return (
    <div className="space-y-6">
      <div><p className="eyebrow">Audit trail</p><h1 className="page-title">Trading activity.</h1><p className="mt-2 text-sm text-slate-500">Every order submitted and every execution produced by the engine.</p></div>
      <div className="inline-flex rounded-xl border border-line bg-panel p-1" role="tablist">
        <button aria-selected={tab === 'trades'} className={`tab-button ${tab === 'trades' ? 'tab-button-active' : ''}`} onClick={() => selectTab('trades')} role="tab" type="button">Trades</button>
        <button aria-selected={tab === 'orders'} className={`tab-button ${tab === 'orders' ? 'tab-button-active' : ''}`} onClick={() => selectTab('orders')} role="tab" type="button">Orders</button>
      </div>
      {tab === 'orders'
        ? <OrdersTable onPageChange={setOrderPage} page={orderPage} />
        : <TradesTable onPageChange={setTradePage} page={tradePage} />}
    </div>
  );
}

function OrdersTable({ page, onPageChange }: { page: number; onPageChange: (page: number) => void }) {
  const result = useQuery({
    queryKey: ['orders', page, 20],
    queryFn: () => api.orders.mine(page, 20),
    placeholderData: keepPreviousData,
  });
  if (result.isLoading) return <LoadingBlock label="Loading orders" />;
  if (result.isError) return <ErrorBlock message={result.error.message} retry={() => void result.refetch()} />;
  const orders = result.data?.items ?? [];

  return (
    <section className={`panel overflow-hidden ${result.isFetching ? 'opacity-70' : ''}`}>
      <div className="panel-heading"><h2 className="section-title">Orders</h2><span className="number text-xs text-slate-500">{result.data?.totalItems ?? orders.length} total</span></div>
      {orders.length === 0 ? <EmptyState icon={ListOrdered} title="No orders yet" description="Your submitted orders will appear here." /> : (
        <div className="overflow-x-auto"><table className="data-table"><thead><tr><th>Date</th><th>Asset</th><th>Side</th><th>Price</th><th>Filled</th><th>Total</th><th>Status</th></tr></thead><tbody>
          {orders.map((order) => <tr key={order.id}><td>{formatDate(order.createdAtUtc)}</td><td><span className="symbol-link">{order.stock.symbol}</span><span className="mt-1 block text-xs text-slate-500">{order.stock.name}</span></td><td><SideBadge side={order.side} /></td><td className="number">{formatCurrency(order.price)}</td><td className="number">{order.filledQuantity} / {order.quantity}</td><td className="number font-medium text-white">{formatCurrency(order.price * order.quantity)}</td><td><StatusBadge status={order.status} /></td></tr>)}
        </tbody></table></div>
      )}
      <Pagination onChange={onPageChange} page={page} totalPages={result.data?.totalPages ?? 1} />
    </section>
  );
}

function TradesTable({ page, onPageChange }: { page: number; onPageChange: (page: number) => void }) {
  const result = useQuery({
    queryKey: ['trades', page, 20],
    queryFn: () => api.trades.mine(page, 20),
    placeholderData: keepPreviousData,
  });
  if (result.isLoading) return <LoadingBlock label="Loading trades" />;
  if (result.isError) return <ErrorBlock message={result.error.message} retry={() => void result.refetch()} />;
  const trades = result.data?.items ?? [];

  return (
    <section className={`panel overflow-hidden ${result.isFetching ? 'opacity-70' : ''}`}>
      <div className="panel-heading"><h2 className="section-title">Executions</h2><span className="number text-xs text-slate-500">{result.data?.totalItems ?? trades.length} total</span></div>
      {trades.length === 0 ? <EmptyState icon={Clock3} title="No trades yet" description="Matched orders will appear here." /> : (
        <div className="overflow-x-auto"><table className="data-table"><thead><tr><th>Date</th><th>Asset</th><th>Side</th><th>Price</th><th>Quantity</th><th>Total</th></tr></thead><tbody>
          {trades.map((trade) => <tr key={trade.id}><td>{formatDate(trade.executedAt)}</td><td><span className="symbol-link">{trade.symbol}</span></td><td><SideBadge side={trade.side} /></td><td className="number">{formatCurrency(trade.price)}</td><td className="number">{trade.quantity}</td><td className="number font-medium text-white">{formatCurrency(trade.price * trade.quantity)}</td></tr>)}
        </tbody></table></div>
      )}
      <Pagination onChange={onPageChange} page={page} totalPages={result.data?.totalPages ?? 1} />
    </section>
  );
}

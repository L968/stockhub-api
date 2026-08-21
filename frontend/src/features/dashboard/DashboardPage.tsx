import { useQuery } from '@tanstack/react-query';
import { ArrowUpRight, Briefcase, Clock3, Landmark, ListOrdered, WalletCards } from 'lucide-react';
import { Link } from 'react-router-dom';
import { EmptyState, ErrorBlock, LoadingBlock } from '../../components/Feedback';
import { SideBadge, StatusBadge } from '../../components/MarketBadges';
import { api } from '../../lib/api';
import { formatCurrency, formatDate } from '../../lib/format';

export function DashboardPage() {
  const user = useQuery({ queryKey: ['me'], queryFn: api.auth.me });
  const portfolio = useQuery({ queryKey: ['portfolio'], queryFn: api.portfolio.mine });
  const orders = useQuery({ queryKey: ['orders', 1, 5], queryFn: () => api.orders.mine(1, 5) });
  const trades = useQuery({ queryKey: ['trades', 1, 5], queryFn: () => api.trades.mine(1, 5) });

  const loading = user.isLoading || portfolio.isLoading || orders.isLoading || trades.isLoading;
  const firstError = [user.error, portfolio.error, orders.error, trades.error].find(Boolean);
  const retry = () => void Promise.all([user.refetch(), portfolio.refetch(), orders.refetch(), trades.refetch()]);

  if (loading) return <LoadingBlock label="Loading your account" />;
  if (firstError && !portfolio.data) {
    return <ErrorBlock message={firstError instanceof Error ? firstError.message : 'Unable to load the dashboard.'} retry={retry} />;
  }

  const positions = portfolio.data?.positions ?? [];
  const recentOrders = orders.data?.items ?? [];
  const recentTrades = trades.data?.items ?? [];
  const openOrders = recentOrders.filter((order) => !['filled', 'cancelled'].includes(order.status.toLowerCase())).length;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="eyebrow">Account overview</p>
          <h1 className="page-title">Good to see you, {user.data?.fullName?.split(' ')[0] ?? 'trader'}.</h1>
        </div>
        <Link className="button-primary" to="/market">
          Open market <ArrowUpRight size={16} />
        </Link>
      </div>

      <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4" aria-label="Account summary">
        <Metric icon={WalletCards} label="Cash balance" value={formatCurrency(user.data?.currentBalance ?? 0)} />
        <Metric icon={Briefcase} label="Portfolio value" value={formatCurrency(portfolio.data?.totalValue ?? 0)} />
        <Metric icon={Landmark} label="Positions" value={String(positions.length)} />
        <Metric icon={ListOrdered} label="Recent open orders" value={String(openOrders)} />
      </section>

      <section className="panel overflow-hidden">
        <div className="panel-heading">
          <div>
            <p className="eyebrow">Holdings</p>
            <h2 className="section-title">Portfolio</h2>
          </div>
          <span className="font-mono text-xs text-slate-500">{positions.length} assets</span>
        </div>
        {positions.length === 0 ? (
          <EmptyState icon={Briefcase} title="No positions yet" description="Executed buy orders will appear here." />
        ) : (
          <div className="overflow-x-auto">
            <table className="data-table">
              <thead><tr><th>Asset</th><th>Quantity</th><th>Avg. price</th><th>Last price</th><th>Market value</th><th>P&amp;L</th></tr></thead>
              <tbody>
                {positions.map((position) => {
                  const profit = position.marketValue - position.quantity * position.avgPrice;
                  const percentage = position.avgPrice ? ((position.currentPrice - position.avgPrice) / position.avgPrice) * 100 : 0;
                  return (
                    <tr key={position.symbol}>
                      <td><Link className="symbol-link" to={`/market/${position.symbol}`}>{position.symbol}</Link></td>
                      <td className="font-mono">{position.quantity}</td>
                      <td>{formatCurrency(position.avgPrice)}</td>
                      <td>{formatCurrency(position.currentPrice)}</td>
                      <td className="font-medium text-white">{formatCurrency(position.marketValue)}</td>
                      <td className={profit >= 0 ? 'text-emerald-400' : 'text-rose-400'}>
                        {profit >= 0 ? '+' : ''}{formatCurrency(profit)} <span className="text-xs opacity-70">({percentage.toFixed(2)}%)</span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <div className="grid gap-6 xl:grid-cols-2">
        <section className="panel overflow-hidden">
          <div className="panel-heading"><h2 className="section-title">Recent orders</h2><Link className="text-link" to="/activity?tab=orders">View all</Link></div>
          {recentOrders.length === 0 ? <EmptyState icon={ListOrdered} title="No orders" description="Place an order from the market." /> : (
            <div className="divide-y divide-line">
              {recentOrders.map((order) => (
                <div className="flex items-center justify-between gap-3 px-5 py-4" key={order.id}>
                  <div className="min-w-0"><div className="flex items-center gap-2"><span className="font-semibold text-white">{order.stock.symbol}</span><SideBadge side={order.side} /></div><p className="mt-1 truncate text-xs text-slate-500">{order.quantity} × {formatCurrency(order.price)} · {formatDate(order.createdAtUtc)}</p></div>
                  <StatusBadge status={order.status} />
                </div>
              ))}
            </div>
          )}
        </section>

        <section className="panel overflow-hidden">
          <div className="panel-heading"><h2 className="section-title">Recent trades</h2><Link className="text-link" to="/activity?tab=trades">View all</Link></div>
          {recentTrades.length === 0 ? <EmptyState icon={Clock3} title="No executions" description="Matched orders will appear here." /> : (
            <div className="divide-y divide-line">
              {recentTrades.map((trade) => (
                <div className="flex items-center justify-between gap-3 px-5 py-4" key={trade.id}>
                  <div><div className="flex items-center gap-2"><span className="font-semibold text-white">{trade.symbol}</span><SideBadge side={trade.side} /></div><p className="mt-1 text-xs text-slate-500">{formatDate(trade.executedAt)}</p></div>
                  <div className="text-right"><p className="font-mono text-sm text-slate-200">{formatCurrency(trade.price * trade.quantity)}</p><p className="mt-1 text-xs text-slate-500">{trade.quantity} @ {formatCurrency(trade.price)}</p></div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}

function Metric({ icon: Icon, label, value }: { icon: typeof WalletCards; label: string; value: string }) {
  return (
    <div className="panel p-5">
      <div className="flex items-center justify-between"><p className="text-sm text-slate-500">{label}</p><Icon className="text-slate-600" size={17} /></div>
      <p className="mt-4 font-mono text-2xl font-medium tracking-tight text-white">{value}</p>
    </div>
  );
}

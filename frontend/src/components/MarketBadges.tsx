import { isBuy } from '../lib/format';

export function SideBadge({ side }: { side: string }) {
  const buy = isBuy(side);
  return <span className={buy ? 'badge badge-buy' : 'badge badge-sell'}>{buy ? 'BUY' : 'SELL'}</span>;
}

export function StatusBadge({ status }: { status: string }) {
  const key = status.toLowerCase();
  const color = key === 'filled'
    ? 'badge-buy'
    : key === 'cancelled'
      ? 'badge-neutral'
      : 'badge-pending';
  const label = status.replace(/([a-z])([A-Z])/g, '$1 $2');
  return <span className={`badge ${color}`}>{label}</span>;
}

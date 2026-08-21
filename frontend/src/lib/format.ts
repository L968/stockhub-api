const currencyFormatter = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
const numberFormatter = new Intl.NumberFormat('en-US');

export const formatCurrency = (value: number): string => currencyFormatter.format(value);
export const formatNumber = (value: number): string => numberFormatter.format(value);

export function formatDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleString('en-US', {
    day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit',
  });
}

export const isBuy = (side: string): boolean => side.toLowerCase() === 'buy';

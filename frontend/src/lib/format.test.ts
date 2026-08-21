import { formatCompactNumber, formatCurrency, formatNumber, formatPercent, formatPrice } from './format';

describe('English number formatting', () => {
  it('formats monetary values as US dollars', () => {
    expect(formatCurrency(1234.5)).toBe('$1,234.50');
  });

  it('formats numbers with the en-US locale', () => {
    expect(formatNumber(1234.5)).toBe('1,234.5');
  });

  it('keeps market data compact and consistent', () => {
    expect(formatCompactNumber(68_420_000)).toBe('68.4M');
    expect(formatPrice(228.5)).toBe('228.50');
    expect(formatPercent(1.24)).toBe('+1.24%');
    expect(formatPercent(-0.48)).toBe('-0.48%');
  });
});

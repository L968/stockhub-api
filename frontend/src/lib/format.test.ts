import { formatCurrency, formatNumber } from './format';

describe('English number formatting', () => {
  it('formats monetary values as US dollars', () => {
    expect(formatCurrency(1234.5)).toBe('$1,234.50');
  });

  it('formats numbers with the en-US locale', () => {
    expect(formatNumber(1234.5)).toBe('1,234.5');
  });
});

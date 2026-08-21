import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { api } from '../../lib/api';
import type { Stock } from '../../types/api';
import { OrderTicket } from './OrderTicket';

const stock: Stock = {
  id: 'ff77bada-83ac-4160-835d-58943fa7204a',
  symbol: 'STHB',
  name: 'StockHub Inc.',
  lastPrice: 12.5,
  changePercent: 1.2,
  minPrice: 11,
  maxPrice: 13,
  volume: 100,
  updatedAtUtc: '2026-08-21T12:00:00Z',
};

function renderTicket() {
  const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } });
  return render(<QueryClientProvider client={client}><OrderTicket stock={stock} /></QueryClientProvider>);
}

describe('OrderTicket', () => {
  it('submits a valid buy order using the selected asset', async () => {
    const createOrder = vi.spyOn(api.orders, 'create').mockResolvedValue({ id: 'order-1' });
    const user = userEvent.setup();
    renderTicket();

    const quantity = screen.getByLabelText('Quantity');
    await user.clear(quantity);
    await user.type(quantity, '4');
    await user.click(screen.getByRole('button', { name: 'Buy STHB' }));

    await waitFor(() => expect(createOrder).toHaveBeenCalled());
    expect(createOrder.mock.calls[0][0]).toEqual({
      stockId: stock.id,
      side: 0,
      price: 12.5,
      quantity: 4,
    });
    expect(await screen.findByText('Order accepted for processing.')).toBeInTheDocument();
  });

  it('prevents submission with an invalid quantity', async () => {
    const createOrder = vi.spyOn(api.orders, 'create').mockResolvedValue({ id: 'order-1' });
    const user = userEvent.setup();
    renderTicket();

    const quantity = screen.getByLabelText('Quantity');
    await user.clear(quantity);
    expect(screen.getByRole('button', { name: 'Buy STHB' })).toBeDisabled();
    expect(createOrder).not.toHaveBeenCalled();
  });
});

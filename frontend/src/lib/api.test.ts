import { api, ApiError } from './api';
import { saveUser } from './storage';

describe('API client', () => {
  it('sends the current user id on authenticated requests', async () => {
    saveUser({ userId: '43fc080c-165b-4b68-965c-8ae64701e317', email: 'trader@stockhub.dev', fullName: 'Trader' });
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify([]), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }));

    await api.stocks.all();

    const headers = new Headers(fetchMock.mock.calls[0][1]?.headers);
    expect(headers.get('x-user-id')).toBe('43fc080c-165b-4b68-965c-8ae64701e317');
  });

  it('surfaces Problem Details messages', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({
      detail: 'Insufficient balance.',
    }), {
      status: 409,
      headers: { 'Content-Type': 'application/json' },
    }));

    await expect(api.orders.create({ stockId: 'stock-1', side: 0, price: 10, quantity: 2 }))
      .rejects.toEqual(new ApiError('Insufficient balance.', 409));
  });

  it('returns a useful message when the API cannot be reached', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('network error'));
    await expect(api.stocks.all()).rejects.toMatchObject({ status: 0 });
  });
});

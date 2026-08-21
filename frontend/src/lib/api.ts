import { getUserId } from './storage';
import type {
  CreateOrderRequest, Order, OrderBook, PagedResponse, Portfolio,
  Stock, StockSearchResult, Trade, User,
} from '../types/api';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5196').replace(/\/$/, '');

export class ApiError extends Error {
  constructor(message: string, public readonly status: number) {
    super(message);
    this.name = 'ApiError';
  }
}

type ProblemDetails = { detail?: string; title?: string; errors?: Record<string, string[]> };

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');
  if (init.body) headers.set('Content-Type', 'application/json');
  const userId = getUserId();
  if (userId) headers.set('x-user-id', userId);

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers });
  } catch {
    throw new ApiError('The API is unavailable. Check that StockHub is running.', 0);
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as ProblemDetails | null;
    const validation = problem?.errors ? Object.values(problem.errors).flat().join(' ') : null;
    throw new ApiError(
      validation || problem?.detail || problem?.title || 'The request could not be completed.',
      response.status,
    );
  }

  return response.status === 204 ? (undefined as T) : response.json() as Promise<T>;
}

const query = (values: Record<string, string | number>): string => {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => params.set(key, String(value)));
  return params.toString();
};

export const api = {
  auth: {
    signUp: (email: string, fullName: string) => request<{ userId: string }>('/v1/users', {
      method: 'POST', body: JSON.stringify({ email, fullName }),
    }),
    signIn: (email: string) => request<{ userId: string }>('/v1/users/login', {
      method: 'POST', body: JSON.stringify({ email }),
    }),
    me: () => request<User>('/v1/users/me'),
  },
  stocks: {
    all: () => request<Stock[]>('/v1/stocks'),
    bySymbol: (symbol: string) => request<Stock>(`/v1/stocks/${encodeURIComponent(symbol)}`),
    search: (value: string) => request<StockSearchResult[]>(`/v1/stocks/find?${query({ query: value })}`),
    orderBook: (symbol: string) => request<OrderBook>(`/v1/stocks/${encodeURIComponent(symbol)}/order-book`),
  },
  orders: {
    mine: (page = 1, pageSize = 20) => request<PagedResponse<Order>>(`/v1/orders/me?${query({ page, pageSize })}`),
    create: (order: CreateOrderRequest) => request<{ id: string }>('/v1/orders', {
      method: 'POST', body: JSON.stringify(order),
    }),
  },
  trades: {
    mine: (page = 1, pageSize = 20) => request<PagedResponse<Trade>>(`/v1/trades/me?${query({ page, pageSize })}`),
  },
  portfolio: { mine: () => request<Portfolio>('/v1/portfolio/me') },
};

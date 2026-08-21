import type { Stock, Order, Trade, PortfolioResponse, OrderBook, CreateOrderRequest, PagedResponse } from '../types';
import { getUserId } from '../lib/auth';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;


const getHeaders = () => {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };

  const userId = getUserId();
  if (userId) {
    headers['x-user-id'] = userId;
  }

  return headers;
};

export const authService = {
  async signUp(email: string, fullName: string) {
    try {
      const response = await fetch(`${API_BASE_URL}/v1/users`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, fullName }),
      });

      if (!response.ok) throw new Error('Sign up failed');
      const data = await response.json();
      return data;
    } catch {
      throw new Error('Failed to create account');
    }
  },

  async signIn(email: string) {
    try {
      const response = await fetch(`${API_BASE_URL}/v1/users/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email }),
      });

      if (response.status === 404) {
        const errorData = await response.json();
        throw new Error(errorData.detail || 'Invalid credentials');
      }

      if (!response.ok) {
        throw new Error('Sign in failed');
      }

      const data = await response.json();
      return data;
    } catch (error) {
      if (error instanceof Error) {
        throw error;
      }
      throw new Error('Failed to sign in');
    }
  },

  async getMe() {
    try {
      const response = await fetch(`${API_BASE_URL}/v1/users/me`, {
        headers: getHeaders(),
      });

      if (!response.ok) {
        throw new Error('Failed to fetch user data');
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        throw error;
      }
      throw new Error('Failed to fetch user data');
    }
  },
};

export const stockService = {
  async getAll(): Promise<Stock[]> {
    const response = await fetch(`${API_BASE_URL}/v1/stocks`, {
      headers: getHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch stocks');
    }

    return response.json();
  },

  async getBySymbol(symbol: string): Promise<Stock | null> {
    const response = await fetch(`${API_BASE_URL}/v1/stocks/${symbol}`, {
      headers: getHeaders(),
    });

    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error('Failed to fetch stock');
    }

    return response.json();
  },

  async search(query: string): Promise<Stock[]> {
    if (query.length < 2) {
      return [];
    }

    const params = new URLSearchParams({
      query: query,
    });

    const response = await fetch(`${API_BASE_URL}/v1/stocks/find?${params}`, {
      headers: getHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to search stocks');
    }

    return response.json();
  },

  async getOrderBook(symbol: string): Promise<OrderBook> {
    const response = await fetch(`${API_BASE_URL}/v1/stocks/${symbol}/order-book`, {
      headers: getHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch order book');
    }

    return response.json();
  },
};

export const orderService = {
  async getMyOrders(
    page: number = 1,
    pageSize: number = 20,
    filters?: {
      startDate?: string;
      endDate?: string;
      status?: number;
    }
  ): Promise<PagedResponse<Order>> {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (filters?.startDate) params.append('startDate', filters.startDate);
    if (filters?.endDate) params.append('endDate', filters.endDate);
    if (filters?.status !== undefined) params.append('status', filters.status.toString());

    const response = await fetch(`${API_BASE_URL}/v1/orders/me?${params}`, {
      headers: getHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch orders');
    }

    return response.json();
  },

  async create(order: CreateOrderRequest): Promise<Order> {
    const response = await fetch(`${API_BASE_URL}/v1/orders`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(order),
    });

    if (!response.ok) {
      throw new Error('Failed to create order');
    }

    return response.json();
  },
};

export const tradeService = {
  async getMyTrades(
    page: number = 1,
    pageSize: number = 20,
    filters?: {
      startDate?: string;
      endDate?: string;
      symbol?: string;
    }
  ): Promise<PagedResponse<Trade>> {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (filters?.startDate) params.append('startDate', filters.startDate);
    if (filters?.endDate) params.append('endDate', filters.endDate);
    if (filters?.symbol) params.append('symbol', filters.symbol);

    const response = await fetch(`${API_BASE_URL}/v1/trades/me?${params}`, {
      headers: getHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch trades');
    }

    return response.json();
  },
};

export const portfolioService = {
  async getMyPortfolio(): Promise<PortfolioResponse> {
    const response = await fetch(`${API_BASE_URL}/v1/portfolio/me`, {
      headers: getHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch portfolio');
    }

    return response.json();
  },
};



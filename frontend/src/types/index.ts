export enum OrderSide {
  Buy = 0,
  Sell = 1,
}

export enum OrderStatus {
  Pending = 0,
  PartiallyFilled = 1,
  Filled = 2,
  Cancelled = 3,
}

export interface Stock {
  id: string;
  symbol: string;
  name: string;
  sector?: string;
  created_at?: string;
}

export interface Order {
  id: string;
  side: string;
  price: number;
  quantity: number;
  filledQuantity: number;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  stock: {
    id: string;
    symbol: string;
    name: string;
  };
}

export interface Trade {
  id: string;
  symbol: string;
  side: string;
  price: number;
  quantity: number;
  orderId?: string;
  executedAt: string;
}

export interface PortfolioPosition {
  symbol: string;
  quantity: number;
  avgPrice: number;
  currentPrice: number;
  marketValue: number;
}

export interface PortfolioResponse {
  totalValue: number;
  positions: PortfolioPosition[];
}

export interface Portfolio {
  id: string;
  user_id: string;
  stock_id: string;
  quantity: number;
  average_price: number;
  updated_at: string;
  stock?: Stock;
}

export interface OrderBookEntry {
  price: number;
  quantity: number;
}

export interface OrderBook {
  symbol: string;
  bids: OrderBookEntry[];
  asks: OrderBookEntry[];
}

export interface CreateOrderRequest {
  stockId: string;
  side: OrderSide;
  price: number;
  quantity: number;
}

export interface PagedResponse<T> {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  items: T[];
}

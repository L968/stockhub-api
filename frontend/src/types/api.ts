export type User = {
  userId: string;
  email: string;
  fullName: string;
  createdAt?: string;
  currentBalance?: number;
};

export type Stock = {
  id: string;
  symbol: string;
  name: string;
  lastPrice: number;
  changePercent: number;
  minPrice: number;
  maxPrice: number;
  volume: number;
  updatedAtUtc: string;
};

export type StockSearchResult = Pick<Stock, 'id' | 'symbol' | 'name'>;
export const OrderSide = { Buy: 0, Sell: 1 } as const;
export type OrderSide = (typeof OrderSide)[keyof typeof OrderSide];

export type Order = {
  id: string;
  side: string;
  price: number;
  quantity: number;
  filledQuantity: number;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  stock: StockSearchResult;
};

export type Trade = {
  id: string;
  symbol: string;
  side: string;
  price: number;
  quantity: number;
  orderId: string;
  executedAt: string;
};

export type PortfolioPosition = {
  symbol: string;
  quantity: number;
  avgPrice: number;
  currentPrice: number;
  marketValue: number;
};

export type Portfolio = { totalValue: number; positions: PortfolioPosition[] };
export type OrderBookLevel = { price: number; quantity: number; orderCount: number };
export type OrderBook = { bids: OrderBookLevel[]; asks: OrderBookLevel[]; updatedAtUtc: string };
export type CreateOrderRequest = { stockId: string; side: OrderSide; price: number; quantity: number };
export type PagedResponse<T> = {
  page: number;
  pageSize: number;
  totalItems: number | null;
  totalPages: number | null;
  items: T[];
};

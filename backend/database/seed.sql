BEGIN;

-- Demo users. Market makers provide cash and inventory for executable books.
INSERT INTO users."user" (id, email, full_name, current_balance, created_at, updated_at)
VALUES
    ('0198e000-0000-7000-8000-000000000001', 'demo@stockhub.dev', 'Demo Trader', 50000.00, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('0198e000-0000-7000-8000-000000000002', 'market.maker.one@stockhub.dev', 'Northstar Liquidity', 10000000.00, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('0198e000-0000-7000-8000-000000000003', 'market.maker.two@stockhub.dev', 'Atlas Market Making', 10000000.00, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (email) DO NOTHING;

-- Tradable US-listed companies.
INSERT INTO stocks.stock (id, symbol, name, sector, created_at, updated_at)
VALUES
    ('0198e100-0000-7000-8000-000000000001', 'AAPL', 'Apple Inc.', 'Technology', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('0198e100-0000-7000-8000-000000000002', 'MSFT', 'Microsoft Corporation', 'Technology', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('0198e100-0000-7000-8000-000000000003', 'NVDA', 'NVIDIA Corporation', 'Semiconductors', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('0198e100-0000-7000-8000-000000000004', 'AMZN', 'Amazon.com, Inc.', 'Consumer Discretionary', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('0198e100-0000-7000-8000-000000000005', 'TSLA', 'Tesla, Inc.', 'Automotive', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('0198e100-0000-7000-8000-000000000006', 'JPM', 'JPMorgan Chase & Co.', 'Financial Services', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('0198e100-0000-7000-8000-000000000007', 'KO', 'The Coca-Cola Company', 'Consumer Staples', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('0198e100-0000-7000-8000-000000000008', 'DIS', 'The Walt Disney Company', 'Communication Services', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (symbol) DO NOTHING;

WITH snapshot_data(symbol, last_price, change_percent, min_price, max_price, volume) AS (
    VALUES
        ('AAPL', 228.50,  1.24, 224.80, 230.10,  68420000::bigint),
        ('MSFT', 418.75,  0.62, 414.20, 421.30,  21870000::bigint),
        ('NVDA', 137.20,  2.85, 132.60, 138.45, 192340000::bigint),
        ('AMZN', 225.10, -0.48, 222.35, 228.70,  37910000::bigint),
        ('TSLA', 352.40, -1.76, 345.15, 361.80,  94760000::bigint),
        ('JPM',  268.60,  0.91, 265.40, 270.25,   9840000::bigint),
        ('KO',    70.15,  0.34,  69.55,  70.48,  13260000::bigint),
        ('DIS',  112.80, -0.22, 111.40, 114.05,  11730000::bigint)
)
INSERT INTO stocks.stock_snapshot
    (stock_id, last_price, change_percent, min_price, max_price, volume, created_at, updated_at)
SELECT stock.id, data.last_price, data.change_percent, data.min_price, data.max_price, data.volume,
       CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM snapshot_data data
JOIN stocks.stock stock ON stock.symbol = data.symbol
ON CONFLICT (stock_id) DO NOTHING;

-- Upgrade the earlier lightweight demo account only before this market seed exists.
UPDATE users."user"
SET current_balance = 50000.00,
    full_name = 'Demo Trader',
    updated_at = CURRENT_TIMESTAMP
WHERE email = 'demo@stockhub.dev'
  AND NOT EXISTS (
      SELECT 1 FROM orders."order"
      WHERE id = md5('stockhub-seed-order-AAPL-buy-1')::uuid
  );

-- Inventories make every seeded ask executable. The demo account starts diversified.
WITH market_maker(email) AS (
    VALUES ('market.maker.one@stockhub.dev'), ('market.maker.two@stockhub.dev')
),
market_inventory AS (
    SELECT maker.email, stock.symbol, 10000 AS quantity, snapshot.last_price AS avg_price
    FROM market_maker maker
    CROSS JOIN stocks.stock stock
    JOIN stocks.stock_snapshot snapshot ON snapshot.stock_id = stock.id
    WHERE stock.symbol IN ('AAPL', 'MSFT', 'NVDA', 'AMZN', 'TSLA', 'JPM', 'KO', 'DIS')
),
demo_inventory(email, symbol, quantity, avg_price) AS (
    VALUES
        ('demo@stockhub.dev', 'AAPL', 25, 205.40),
        ('demo@stockhub.dev', 'MSFT', 12, 390.15),
        ('demo@stockhub.dev', 'NVDA', 40, 120.80),
        ('demo@stockhub.dev', 'KO',   80,  65.25)
),
inventory AS (
    SELECT * FROM market_inventory
    UNION ALL
    SELECT * FROM demo_inventory
)
INSERT INTO orders.portfolio
    (id, user_id, stock_id, quantity, avg_price, created_at, updated_at)
SELECT md5('stockhub-seed-portfolio-' || inventory.email || '-' || inventory.symbol)::uuid,
       account.id, stock.id, inventory.quantity, inventory.avg_price,
       CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM inventory
JOIN users."user" account ON account.email = inventory.email
JOIN stocks.stock stock ON stock.symbol = inventory.symbol
ON CONFLICT (user_id, stock_id) DO NOTHING;

-- Three non-crossing price levels on each side for every stock: 48 open orders.
WITH stock_data(symbol, last_price) AS (
    VALUES
        ('AAPL', 228.50), ('MSFT', 418.75), ('NVDA', 137.20), ('AMZN', 225.10),
        ('TSLA', 352.40), ('JPM', 268.60), ('KO', 70.15), ('DIS', 112.80)
),
levels(level, quantity) AS (
    VALUES (1, 35), (2, 50), (3, 65)
),
sides(side, side_name, direction) AS (
    VALUES (0, 'buy', -1), (1, 'sell', 1)
),
book AS (
    SELECT data.symbol, sides.side, sides.side_name, levels.level, levels.quantity,
           ROUND(data.last_price * (1 + sides.direction * 0.0025 * levels.level), 2) AS price,
           CASE
               WHEN MOD(levels.level + sides.side, 2) = 0 THEN 'market.maker.two@stockhub.dev'
               ELSE 'market.maker.one@stockhub.dev'
           END AS email
    FROM stock_data data
    CROSS JOIN levels
    CROSS JOIN sides
)
INSERT INTO orders."order"
    (id, user_id, stock_id, side, price, quantity, filled_quantity, is_cancelled, created_at, updated_at)
SELECT md5('stockhub-seed-order-' || book.symbol || '-' || book.side_name || '-' || book.level)::uuid,
       account.id, stock.id, book.side, book.price, book.quantity, 0, FALSE,
       CURRENT_TIMESTAMP - make_interval(mins => 10 - book.level),
       CURRENT_TIMESTAMP - make_interval(mins => 10 - book.level)
FROM book
JOIN users."user" account ON account.email = book.email
JOIN stocks.stock stock ON stock.symbol = book.symbol
ON CONFLICT (id) DO NOTHING;

-- Completed and cancelled demo orders make the activity screens useful immediately.
WITH historical_order(seed_key, email, symbol, side, price, quantity, filled_quantity, is_cancelled, age) AS (
    VALUES
        ('demo-aapl-buy', 'demo@stockhub.dev', 'AAPL', 0, 221.40, 5, 5, FALSE, INTERVAL '4 days'),
        ('maker-aapl-sell', 'market.maker.two@stockhub.dev', 'AAPL', 1, 221.40, 5, 5, FALSE, INTERVAL '4 days'),
        ('demo-nvda-sell', 'demo@stockhub.dev', 'NVDA', 1, 134.10, 4, 4, FALSE, INTERVAL '2 days'),
        ('maker-nvda-buy', 'market.maker.one@stockhub.dev', 'NVDA', 0, 134.10, 4, 4, FALSE, INTERVAL '2 days'),
        ('demo-tsla-cancelled', 'demo@stockhub.dev', 'TSLA', 0, 330.00, 3, 0, TRUE, INTERVAL '1 day')
)
INSERT INTO orders."order"
    (id, user_id, stock_id, side, price, quantity, filled_quantity, is_cancelled, created_at, updated_at)
SELECT md5('stockhub-seed-history-order-' || history.seed_key)::uuid,
       account.id, stock.id, history.side, history.price, history.quantity,
       history.filled_quantity, history.is_cancelled,
       CURRENT_TIMESTAMP - history.age, CURRENT_TIMESTAMP - history.age
FROM historical_order history
JOIN users."user" account ON account.email = history.email
JOIN stocks.stock stock ON stock.symbol = history.symbol
ON CONFLICT (id) DO NOTHING;

WITH trade_data(seed_key, symbol, buyer_email, seller_email, buy_order_key, sell_order_key, price, quantity, age) AS (
    VALUES
        ('aapl-trade', 'AAPL', 'demo@stockhub.dev', 'market.maker.two@stockhub.dev',
         'demo-aapl-buy', 'maker-aapl-sell', 221.40, 5, INTERVAL '4 days'),
        ('nvda-trade', 'NVDA', 'market.maker.one@stockhub.dev', 'demo@stockhub.dev',
         'maker-nvda-buy', 'demo-nvda-sell', 134.10, 4, INTERVAL '2 days')
)
INSERT INTO orders.trade
    (id, stock_id, buyer_id, seller_id, buy_order_id, sell_order_id, price, quantity, executed_at)
SELECT md5('stockhub-seed-trade-' || trade.seed_key)::uuid,
       stock.id, buyer.id, seller.id,
       md5('stockhub-seed-history-order-' || trade.buy_order_key)::uuid,
       md5('stockhub-seed-history-order-' || trade.sell_order_key)::uuid,
       trade.price, trade.quantity, CURRENT_TIMESTAMP - trade.age
FROM trade_data trade
JOIN stocks.stock stock ON stock.symbol = trade.symbol
JOIN users."user" buyer ON buyer.email = trade.buyer_email
JOIN users."user" seller ON seller.email = trade.seller_email
ON CONFLICT (id) DO NOTHING;

COMMIT;

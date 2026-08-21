using Dapper;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Stockhub.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        DataSource = NpgsqlDataSource.Create(_container.GetConnectionString());

        await using NpgsqlConnection connection = await DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(SchemaSql);
    }

    public async Task ResetAsync()
    {
        await using NpgsqlConnection connection = await DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "TRUNCATE orders.trade, orders.portfolio, orders.integration_outbox, orders.\"order\", users.\"user\";");
    }

    public async Task DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await _container.DisposeAsync();
    }

    private const string SchemaSql = """
        CREATE SCHEMA users;
        CREATE SCHEMA orders;

        CREATE TABLE users."user" (
            id uuid PRIMARY KEY,
            current_balance numeric(18,2) NOT NULL,
            updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE orders."order" (
            id uuid PRIMARY KEY,
            user_id uuid NOT NULL,
            stock_id uuid NOT NULL,
            side integer NOT NULL,
            price numeric(18,2) NOT NULL,
            quantity integer NOT NULL,
            filled_quantity integer NOT NULL DEFAULT 0,
            is_cancelled boolean NOT NULL DEFAULT FALSE,
            created_at timestamp with time zone NOT NULL,
            updated_at timestamp with time zone NOT NULL
        );

        CREATE TABLE orders.portfolio (
            id uuid PRIMARY KEY,
            user_id uuid NOT NULL,
            stock_id uuid NOT NULL,
            quantity integer NOT NULL,
            avg_price numeric(18,2) NOT NULL,
            created_at timestamp with time zone NOT NULL,
            updated_at timestamp with time zone NOT NULL,
            UNIQUE (user_id, stock_id)
        );

        CREATE TABLE orders.trade (
            id uuid PRIMARY KEY,
            stock_id uuid NOT NULL,
            buyer_id uuid NOT NULL,
            seller_id uuid NOT NULL,
            buy_order_id uuid NOT NULL,
            sell_order_id uuid NOT NULL,
            price numeric(18,2) NOT NULL,
            quantity integer NOT NULL,
            executed_at timestamp with time zone NOT NULL
        );

        CREATE TABLE orders.integration_outbox (
            id uuid PRIMARY KEY,
            order_id uuid NOT NULL UNIQUE,
            stock_id uuid NOT NULL,
            type varchar(200) NOT NULL,
            payload jsonb NOT NULL,
            occurred_at timestamp with time zone NOT NULL,
            published_at timestamp with time zone NULL,
            attempts integer NOT NULL DEFAULT 0,
            lock_id uuid NULL,
            locked_until timestamp with time zone NULL,
            last_error varchar(2000) NULL
        );
        """;
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}

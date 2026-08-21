using System;
#pragma warning disable CA1861, IDE0161 // Generated migration code.
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stockhub.Modules.Orders.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceReplicasWithViewsAndAddIntegrationOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_mv_stock_stock_id",
                schema: "orders",
                table: "order");

            migrationBuilder.DropForeignKey(
                name: "FK_order_mv_user_user_id",
                schema: "orders",
                table: "order");

            migrationBuilder.DropForeignKey(
                name: "FK_portfolio_mv_stock_stock_id",
                schema: "orders",
                table: "portfolio");

            migrationBuilder.DropForeignKey(
                name: "FK_portfolio_mv_user_user_id",
                schema: "orders",
                table: "portfolio");

            migrationBuilder.DropForeignKey(
                name: "FK_trade_mv_stock_stock_id",
                schema: "orders",
                table: "trade");

            migrationBuilder.DropTable(
                name: "mv_stock",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "mv_user",
                schema: "orders");

            migrationBuilder.Sql(
                """
                CREATE VIEW orders.user_view AS
                SELECT id, email, full_name, current_balance, created_at, updated_at
                FROM users."user";

                CREATE VIEW orders.stock_view AS
                SELECT id, symbol, name, sector, created_at, updated_at
                FROM stocks.stock;
                """);

            migrationBuilder.DropIndex(
                name: "IX_portfolio_user_id",
                schema: "orders",
                table: "portfolio");

            migrationBuilder.CreateTable(
                name: "integration_outbox",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    lock_id = table.Column<Guid>(type: "uuid", nullable: true),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_outbox", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_user_id_stock_id",
                schema: "orders",
                table: "portfolio",
                columns: new[] { "user_id", "stock_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_outbox_order_id",
                schema: "orders",
                table: "integration_outbox",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_outbox_published_at_occurred_at",
                schema: "orders",
                table: "integration_outbox",
                columns: new[] { "published_at", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP VIEW IF EXISTS orders.user_view;
                DROP VIEW IF EXISTS orders.stock_view;
                """);

            migrationBuilder.DropTable(
                name: "integration_outbox",
                schema: "orders");

            migrationBuilder.DropIndex(
                name: "IX_portfolio_user_id_stock_id",
                schema: "orders",
                table: "portfolio");

            migrationBuilder.CreateTable(
                name: "mv_stock",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    sector = table.Column<string>(type: "text", nullable: false),
                    symbol = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mv_stock", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mv_user",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mv_user", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_user_id",
                schema: "orders",
                table: "portfolio",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_mv_stock_stock_id",
                schema: "orders",
                table: "order",
                column: "stock_id",
                principalSchema: "orders",
                principalTable: "mv_stock",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_order_mv_user_user_id",
                schema: "orders",
                table: "order",
                column: "user_id",
                principalSchema: "orders",
                principalTable: "mv_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_portfolio_mv_stock_stock_id",
                schema: "orders",
                table: "portfolio",
                column: "stock_id",
                principalSchema: "orders",
                principalTable: "mv_stock",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_portfolio_mv_user_user_id",
                schema: "orders",
                table: "portfolio",
                column: "user_id",
                principalSchema: "orders",
                principalTable: "mv_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_trade_mv_stock_stock_id",
                schema: "orders",
                table: "trade",
                column: "stock_id",
                principalSchema: "orders",
                principalTable: "mv_stock",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

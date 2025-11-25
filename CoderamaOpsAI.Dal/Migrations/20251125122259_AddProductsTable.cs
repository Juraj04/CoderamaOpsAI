using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoderamaOpsAI.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddProductsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: DateTime.UtcNow);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: DateTime.UtcNow);

            // Update existing users with proper timestamps
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"CreatedAt\" = NOW(), \"UpdatedAt\" = NOW() WHERE \"Email\" IN ('admin@example.com', 'test@example.com')");

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProductId",
                table: "Orders",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            // Seed Products
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Name", "Description", "Price", "Stock", "CreatedAt" },
                values: new object[] { "Laptop Pro", "High-performance laptop", 1299.99m, 50, DateTime.UtcNow });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Name", "Description", "Price", "Stock", "CreatedAt" },
                values: new object[] { "Wireless Mouse", "Ergonomic wireless mouse", 29.99m, 200, DateTime.UtcNow });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Name", "Description", "Price", "Stock", "CreatedAt" },
                values: new object[] { "USB-C Cable", "3-meter USB-C cable", 9.99m, 500, DateTime.UtcNow });

            // Seed Orders
            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "UserId", "ProductId", "Quantity", "Price", "Total", "Status", "CreatedAt", "UpdatedAt" },
                values: new object[] { 1, 1, 2, 1299.99m, 2599.98m, "Completed", DateTime.UtcNow, DateTime.UtcNow });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "UserId", "ProductId", "Quantity", "Price", "Total", "Status", "CreatedAt", "UpdatedAt" },
                values: new object[] { 2, 2, 1, 29.99m, 29.99m, "Pending", DateTime.UtcNow, DateTime.UtcNow });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");
        }
    }
}

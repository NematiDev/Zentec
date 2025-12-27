using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zentec.PaymentService.Data
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripeChargeId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CardLast4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    CardBrand = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    IsTestPayment = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_OrderId",
                table: "payment_transactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_StripePaymentIntentId",
                table: "payment_transactions",
                column: "StripePaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_UserId",
                table: "payment_transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_UserId_CreatedAt",
                table: "payment_transactions",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_transactions");
        }
    }
}

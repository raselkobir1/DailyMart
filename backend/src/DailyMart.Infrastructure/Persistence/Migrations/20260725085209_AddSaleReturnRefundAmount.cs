using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyMart.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleReturnRefundAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "refund_amount",
                table: "sale_returns",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "refund_amount",
                table: "sale_returns");
        }
    }
}

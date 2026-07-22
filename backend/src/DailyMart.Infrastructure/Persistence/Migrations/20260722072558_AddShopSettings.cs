using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DailyMart.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShopSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "settings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shop_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    shop_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    shop_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    shop_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    shop_logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    invoice_prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "INV-"),
                    invoice_footer_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "BDT"),
                    currency_symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "৳"),
                    default_vat_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    default_discount_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    backup_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    backup_frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Daily"),
                    date_format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "dd/MM/yyyy"),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "UTC"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "settings");
        }
    }
}

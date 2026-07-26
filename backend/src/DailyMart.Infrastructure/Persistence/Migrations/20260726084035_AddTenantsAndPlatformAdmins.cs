using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DailyMart.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantsAndPlatformAdmins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_units_name",
                table: "units");

            migrationBuilder.DropIndex(
                name: "ix_suppliers_name",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "ix_roles_name",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ix_products_barcode",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_code",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_customers_phone",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_categories_name",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "ix_brands_name",
                table: "brands");

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "units",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "suppliers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "supplier_ledger_entries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "settings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "sales",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "sale_returns",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "sale_return_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "sale_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "roles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "role_menu_permissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "refresh_tokens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "purchases",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "purchase_returns",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "purchase_return_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "purchase_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "products",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "inventory_transactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "inventory_adjustments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "expenses",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "customers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "customer_ledger_entries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "categories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "brands",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "tenant_id",
                table: "audit_logs",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "platform_admins",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_admins", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id",
                table: "users",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_units_tenant_id_name",
                table: "units",
                columns: new[] { "tenant_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_tenant_id_name",
                table: "suppliers",
                columns: new[] { "tenant_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_ledger_entries_tenant_id",
                table: "supplier_ledger_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_settings_tenant_id",
                table: "settings",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_tenant_id",
                table: "sales",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_tenant_id",
                table: "sale_returns",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_items_tenant_id",
                table: "sale_return_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_items_tenant_id",
                table: "sale_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_tenant_id_name",
                table: "roles",
                columns: new[] { "tenant_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_role_menu_permissions_tenant_id",
                table: "role_menu_permissions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_tenant_id",
                table: "refresh_tokens",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchases_tenant_id",
                table: "purchases",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_tenant_id",
                table: "purchase_returns",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_return_items_tenant_id",
                table: "purchase_return_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_items_tenant_id",
                table: "purchase_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_id_barcode",
                table: "products",
                columns: new[] { "tenant_id", "barcode" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_id_code",
                table: "products",
                columns: new[] { "tenant_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_tenant_id",
                table: "inventory_transactions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_adjustments_tenant_id",
                table: "inventory_adjustments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_tenant_id",
                table: "expenses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_tenant_id_phone",
                table: "customers",
                columns: new[] { "tenant_id", "phone" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_customer_ledger_entries_tenant_id",
                table: "customer_ledger_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_tenant_id_name",
                table: "categories",
                columns: new[] { "tenant_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_brands_tenant_id_name",
                table: "brands",
                columns: new[] { "tenant_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id",
                table: "audit_logs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_platform_admins_username",
                table: "platform_admins",
                column: "username",
                unique: true,
                filter: "is_deleted = false");

            // Every AddColumn above defaulted the new NOT NULL tenant_id to 0, but no Tenant with Id=0
            // exists (identity columns start at 1) - the AddForeignKey calls below would fail against
            // that value. Backfill every pre-existing row to one real "Default Company" tenant first,
            // so this migration preserves all existing data as that tenant's own, rather than losing it.
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    default_tenant_id bigint;
                BEGIN
                    INSERT INTO tenants (name, is_active, created_at, created_by, is_deleted)
                    VALUES ('Default Company', true, now(), 'system', false)
                    RETURNING id INTO default_tenant_id;

                    UPDATE users SET tenant_id = default_tenant_id;
                    UPDATE units SET tenant_id = default_tenant_id;
                    UPDATE suppliers SET tenant_id = default_tenant_id;
                    UPDATE supplier_ledger_entries SET tenant_id = default_tenant_id;
                    UPDATE settings SET tenant_id = default_tenant_id;
                    UPDATE sales SET tenant_id = default_tenant_id;
                    UPDATE sale_returns SET tenant_id = default_tenant_id;
                    UPDATE sale_return_items SET tenant_id = default_tenant_id;
                    UPDATE sale_items SET tenant_id = default_tenant_id;
                    UPDATE roles SET tenant_id = default_tenant_id;
                    UPDATE role_menu_permissions SET tenant_id = default_tenant_id;
                    UPDATE refresh_tokens SET tenant_id = default_tenant_id;
                    UPDATE purchases SET tenant_id = default_tenant_id;
                    UPDATE purchase_returns SET tenant_id = default_tenant_id;
                    UPDATE purchase_return_items SET tenant_id = default_tenant_id;
                    UPDATE purchase_items SET tenant_id = default_tenant_id;
                    UPDATE products SET tenant_id = default_tenant_id;
                    UPDATE inventory_transactions SET tenant_id = default_tenant_id;
                    UPDATE inventory_adjustments SET tenant_id = default_tenant_id;
                    UPDATE expenses SET tenant_id = default_tenant_id;
                    UPDATE customers SET tenant_id = default_tenant_id;
                    UPDATE customer_ledger_entries SET tenant_id = default_tenant_id;
                    UPDATE categories SET tenant_id = default_tenant_id;
                    UPDATE brands SET tenant_id = default_tenant_id;
                    UPDATE audit_logs SET tenant_id = default_tenant_id;
                END $$;
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_brands_tenants_tenant_id",
                table: "brands",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_categories_tenants_tenant_id",
                table: "categories",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_ledger_entries_tenants_tenant_id",
                table: "customer_ledger_entries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customers_tenants_tenant_id",
                table: "customers",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expenses_tenants_tenant_id",
                table: "expenses",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_adjustments_tenants_tenant_id",
                table: "inventory_adjustments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_transactions_tenants_tenant_id",
                table: "inventory_transactions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_tenants_tenant_id",
                table: "products",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_items_tenants_tenant_id",
                table: "purchase_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_return_items_tenants_tenant_id",
                table: "purchase_return_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_returns_tenants_tenant_id",
                table: "purchase_returns",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchases_tenants_tenant_id",
                table: "purchases",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_refresh_tokens_tenants_tenant_id",
                table: "refresh_tokens",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_role_menu_permissions_tenants_tenant_id",
                table: "role_menu_permissions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_roles_tenants_tenant_id",
                table: "roles",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_items_tenants_tenant_id",
                table: "sale_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_return_items_tenants_tenant_id",
                table: "sale_return_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_returns_tenants_tenant_id",
                table: "sale_returns",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_tenants_tenant_id",
                table: "sales",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_settings_tenants_tenant_id",
                table: "settings",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_supplier_ledger_entries_tenants_tenant_id",
                table: "supplier_ledger_entries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_suppliers_tenants_tenant_id",
                table: "suppliers",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_units_tenants_tenant_id",
                table: "units",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_users_tenants_tenant_id",
                table: "users",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_brands_tenants_tenant_id",
                table: "brands");

            migrationBuilder.DropForeignKey(
                name: "fk_categories_tenants_tenant_id",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_ledger_entries_tenants_tenant_id",
                table: "customer_ledger_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_customers_tenants_tenant_id",
                table: "customers");

            migrationBuilder.DropForeignKey(
                name: "fk_expenses_tenants_tenant_id",
                table: "expenses");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_adjustments_tenants_tenant_id",
                table: "inventory_adjustments");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_transactions_tenants_tenant_id",
                table: "inventory_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_products_tenants_tenant_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_items_tenants_tenant_id",
                table: "purchase_items");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_return_items_tenants_tenant_id",
                table: "purchase_return_items");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_returns_tenants_tenant_id",
                table: "purchase_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_purchases_tenants_tenant_id",
                table: "purchases");

            migrationBuilder.DropForeignKey(
                name: "fk_refresh_tokens_tenants_tenant_id",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_role_menu_permissions_tenants_tenant_id",
                table: "role_menu_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_roles_tenants_tenant_id",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_items_tenants_tenant_id",
                table: "sale_items");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_return_items_tenants_tenant_id",
                table: "sale_return_items");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_returns_tenants_tenant_id",
                table: "sale_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_tenants_tenant_id",
                table: "sales");

            migrationBuilder.DropForeignKey(
                name: "fk_settings_tenants_tenant_id",
                table: "settings");

            migrationBuilder.DropForeignKey(
                name: "fk_supplier_ledger_entries_tenants_tenant_id",
                table: "supplier_ledger_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_suppliers_tenants_tenant_id",
                table: "suppliers");

            migrationBuilder.DropForeignKey(
                name: "fk_units_tenants_tenant_id",
                table: "units");

            migrationBuilder.DropForeignKey(
                name: "fk_users_tenants_tenant_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "platform_admins");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropIndex(
                name: "ix_users_tenant_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_units_tenant_id_name",
                table: "units");

            migrationBuilder.DropIndex(
                name: "ix_suppliers_tenant_id_name",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "ix_supplier_ledger_entries_tenant_id",
                table: "supplier_ledger_entries");

            migrationBuilder.DropIndex(
                name: "ix_settings_tenant_id",
                table: "settings");

            migrationBuilder.DropIndex(
                name: "ix_sales_tenant_id",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sale_returns_tenant_id",
                table: "sale_returns");

            migrationBuilder.DropIndex(
                name: "ix_sale_return_items_tenant_id",
                table: "sale_return_items");

            migrationBuilder.DropIndex(
                name: "ix_sale_items_tenant_id",
                table: "sale_items");

            migrationBuilder.DropIndex(
                name: "ix_roles_tenant_id_name",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ix_role_menu_permissions_tenant_id",
                table: "role_menu_permissions");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_tenant_id",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_purchases_tenant_id",
                table: "purchases");

            migrationBuilder.DropIndex(
                name: "ix_purchase_returns_tenant_id",
                table: "purchase_returns");

            migrationBuilder.DropIndex(
                name: "ix_purchase_return_items_tenant_id",
                table: "purchase_return_items");

            migrationBuilder.DropIndex(
                name: "ix_purchase_items_tenant_id",
                table: "purchase_items");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_id_barcode",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_id_code",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_inventory_transactions_tenant_id",
                table: "inventory_transactions");

            migrationBuilder.DropIndex(
                name: "ix_inventory_adjustments_tenant_id",
                table: "inventory_adjustments");

            migrationBuilder.DropIndex(
                name: "ix_expenses_tenant_id",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "ix_customers_tenant_id_phone",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customer_ledger_entries_tenant_id",
                table: "customer_ledger_entries");

            migrationBuilder.DropIndex(
                name: "ix_categories_tenant_id_name",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "ix_brands_tenant_id_name",
                table: "brands");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_tenant_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "units");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "supplier_ledger_entries");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sale_returns");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sale_return_items");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sale_items");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "role_menu_permissions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "purchase_returns");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "purchase_return_items");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "purchase_items");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "inventory_transactions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "inventory_adjustments");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "customer_ledger_entries");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "audit_logs");

            migrationBuilder.CreateIndex(
                name: "ix_units_name",
                table: "units",
                column: "name",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_name",
                table: "suppliers",
                column: "name",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_products_barcode",
                table: "products",
                column: "barcode",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_products_code",
                table: "products",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_customers_phone",
                table: "customers",
                column: "phone",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_categories_name",
                table: "categories",
                column: "name",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_brands_name",
                table: "brands",
                column: "name",
                unique: true,
                filter: "is_deleted = false");
        }
    }
}

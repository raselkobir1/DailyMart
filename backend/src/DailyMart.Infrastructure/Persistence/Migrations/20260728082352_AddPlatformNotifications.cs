using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DailyMart.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_notifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    admin_username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_platform_notifications_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_platform_notifications_created_at",
                table: "platform_notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_platform_notifications_is_read",
                table: "platform_notifications",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "ix_platform_notifications_tenant_id",
                table: "platform_notifications",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_notifications");
        }
    }
}

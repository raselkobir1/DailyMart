using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyMart.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnSettingsTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nothing previously prevented more than one Settings row per tenant (see
            // ShopSettingsConfiguration's doc comment) - a few tenants have ended up with duplicates in
            // practice, which made GetSingletonAsync's unordered lookup non-deterministic. Before the
            // unique index below can be created, collapse each tenant back down to its oldest (lowest id)
            // row by soft-deleting the rest, the same "keep the original, drop the accidental copies"
            // choice a person fixing this by hand would make.
            migrationBuilder.Sql(
                """
                UPDATE settings
                SET is_deleted = true
                WHERE is_deleted = false
                  AND id <> (
                      SELECT MIN(s2.id) FROM settings s2
                      WHERE s2.tenant_id = settings.tenant_id AND s2.is_deleted = false
                  );
                """);

            migrationBuilder.DropIndex(
                name: "ix_settings_tenant_id",
                table: "settings");

            migrationBuilder.CreateIndex(
                name: "ix_settings_tenant_id",
                table: "settings",
                column: "tenant_id",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_settings_tenant_id",
                table: "settings");

            migrationBuilder.CreateIndex(
                name: "ix_settings_tenant_id",
                table: "settings",
                column: "tenant_id");
        }
    }
}

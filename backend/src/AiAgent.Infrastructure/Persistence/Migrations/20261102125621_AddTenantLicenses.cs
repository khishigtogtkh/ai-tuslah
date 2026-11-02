using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantLicenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByAdminId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenses", x => x.Id);
                    table.CheckConstraint("CK_Licenses_Status", "\"Status\" IN ('active', 'inactive')");
                    table.ForeignKey(
                        name: "FK_Licenses_AspNetUsers_UpdatedByAdminId",
                        column: x => x.UpdatedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Licenses_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_TenantId",
                table: "Licenses",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_UpdatedByAdminId",
                table: "Licenses",
                column: "UpdatedByAdminId");

            // Existing tenants start without access. This migration is not an admin action.
            migrationBuilder.Sql("""
                INSERT INTO "Licenses" ("Id", "TenantId", "Status", "Version", "CreatedAt", "UpdatedAt", "UpdatedByAdminId")
                SELECT gen_random_uuid(), "Id", 'inactive', gen_random_uuid(), CURRENT_TIMESTAMP, NULL, NULL
                FROM "Tenants";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Licenses");
        }
    }
}

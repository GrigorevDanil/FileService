using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_type = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    status = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    context = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    final_key = table.Column<string>(type: "jsonb", nullable: false),
                    hls_root_key = table.Column<string>(type: "jsonb", nullable: true),
                    media_data = table.Column<string>(type: "jsonb", nullable: false),
                    raw_key = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_asset", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_status_created_at",
                table: "media_assets",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_assets");
        }
    }
}

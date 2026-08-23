using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthwatcherApi.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "targets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    checked_at = table.Column<string>(type: "TEXT", nullable: true),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    response_time_ms = table.Column<double>(type: "REAL", nullable: true),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    status_code = table.Column<int>(type: "INTEGER", nullable: true),
                    error = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    created_at = table.Column<string>(type: "TEXT", nullable: false),
                    updated_at = table.Column<string>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_targets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "target_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    target_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    checked_at = table.Column<string>(type: "TEXT", nullable: true),
                    response_time_ms = table.Column<double>(type: "REAL", nullable: true),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    status_code = table.Column<int>(type: "INTEGER", nullable: true),
                    error = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_target_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_target_history_targets_target_id",
                        column: x => x.target_id,
                        principalTable: "targets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_target_history_target_id_checked_at",
                table: "target_history",
                columns: new[] { "target_id", "checked_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "target_history");

            migrationBuilder.DropTable(
                name: "targets");
        }
    }
}

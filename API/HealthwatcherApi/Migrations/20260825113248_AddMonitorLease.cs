using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthwatcherApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitorLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "monitor_leases",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false),
                    owner = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    expires_at = table.Column<string>(type: "TEXT", nullable: false),
                    token = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_monitor_leases", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "monitor_leases",
                columns: new[] { "id", "expires_at", "owner", "token" },
                values: new object[] { 1, "0001-01-01T00:00:00.0000000Z", "", new Guid("00000000-0000-0000-0000-000000000001") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "monitor_leases");
        }
    }
}

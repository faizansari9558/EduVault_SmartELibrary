using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartELibrary.Migrations
{
    /// <inheritdoc />
    public partial class SeedStabilityFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAtUtc", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "pqp7RUbHAaT3rE1FB2yLYA==.DDvLiDdTGAeURgKOJzbSL8PIJIMsve3hWpUHfv9JOrk=" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAtUtc", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 17, 12, 30, 54, 733, DateTimeKind.Utc).AddTicks(6039), "KVgDUryHesL2qs8B56d/1g==.ppWak9MKg7IhcC8FxEazFukkmLvvrotAkBYKnfjpL0c=" });
        }
    }
}

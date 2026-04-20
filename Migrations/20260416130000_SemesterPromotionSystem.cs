using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartELibrary.Migrations
{
    /// <inheritdoc />
    public partial class SemesterPromotionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentSemesterId",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromotionStatus",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Semesters",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Semesters",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PromotionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FromSemesterId = table.Column<int>(type: "int", nullable: false),
                    ToSemesterId = table.Column<int>(type: "int", nullable: false),
                    TotalPromoted = table.Column<int>(type: "int", nullable: false),
                    TotalHeld = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionLogs_Semesters_FromSemesterId",
                        column: x => x.FromSemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionLogs_Semesters_ToSemesterId",
                        column: x => x.ToSemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Students_CurrentSemesterId",
                table: "Students",
                column: "CurrentSemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionLogs_FromSemesterId",
                table: "PromotionLogs",
                column: "FromSemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionLogs_ToSemesterId",
                table: "PromotionLogs",
                column: "ToSemesterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Semesters_CurrentSemesterId",
                table: "Students",
                column: "CurrentSemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Semesters_CurrentSemesterId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "PromotionLogs");

            migrationBuilder.DropIndex(
                name: "IX_Students_CurrentSemesterId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CurrentSemesterId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "PromotionStatus",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Semesters");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Semesters");
        }
    }
}

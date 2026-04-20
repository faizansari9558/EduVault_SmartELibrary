using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartELibrary.Migrations
{
    /// <inheritdoc />
    public partial class RichTextPagesAndPageProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaterialPageId",
                table: "Quizzes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HtmlContent = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialPages_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MaterialPageProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    MaterialPageId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TimeSpentSeconds = table.Column<double>(type: "double", nullable: false),
                    IsCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialPageProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialPageProgress_MaterialPages_MaterialPageId",
                        column: x => x.MaterialPageId,
                        principalTable: "MaterialPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialPageProgress_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_MaterialPageId",
                table: "Quizzes",
                column: "MaterialPageId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialPageProgress_MaterialPageId",
                table: "MaterialPageProgress",
                column: "MaterialPageId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialPageProgress_StudentId_MaterialPageId",
                table: "MaterialPageProgress",
                columns: new[] { "StudentId", "MaterialPageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialPages_MaterialId_PageNumber",
                table: "MaterialPages",
                columns: new[] { "MaterialId", "PageNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_MaterialPages_MaterialPageId",
                table: "Quizzes",
                column: "MaterialPageId",
                principalTable: "MaterialPages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_MaterialPages_MaterialPageId",
                table: "Quizzes");

            migrationBuilder.DropTable(
                name: "MaterialPageProgress");

            migrationBuilder.DropTable(
                name: "MaterialPages");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_MaterialPageId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "MaterialPageId",
                table: "Quizzes");
        }
    }
}

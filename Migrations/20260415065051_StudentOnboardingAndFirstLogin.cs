using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartELibrary.Migrations
{
    /// <inheritdoc />
    public partial class StudentOnboardingAndFirstLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFirstLogin",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "StudentOnboardingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EnrollmentNo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateOfBirth = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    IsRegistered = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RegisteredUserId = table.Column<int>(type: "int", nullable: true),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentOnboardingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentOnboardingRecords_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentOnboardingRecords_Users_RegisteredUserId",
                        column: x => x.RegisteredUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsFirstLogin",
                value: false);

            migrationBuilder.CreateIndex(
                name: "IX_StudentOnboardingRecords_EnrollmentNo",
                table: "StudentOnboardingRecords",
                column: "EnrollmentNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentOnboardingRecords_RegisteredUserId",
                table: "StudentOnboardingRecords",
                column: "RegisteredUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentOnboardingRecords_SemesterId",
                table: "StudentOnboardingRecords",
                column: "SemesterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentOnboardingRecords");

            migrationBuilder.DropColumn(
                name: "IsFirstLogin",
                table: "Users");
        }
    }
}

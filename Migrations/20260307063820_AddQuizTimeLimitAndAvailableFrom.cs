using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartELibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizTimeLimitAndAvailableFrom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TimeLimitMinutes as a new column
            migrationBuilder.AddColumn<int>(
                name: "TimeLimitMinutes",
                table: "Quizzes",
                type: "int",
                nullable: true);

            // Add AvailableFromUtc column
            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableFromUtc",
                table: "Quizzes",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeLimitMinutes",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "AvailableFromUtc",
                table: "Quizzes");
        }
    }
}

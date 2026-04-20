using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartELibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizMarksFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorrectAnswers",
                table: "QuizResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalQuestions",
                table: "QuizResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuizCorrectAnswers",
                table: "ProgressTrackings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuizTotalQuestions",
                table: "ProgressTrackings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectAnswers",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "TotalQuestions",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "QuizCorrectAnswers",
                table: "ProgressTrackings");

            migrationBuilder.DropColumn(
                name: "QuizTotalQuestions",
                table: "ProgressTrackings");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartELibrary.Migrations
{
    /// <inheritdoc />
    public partial class UpgradePhases_1to4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                // MySQL requires the FK to be dropped before its backing index
                migrationBuilder.DropForeignKey(
                    name: "FK_ProgressTrackings_Users_StudentId",
                    table: "ProgressTrackings");

                migrationBuilder.DropIndex(
                    name: "IX_ProgressTrackings_StudentId",
                    table: "ProgressTrackings");

            migrationBuilder.AddColumn<int>(
                name: "QuizDurationMinutes",
                table: "Quizzes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "QuizResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoSubmitted",
                table: "QuizResults",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUtc",
                table: "QuizResults",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeTakenSeconds",
                table: "QuizResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgressLevel",
                table: "ProgressTrackings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SemesterId",
                table: "ProgressTrackings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Materials",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "Materials",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsDownloadAllowed",
                table: "Materials",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ProgressTrackings_SemesterId",
                table: "ProgressTrackings",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressTrackings_StudentId_MaterialId_ProgressLevel",
                table: "ProgressTrackings",
                columns: new[] { "StudentId", "MaterialId", "ProgressLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressTrackings_StudentId_SemesterId_ProgressLevel",
                table: "ProgressTrackings",
                columns: new[] { "StudentId", "SemesterId", "ProgressLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressTrackings_StudentId_SubjectId_ProgressLevel",
                table: "ProgressTrackings",
                columns: new[] { "StudentId", "SubjectId", "ProgressLevel" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressTrackings_Semesters_SemesterId",
                table: "ProgressTrackings",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

                // Restore the StudentId FK (composite indexes starting with StudentId cover it)
                migrationBuilder.AddForeignKey(
                    name: "FK_ProgressTrackings_Users_StudentId",
                    table: "ProgressTrackings",
                    column: "StudentId",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgressTrackings_Semesters_SemesterId",
                table: "ProgressTrackings");

            migrationBuilder.DropIndex(
                name: "IX_ProgressTrackings_SemesterId",
                table: "ProgressTrackings");

                // Drop FK before dropping the composite indexes that back it
                migrationBuilder.DropForeignKey(
                    name: "FK_ProgressTrackings_Users_StudentId",
                    table: "ProgressTrackings");

                migrationBuilder.DropIndex(
                    name: "IX_ProgressTrackings_StudentId_MaterialId_ProgressLevel",
                    table: "ProgressTrackings");

            migrationBuilder.DropIndex(
                name: "IX_ProgressTrackings_StudentId_SemesterId_ProgressLevel",
                table: "ProgressTrackings");

            migrationBuilder.DropIndex(
                name: "IX_ProgressTrackings_StudentId_SubjectId_ProgressLevel",
                table: "ProgressTrackings");

            migrationBuilder.DropColumn(
                name: "QuizDurationMinutes",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "IsAutoSubmitted",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "TimeTakenSeconds",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "ProgressLevel",
                table: "ProgressTrackings");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "ProgressTrackings");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "IsDownloadAllowed",
                table: "Materials");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressTrackings_StudentId",
                table: "ProgressTrackings",
                column: "StudentId");

                // Restore FK now that the single-column index is back
                migrationBuilder.AddForeignKey(
                    name: "FK_ProgressTrackings_Users_StudentId",
                    table: "ProgressTrackings",
                    column: "StudentId",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
        }
    }
}

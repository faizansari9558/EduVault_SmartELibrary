using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartELibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectCodeAndSingleTeacherPerSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubjectCode",
                table: "Subjects",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"
                UPDATE Subjects
                SET SubjectCode = CONCAT('SUB', LPAD(Id, 4, '0'))
                WHERE SubjectCode IS NULL OR SubjectCode = '';
            ");

            migrationBuilder.AlterColumn<int>(
                name: "PromotionStatus",
                table: "Students",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "SubjectCode",
                table: "Subjects",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"
                DELETE ts_old
                FROM TeacherSubjects ts_old
                INNER JOIN TeacherSubjects ts_new
                    ON ts_old.SubjectId = ts_new.SubjectId
                    AND ts_old.Id < ts_new.Id;
            ");

            migrationBuilder.CreateIndex(
                name: "UX_TeacherSubjects_SubjectId",
                table: "TeacherSubjects",
                column: "SubjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SubjectCode",
                table: "Subjects",
                column: "SubjectCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_TeacherSubjects_SubjectId",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_SubjectCode",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "SubjectCode",
                table: "Subjects");

            migrationBuilder.AlterColumn<int>(
                name: "PromotionStatus",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}

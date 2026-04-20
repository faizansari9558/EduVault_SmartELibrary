using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartELibrary.Migrations
{
    /// <inheritdoc />
    public partial class RenameTeacherCodeToTeacherId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MariaDB compatibility: it may not support "RENAME COLUMN".
            // CHANGE COLUMN preserves existing data while renaming the column.
            migrationBuilder.Sql("ALTER TABLE `Teachers` CHANGE COLUMN `TeacherCode` `TeacherId` varchar(20) CHARACTER SET utf8mb4 NOT NULL;");

            // Recreate the unique index with the new column name.
            migrationBuilder.Sql("DROP INDEX `IX_Teachers_TeacherCode` ON `Teachers`;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX `IX_Teachers_TeacherId` ON `Teachers` (`TeacherId`);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE `Teachers` CHANGE COLUMN `TeacherId` `TeacherCode` varchar(20) CHARACTER SET utf8mb4 NOT NULL;");

            migrationBuilder.Sql("DROP INDEX `IX_Teachers_TeacherId` ON `Teachers`;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX `IX_Teachers_TeacherCode` ON `Teachers` (`TeacherCode`);");
        }
    }
}

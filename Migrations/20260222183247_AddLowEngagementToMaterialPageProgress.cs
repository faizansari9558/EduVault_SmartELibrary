using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartELibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddLowEngagementToMaterialPageProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LowEngagement",
                table: "MaterialPageProgress",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LowEngagement",
                table: "MaterialPageProgress");
        }
    }
}

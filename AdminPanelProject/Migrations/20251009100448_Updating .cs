using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPanelProject.Migrations
{
    /// <inheritdoc />
    public partial class Updating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");
        }
    }
}

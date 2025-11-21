using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPanelProject.Migrations
{
    /// <inheritdoc />
    public partial class addingmyCreatedOnIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedOn",
                table: "Users",
                column: "CreatedOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedOn",
                table: "Users");
        }
    }
}

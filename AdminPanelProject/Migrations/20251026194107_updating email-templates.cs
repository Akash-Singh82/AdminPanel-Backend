using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPanelProject.Migrations
{
    /// <inheritdoc />
    public partial class updatingemailtemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "EmailTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "EmailTemplates",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "EmailTemplates");
        }
    }
}

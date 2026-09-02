using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeugnisApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKindSchulbesuchsjahr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Schulbesuchsjahr",
                table: "Kinder",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Schulbesuchsjahr",
                table: "Kinder");
        }
    }
}

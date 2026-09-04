using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeugnisApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddZeugnisFehlstundenOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FehlstundenManuellUeberschrieben",
                table: "Zeugnisse",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FehlstundenManuellUeberschrieben",
                table: "Zeugnisse");
        }
    }
}

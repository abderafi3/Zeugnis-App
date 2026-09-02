using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeugnisApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolAndFloskelMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Schulnummer",
                table: "SchoolSettings",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kuerzel",
                table: "Floskeln",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Niveau",
                table: "Floskeln",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Floskeln_Kuerzel",
                table: "Floskeln",
                column: "Kuerzel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Floskeln_Kuerzel",
                table: "Floskeln");

            migrationBuilder.DropColumn(
                name: "Schulnummer",
                table: "SchoolSettings");

            migrationBuilder.DropColumn(
                name: "Kuerzel",
                table: "Floskeln");

            migrationBuilder.DropColumn(
                name: "Niveau",
                table: "Floskeln");
        }
    }
}

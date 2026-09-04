using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeugnisApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFoerderempfehlung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Foerderempfehlungen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    KindId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Schuljahr = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Halbjahr = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Fach = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Beobachtungen = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    SchulischeMassnahmen = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    EmpfehlungenFuerZuhause = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Fertiggestellt = table.Column<bool>(type: "INTEGER", nullable: false),
                    GeneratedFilePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foerderempfehlungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Foerderempfehlungen_Kinder_KindId",
                        column: x => x.KindId,
                        principalTable: "Kinder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Foerderempfehlungen_KindId_Schuljahr_Halbjahr_Fach",
                table: "Foerderempfehlungen",
                columns: new[] { "KindId", "Schuljahr", "Halbjahr", "Fach" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Foerderempfehlungen");
        }
    }
}

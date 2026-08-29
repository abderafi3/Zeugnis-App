using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeugnisApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditTrail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotJson = table.Column<string>(type: "TEXT", maxLength: 1000000, nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Bezeichnung = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Floskeln",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Bereich = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Stufe = table.Column<int>(type: "INTEGER", nullable: false),
                    Klassengruppe = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DeletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Floskeln", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Formulare",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Anzeigename = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Ueberschrift = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Jahrgang = table.Column<int>(type: "INTEGER", nullable: false),
                    GiltFuerHalbjahr = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    Faecher = table.Column<string>(type: "TEXT", nullable: false),
                    HatNotenblock = table.Column<bool>(type: "INTEGER", nullable: false),
                    HatBegruendeteEmpfehlung = table.Column<bool>(type: "INTEGER", nullable: false),
                    HatVersetzungsvermerk = table.Column<bool>(type: "INTEGER", nullable: false),
                    IstZieldifferent = table.Column<bool>(type: "INTEGER", nullable: false),
                    WordFilePath = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    FieldMappingJson = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: true),
                    DeletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Formulare", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Klassen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Jahrgang = table.Column<int>(type: "INTEGER", nullable: false),
                    Klassenleitung = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Zeugnisdatum = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Wochenstundenplan_Montag = table.Column<int>(type: "INTEGER", nullable: false),
                    Wochenstundenplan_Dienstag = table.Column<int>(type: "INTEGER", nullable: false),
                    Wochenstundenplan_Mittwoch = table.Column<int>(type: "INTEGER", nullable: false),
                    Wochenstundenplan_Donnerstag = table.Column<int>(type: "INTEGER", nullable: false),
                    Wochenstundenplan_Freitag = table.Column<int>(type: "INTEGER", nullable: false),
                    DeletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Klassen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Zusatzbezeichnung = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Adresse = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Schulleitung = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Schuljahr = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AktuellesHalbjahr = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Zeugnisausgabedatum = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Konferenzdatum = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DatumUnterrichtsWiederbeginn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PapierkorbAufbewahrungTage = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FreitextFeldDefinition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FormularId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Hinweistext = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Reihenfolge = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxZeichen = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreitextFeldDefinition", x => new { x.FormularId, x.Id });
                    table.ForeignKey(
                        name: "FK_FreitextFeldDefinition_Formulare_FormularId",
                        column: x => x.FormularId,
                        principalTable: "Formulare",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Kinder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Vorname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Nachname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Geburtsdatum = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Geschlecht = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    KlasseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Foerderstatus = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Foerderschwerpunkt = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ManuellesFormularId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DeletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kinder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kinder_Formulare_ManuellesFormularId",
                        column: x => x.ManuellesFormularId,
                        principalTable: "Formulare",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Kinder_Klassen_KlasseId",
                        column: x => x.KlasseId,
                        principalTable: "Klassen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fehlzeiten",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    KindId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Schuljahr = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Halbjahr = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    StundenGesamt = table.Column<int>(type: "INTEGER", nullable: false),
                    StundenUnentschuldigt = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fehlzeiten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fehlzeiten_Kinder_KindId",
                        column: x => x.KindId,
                        principalTable: "Kinder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Zeugnisse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    KindId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FormularId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Schuljahr = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Halbjahr = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Empfehlung = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EmpfehlungBegruendung = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    FehlstundenGesamt = table.Column<int>(type: "INTEGER", nullable: true),
                    FehlstundenUnentschuldigt = table.Column<int>(type: "INTEGER", nullable: true),
                    VersetzungsvermerkText = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Bemerkungen = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Fertiggestellt = table.Column<bool>(type: "INTEGER", nullable: false),
                    GeneratedFilePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DeletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zeugnisse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Zeugnisse_Formulare_FormularId",
                        column: x => x.FormularId,
                        principalTable: "Formulare",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Zeugnisse_Kinder_KindId",
                        column: x => x.KindId,
                        principalTable: "Kinder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FreitextEintrag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ZeugnisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FeldName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreitextEintrag", x => new { x.ZeugnisId, x.Id });
                    table.ForeignKey(
                        name: "FK_FreitextEintrag_Zeugnisse_ZeugnisId",
                        column: x => x.ZeugnisId,
                        principalTable: "Zeugnisse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NoteEintrag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ZeugnisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Fach = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteEintrag", x => new { x.ZeugnisId, x.Id });
                    table.ForeignKey(
                        name: "FK_NoteEintrag_Zeugnisse_ZeugnisId",
                        column: x => x.ZeugnisId,
                        principalTable: "Zeugnisse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_DeletedAtUtc",
                table: "AuditTrail",
                column: "DeletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_EntityType_EntityId",
                table: "AuditTrail",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Fehlzeiten_KindId_Schuljahr_Halbjahr",
                table: "Fehlzeiten",
                columns: new[] { "KindId", "Schuljahr", "Halbjahr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Floskeln_Bereich_Stufe_Klassengruppe",
                table: "Floskeln",
                columns: new[] { "Bereich", "Stufe", "Klassengruppe" });

            migrationBuilder.CreateIndex(
                name: "IX_Formulare_Jahrgang_GiltFuerHalbjahr",
                table: "Formulare",
                columns: new[] { "Jahrgang", "GiltFuerHalbjahr" });

            migrationBuilder.CreateIndex(
                name: "IX_Kinder_KlasseId_Nachname_Vorname",
                table: "Kinder",
                columns: new[] { "KlasseId", "Nachname", "Vorname" });

            migrationBuilder.CreateIndex(
                name: "IX_Kinder_ManuellesFormularId",
                table: "Kinder",
                column: "ManuellesFormularId");

            migrationBuilder.CreateIndex(
                name: "IX_Klassen_Name",
                table: "Klassen",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Zeugnisse_FormularId",
                table: "Zeugnisse",
                column: "FormularId");

            migrationBuilder.CreateIndex(
                name: "IX_Zeugnisse_KindId_FormularId_Schuljahr_Halbjahr",
                table: "Zeugnisse",
                columns: new[] { "KindId", "FormularId", "Schuljahr", "Halbjahr" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail");

            migrationBuilder.DropTable(
                name: "Fehlzeiten");

            migrationBuilder.DropTable(
                name: "Floskeln");

            migrationBuilder.DropTable(
                name: "FreitextEintrag");

            migrationBuilder.DropTable(
                name: "FreitextFeldDefinition");

            migrationBuilder.DropTable(
                name: "NoteEintrag");

            migrationBuilder.DropTable(
                name: "SchoolSettings");

            migrationBuilder.DropTable(
                name: "Zeugnisse");

            migrationBuilder.DropTable(
                name: "Kinder");

            migrationBuilder.DropTable(
                name: "Formulare");

            migrationBuilder.DropTable(
                name: "Klassen");
        }
    }
}

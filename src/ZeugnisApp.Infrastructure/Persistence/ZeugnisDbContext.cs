using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Domain.Common;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context over the SQLite data file (plan §3 data layer).
/// Schema evolution is handled by migrations (plan §2) — never <c>EnsureCreated</c>.
/// </summary>
public sealed class ZeugnisDbContext(DbContextOptions<ZeugnisDbContext> options, IClock clock)
    : DbContext(options)
{
    public DbSet<SchoolSettings> SchoolSettings => Set<SchoolSettings>();
    public DbSet<Klasse> Klassen => Set<Klasse>();
    public DbSet<Kind> Kinder => Set<Kind>();
    public DbSet<Floskel> Floskeln => Set<Floskel>();
    public DbSet<Formular> Formulare => Set<Formular>();
    public DbSet<Fehlzeit> Fehlzeiten => Set<Fehlzeit>();
    public DbSet<Fehltag> Fehltage => Set<Fehltag>();
    public DbSet<Zeugnis> Zeugnisse => Set<Zeugnis>();
    public DbSet<AuditTrailEntry> AuditTrail => Set<AuditTrailEntry>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Soft-deleted aggregates (Kind, Klasse, Zeugnis) are filtered globally; their required
        // relationships (Fehlzeit -> Kind, Zeugnis -> Kind) are handled by cascade delete for hard
        // deletes and by matching soft-delete of the children. The advisory does not apply here.
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Store enums as text so the raw data file stays human-inspectable during support.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(64);
        configurationBuilder.Properties<string>().HaveMaxLength(4000);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var b = modelBuilder;

        // ---------- SchoolSettings (singleton) ----------
        b.Entity<SchoolSettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Schuljahr).HasMaxLength(20);
        });

        // ---------- Klasse ----------
        b.Entity<Klasse>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.Klassenleitung).HasMaxLength(200);
            e.HasIndex(x => x.Name);
            e.OwnsOne(x => x.Wochenstundenplan);
            e.HasMany(x => x.Kinder)
                .WithOne(k => k.Klasse!)
                .HasForeignKey(k => k.KlasseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => x.DeletedUtc == null);
        });

        // ---------- Kind ----------
        b.Entity<Kind>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Vorname).HasMaxLength(100);
            e.Property(x => x.Nachname).HasMaxLength(100);
            e.Property(x => x.Foerderschwerpunkt).HasMaxLength(500);
            e.HasIndex(x => new { x.KlasseId, x.Nachname, x.Vorname });
            e.HasOne(x => x.ManuellesFormular)
                .WithMany()
                .HasForeignKey(x => x.ManuellesFormularId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Ignore(x => x.IstVollstaendig);
            e.Ignore(x => x.AnzeigeName);
            e.HasQueryFilter(x => x.DeletedUtc == null);
        });

        // ---------- Floskel ----------
        b.Entity<Floskel>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Bereich).HasMaxLength(100);
            e.HasIndex(x => new { x.Bereich, x.Stufe, x.Klassengruppe });
            e.HasQueryFilter(x => x.DeletedUtc == null);
        });

        // ---------- Formular ----------
        b.Entity<Formular>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Anzeigename).HasMaxLength(200).IsRequired();
            e.Property(x => x.Ueberschrift).HasMaxLength(500);
            e.Property(x => x.GiltFuerHalbjahr).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.FieldMappingJson).HasMaxLength(20000);
            e.HasIndex(x => new { x.Jahrgang, x.GiltFuerHalbjahr });
            e.PrimitiveCollection(x => x.Faecher);
            e.OwnsMany(x => x.Freitextfelder, f =>
            {
                f.Property(p => p.Name).HasMaxLength(100).IsRequired();
                f.Property(p => p.Hinweistext).HasMaxLength(1000);
            });
            e.HasQueryFilter(x => x.DeletedUtc == null);
        });

        // ---------- Fehlzeit ----------
        b.Entity<Fehlzeit>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Schuljahr).HasMaxLength(20);
            e.HasOne(x => x.Kind).WithMany().HasForeignKey(x => x.KindId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.KindId, x.Schuljahr, x.Halbjahr }).IsUnique();
            e.Ignore(x => x.IstGueltig);
        });

        // ---------- Fehltag (calendar variant — modelled, UI not linked, plan §4) ----------
        b.Entity<Fehltag>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Bemerkung).HasMaxLength(500);
            e.HasOne(x => x.Kind).WithMany().HasForeignKey(x => x.KindId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.KindId, x.Datum });
        });

        // ---------- Zeugnis ----------
        b.Entity<Zeugnis>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Schuljahr).HasMaxLength(20);
            e.Property(x => x.Empfehlung).HasMaxLength(500);
            e.Property(x => x.EmpfehlungBegruendung).HasMaxLength(4000);
            e.Property(x => x.VersetzungsvermerkText).HasMaxLength(2000);
            e.Property(x => x.Bemerkungen).HasMaxLength(4000);
            e.Property(x => x.GeneratedFilePath).HasMaxLength(1000);

            e.HasOne(x => x.Kind).WithMany().HasForeignKey(x => x.KindId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Formular).WithMany().HasForeignKey(x => x.FormularId).OnDelete(DeleteBehavior.Restrict);

            e.OwnsMany(x => x.Freitexte, f =>
            {
                f.Property(p => p.FeldName).HasMaxLength(100).IsRequired();
                f.Property(p => p.Text).HasMaxLength(8000);
            });
            e.OwnsMany(x => x.Noten, n =>
            {
                n.Property(p => p.Fach).HasMaxLength(100).IsRequired();
                n.Property(p => p.Note).HasMaxLength(50);
            });

            e.HasIndex(x => new { x.KindId, x.FormularId, x.Schuljahr, x.Halbjahr }).IsUnique();
            e.HasQueryFilter(x => x.DeletedUtc == null);
        });

        // ---------- AuditTrailEntry ----------
        b.Entity<AuditTrailEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityType).HasMaxLength(100);
            e.Property(x => x.Bezeichnung).HasMaxLength(300);
            e.Property(x => x.SnapshotJson).HasMaxLength(1_000_000);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.DeletedAtUtc);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = clock.UtcNow;
        foreach (EntityEntry<EntityBase> entry in ChangeTracker.Entries<EntityBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedUtc = now;
                    entry.Entity.ModifiedUtc = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedUtc = now;
                    break;
            }
        }
    }
}

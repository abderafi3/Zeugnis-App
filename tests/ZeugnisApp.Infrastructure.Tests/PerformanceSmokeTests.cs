using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;
using ZeugnisApp.Infrastructure.Services;

namespace ZeugnisApp.Infrastructure.Tests;

/// <summary>
/// Guards against O(n²) / N+1 regressions at a realistic single-school, multi-year data volume
/// (plan §Sprint 12 performance pass). Bounds are deliberately generous — the point is "instant",
/// not a micro-benchmark.
/// </summary>
public sealed class PerformanceSmokeTests
{
    [Fact]
    public async Task Key_list_queries_stay_fast_with_years_of_data()
    {
        await using var db = new SqliteTestDatabase();
        await new DatabaseInitializer(db.ContextFactory).InitializeAsync();

        // ~10 classes x 26 children x 3 school years x 2 terms of reports + absence rows.
        await using (var ctx = db.NewContext())
        {
            var formularId = await ctx.Formulare.Select(f => f.Id).FirstAsync();
            var jahre = new[] { "2024/25", "2025/26", "2026/27" };

            for (var c = 0; c < 10; c++)
            {
                var klasse = new Klasse { Name = $"K{c}", Jahrgang = (c % 4) + 1 };
                ctx.Klassen.Add(klasse);

                for (var k = 0; k < 26; k++)
                {
                    var kind = new Kind { Vorname = $"Kind{k}", Nachname = $"N{c}", Klasse = klasse };
                    ctx.Kinder.Add(kind);

                    foreach (var jahr in jahre)
                    {
                        foreach (var hj in new[] { Halbjahr.Erstes, Halbjahr.Zweites })
                        {
                            ctx.Zeugnisse.Add(new Zeugnis { KindId = kind.Id, FormularId = formularId, Schuljahr = jahr, Halbjahr = hj });
                            ctx.Fehlzeiten.Add(new Fehlzeit { KindId = kind.Id, Schuljahr = jahr, Halbjahr = hj, StundenGesamt = 8, StundenUnentschuldigt = 1 });
                        }
                    }
                }
            }

            await ctx.SaveChangesAsync();
        }

        var klassen = new KlassenService(db.ContextFactory, db.Clock);
        var dashboard = new DashboardService(db.NewContext());
        var zeugnisse = new ZeugnisService(db.ContextFactory);
        var fehlzeiten = new FehlzeitenService(db.ContextFactory);

        var ersteKlasse = (await klassen.GetKlassenAsync())[0].Id;

        var sw = Stopwatch.StartNew();
        _ = await dashboard.GetCountsAsync();
        _ = await klassen.GetKlassenAsync();
        _ = await zeugnisse.GetUebersichtForKlasseAsync(ersteKlasse, "2026/27", Halbjahr.Erstes);
        _ = await fehlzeiten.GetForKlasseAsync(ersteKlasse, "2026/27", Halbjahr.Erstes);
        sw.Stop();

        // Generous: catches an O(n²)/N+1 regression without being a flaky micro-benchmark under
        // parallel test load. The four queries together are milliseconds on real hardware.
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Listenabfragen dauerten {sw.ElapsedMilliseconds} ms");
    }
}

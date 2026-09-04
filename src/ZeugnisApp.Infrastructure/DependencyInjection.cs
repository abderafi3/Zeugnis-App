using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Infrastructure.Import;
using ZeugnisApp.Infrastructure.Persistence;
using ZeugnisApp.Infrastructure.Security;
using ZeugnisApp.Infrastructure.Services;

namespace ZeugnisApp.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the data layer and infrastructure services (plan §3).
    /// </summary>
    /// <param name="connectionString">
    /// SQLite connection string. Sprint 11 extends this to carry the SQLCipher key
    /// (see <c>Spike.SqlCipher</c> for the proven approach).
    /// </param>
    public static IServiceCollection AddZeugnisInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Load the SQLCipher native provider before any connection is opened (plan §Sprint 11).
        SQLitePCL.Batteries_V2.Init();

        services.AddSingleton<IClock, SystemClock>();

        // A pooled factory: the WPF app is single-user but creates short-lived contexts
        // per screen/operation rather than one long-lived context.
        services.AddDbContextFactory<ZeugnisDbContext>(options =>
            options.UseSqlite(connectionString, sqlite =>
                sqlite.MigrationsAssembly(typeof(ZeugnisDbContext).Assembly.FullName)));

        // Also expose a scoped DbContext for services that take one directly.
        services.AddScoped<ZeugnisDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<ZeugnisDbContext>>().CreateDbContext());

        services.AddSingleton<DatabaseInitializer>();

        services.AddScoped<ISchoolSettingsService, SchoolSettingsService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IKlassenService, KlassenService>();
        services.AddScoped<IFloskelnService, FloskelnService>();
        services.AddScoped<IFormulareService, FormulareService>();
        services.AddScoped<IFehlzeitenService, FehlzeitenService>();
        services.AddScoped<IZeugnisService, ZeugnisService>();
        services.AddScoped<IFoerderempfehlungService, FoerderempfehlungService>();
        services.AddSingleton<ITabularImport, TabularImport>();
        services.AddScoped<IKinderImportService, KinderImportService>();
        services.AddScoped<IFloskelImportService, FloskelImportService>();
        services.AddScoped<IFehlzeitenImportService, FehlzeitenImportService>();
        services.AddScoped<IPapierkorbService, PapierkorbService>();
        services.AddSingleton<IBackupService, BackupService>();

        return services;
    }
}

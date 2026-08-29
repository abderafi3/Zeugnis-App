using Microsoft.Extensions.DependencyInjection;
using ZeugnisApp.Application.Abstractions;

namespace ZeugnisApp.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddZeugnisReporting(this IServiceCollection services)
    {
        services.AddSingleton<IReportGenerationEngine, OpenXmlReportGenerationEngine>();
        return services;
    }
}

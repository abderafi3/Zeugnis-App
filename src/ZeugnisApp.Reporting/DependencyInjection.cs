using Microsoft.Extensions.DependencyInjection;
using ZeugnisApp.Application.Abstractions;
using ZeugnisApp.Reporting.Ooxml;

namespace ZeugnisApp.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddZeugnisReporting(this IServiceCollection services)
    {
        services.AddSingleton<IReportGenerationEngine, OpenXmlReportGenerationEngine>();
        services.AddSingleton<ITemplateInspector, TemplateInspector>();
        return services;
    }
}

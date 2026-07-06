using System;
using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Http;
using ERP.Fiscal.PlugNotas.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ERP.Fiscal.PlugNotas.Extensions;

/// <summary>
/// Registro dos providers PlugNotas sem depender do sistema de módulos ABP — útil em testes de
/// integração isolados ou hosts que não usam <c>AbpModule</c>. Uso normal em produção: módulo ABP
/// <see cref="PlugNotasFiscalModule"/> via <c>[DependsOn]</c>.
/// </summary>
public static class PlugNotasServiceCollectionExtensions
{
    public static IServiceCollection AddPlugNotasFiscal(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PlugNotasOptions>(configuration.GetSection(PlugNotasOptions.SectionName));

        services.AddHttpClient<PlugNotasHttpClient>()
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromMinutes(2));

        services.AddHttpClient<INfeAuxiliaresProvider, PlugNotasAuxiliaresProvider>()
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));

        services.AddTransient<INfeEmissaoProvider, PlugNotasNfeEmissaoProvider>();
        services.AddTransient<INfeIntegracaoProvider, PlugNotasIntegracaoProvider>();
        services.AddTransient<INfeDestinadaProvider, PlugNotasDestinadaProvider>();

        return services;
    }

    /// <summary>Registra <see cref="PlugNotasDefaultAmbientePolicy"/> se o consumidor ainda não definiu <see cref="INfeAmbientePolicy"/>.</summary>
    public static IServiceCollection AddPlugNotasDefaultAmbientePolicy(this IServiceCollection services)
    {
        services.TryAddTransient<INfeAmbientePolicy, PlugNotasDefaultAmbientePolicy>();
        return services;
    }
}

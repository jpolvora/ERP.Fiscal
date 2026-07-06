using System;
using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Http;
using ERP.Fiscal.PlugNotas.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace ERP.Fiscal.PlugNotas;

/// <summary>
/// Módulo ABP plugável (backend-only, sem views) que registra a implementação PlugNotas das
/// abstrações de <see cref="ERP.Fiscal.Abstractions"/>. Consumo: <c>[DependsOn(typeof(PlugNotasFiscalModule))]</c>
/// no <c>*ApplicationModule</c> do ERP consumidor.
/// </summary>
public class PlugNotasFiscalModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<PlugNotasOptions>(configuration.GetSection(PlugNotasOptions.SectionName));

        context.Services.AddHttpClient<PlugNotasHttpClient>()
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromMinutes(2));

        context.Services.AddHttpClient<INfeAuxiliaresProvider, PlugNotasAuxiliaresProvider>()
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));

        context.Services.AddTransient<INfeEmissaoProvider, PlugNotasNfeEmissaoProvider>();
        context.Services.AddTransient<INfeIntegracaoProvider, PlugNotasIntegracaoProvider>();
        context.Services.AddTransient<INfeDestinadaProvider, PlugNotasDestinadaProvider>();
    }
}

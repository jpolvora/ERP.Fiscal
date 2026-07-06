using ERP.Fiscal.Abstractions;
using Microsoft.Extensions.Options;

namespace ERP.Fiscal.PlugNotas.Configuration;

/// <summary>Resolve o token <c>x-api-key</c> PlugNotas conforme o <see cref="NfeAmbiente"/> efetivo.</summary>
public static class PlugNotasApiKeyResolver
{
    /// <summary>
    /// Sandbox: <see cref="PlugNotasOptions.SandboxApiKey"/> se preenchida; senão <see cref="PlugNotasAmbienteConstants.PublicSandboxApiKey"/>.
    /// Homologação/Produção: <see cref="PlugNotasOptions.ProductionApiKey"/> obrigatória.
    /// </summary>
    public static string Resolve(IOptions<PlugNotasOptions> options, NfeAmbiente ambiente)
    {
        if (ambiente == NfeAmbiente.Sandbox)
        {
            var sandboxKey = options.Value.SandboxApiKey?.Trim();
            if (string.IsNullOrEmpty(sandboxKey))
                sandboxKey = PlugNotasAmbienteConstants.PublicSandboxApiKey;
            return sandboxKey;
        }

        var productionKey = options.Value.ProductionApiKey?.Trim();
        if (string.IsNullOrEmpty(productionKey))
            throw new PlugNotasConfigurationException("PlugNotas: ProductionApiKey não configurada para ambiente de Homologação/Produção.");
        return productionKey;
    }
}

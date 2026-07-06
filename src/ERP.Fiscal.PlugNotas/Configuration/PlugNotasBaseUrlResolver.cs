using ERP.Fiscal.Abstractions;

namespace ERP.Fiscal.PlugNotas.Configuration;

/// <summary>Resolve a URL base da API PlugNotas conforme o <see cref="NfeAmbiente"/> efetivo.</summary>
public static class PlugNotasBaseUrlResolver
{
    public static string Resolve(NfeAmbiente ambiente) =>
        ambiente == NfeAmbiente.Sandbox
            ? PlugNotasAmbienteConstants.SandboxBaseUrl
            : PlugNotasAmbienteConstants.ApiOficialBaseUrl;
}

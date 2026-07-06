namespace ERP.Fiscal.PlugNotas.Configuration;

/// <summary>
/// URLs e chave pública mock PlugNotas (documentação oficial). Homologação/produção usam
/// <see cref="ApiOficialBaseUrl"/> e token do portal PlugNotas.
/// </summary>
public static class PlugNotasAmbienteConstants
{
    public const string ApiOficialBaseUrl = "https://api.plugnotas.com.br";

    public const string SandboxBaseUrl = "https://api.sandbox.plugnotas.com.br";

    public const string PublicSandboxApiKey = "2da392a6-79d2-4304-a8b7-959572c7e44d";
}

namespace ERP.Fiscal.Abstractions;

/// <summary>Validação neutra de resultados de consultas auxiliares (sem localização).</summary>
public static class NfeAuxiliaresResultExtensions
{
    public static bool TemDadosCadastrais(this NfeConsultaCepResult result) =>
        !string.IsNullOrWhiteSpace(result.Municipio) || !string.IsNullOrWhiteSpace(result.Logradouro);

    public static bool TemDadosCadastrais(this NfeConsultaCnpjResult result) =>
        !string.IsNullOrWhiteSpace(result.RazaoSocial);
}

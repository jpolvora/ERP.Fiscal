namespace ERP.Fiscal.PlugNotas.Parsers;

/// <summary>Classifica falhas HTTP da API PlugNotas para retry e mensagens ao usuário.</summary>
public static class PlugNotasHttpErrorClassifier
{
    /// <summary>Falhas retentáveis: rede/timeout (0), timeout HTTP, rate limit e erros de gateway.</summary>
    public static bool IsTransient(int httpStatusCode) =>
        httpStatusCode switch
        {
            0 or 408 or 429 or 502 or 503 or 504 => true,
            _ => false
        };
}

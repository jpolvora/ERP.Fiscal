namespace ERP.Fiscal.Abstractions;

/// <summary>
/// Regras neutras para validar identificadores retornados pelo provedor fiscal (ex.: id MongoDB PlugNotas, protocolo SEFAZ).
/// Permite uso na camada de domínio sem referenciar implementação PlugNotas.
/// </summary>
public static class NfeProvedorIdentificadorRules
{
    /// <summary>Id MongoDB do documento no PlugNotas (~24 hex), usado em GET <c>/nfe/{id}/...</c>.</summary>
    public static bool LooksLikeIdDocumentoPlugNotas(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var s = value.Trim();
        return s.Length == 24 && IsHexString(s);
    }

    /// <summary>Protocolo de autorização SEFAZ — numérico, tipicamente 15 dígitos.</summary>
    public static bool LooksLikeProtocoloSefaz(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var s = value.Trim();
        return s.Length >= 12 && IsAllDigits(s);
    }

    private static bool IsHexString(string s)
    {
        foreach (var c in s)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                return false;
        }

        return true;
    }

    private static bool IsAllDigits(string s)
    {
        foreach (var c in s)
        {
            if (c is < '0' or > '9')
                return false;
        }

        return true;
    }
}

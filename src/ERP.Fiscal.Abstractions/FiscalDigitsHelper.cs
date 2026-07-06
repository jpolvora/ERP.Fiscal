namespace ERP.Fiscal.Abstractions;

/// <summary>Normalização genérica de documentos/códigos numéricos (CNPJ, CEP, IBGE, etc.).</summary>
public static class FiscalDigitsHelper
{
    public static string DigitsOnly(string? s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        Span<char> buffer = stackalloc char[s.Length];
        var count = 0;
        foreach (var c in s)
        {
            if (c is >= '0' and <= '9')
            {
                buffer[count++] = c;
            }
        }

        return count == 0 ? string.Empty : new string(buffer[..count]);
    }

    public static string? DigitsOnlyOrNull(string? s)
    {
        var digits = DigitsOnly(s);
        return digits.Length == 0 ? null : digits;
    }
}

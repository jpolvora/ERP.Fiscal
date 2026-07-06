using System;
using System.Globalization;
using System.Text.Json;

namespace ERP.Fiscal.PlugNotas.Parsers;

/// <summary>Extrai campos úteis de respostas de consulta de empresa/certificado na API PlugNotas.</summary>
internal static class PlugNotasIntegracaoConsultaRespostaParser
{
    public static PlugNotasIntegracaoConsultaCamposParseados? TryParse(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                return ParseObject(root[0]);
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (TryGetPropertyIgnoreCase(root, "data", out var data))
            {
                if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                {
                    return ParseObject(data[0]);
                }

                if (data.ValueKind == JsonValueKind.Object)
                {
                    return ParseObject(data);
                }
            }

            return ParseObject(root);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static PlugNotasIntegracaoConsultaCamposParseados ParseObject(JsonElement obj)
    {
        var result = new PlugNotasIntegracaoConsultaCamposParseados();
        if (obj.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        result.IdProvedor = TryGetStringAny(obj, "id");
        result.CpfCnpj = TryGetStringAny(obj, "cpfCnpj", "cnpj", "cpf");
        result.Nome = TryGetStringAny(obj, "razaoSocial", "nomeFantasia", "nome");
        result.Email = TryGetStringAny(obj, "email");
        result.ValidadeInicial = TryGetDateTimeAny(obj, "validadeInicial", "dataInicioVigencia");
        result.ValidadeFinal = TryGetDateTimeAny(obj, "validadeFinal", "dataVencimento", "vencimento", "validade");
        result.Producao = TryGetBoolByPath(obj, "nfe", "config", "producao")
            ?? TryGetBoolByPath(obj, "nfe", "producao");

        return result;
    }

    private static string? TryGetStringAny(JsonElement obj, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetStringIgnoreCase(obj, name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static DateTime? TryGetDateTimeAny(JsonElement obj, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetPropertyIgnoreCase(obj, name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String
                && DateTime.TryParse(
                    value.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool? TryGetBoolByPath(JsonElement obj, params string[] path)
    {
        var current = obj;
        foreach (var segment in path)
        {
            if (!TryGetPropertyIgnoreCase(current, segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(current.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static bool TryGetStringIgnoreCase(JsonElement obj, string name, out string? value)
    {
        value = null;
        if (!TryGetPropertyIgnoreCase(obj, name, out var el))
        {
            return false;
        }

        value = el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.GetRawText(),
            _ => null
        };

        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
    {
        value = default;
        if (obj.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in obj.EnumerateObject())
        {
            if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = property.Value;
            return true;
        }

        return false;
    }
}

internal sealed class PlugNotasIntegracaoConsultaCamposParseados
{
    public string? IdProvedor { get; set; }
    public string? CpfCnpj { get; set; }
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public DateTime? ValidadeInicial { get; set; }
    public DateTime? ValidadeFinal { get; set; }
    public bool? Producao { get; set; }
}

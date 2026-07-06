using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using ERP.Fiscal.Abstractions;

namespace ERP.Fiscal.PlugNotas.Parsers;

/// <summary>Parser da resposta <c>GET /nfe/destinada</c>.</summary>
public static class PlugNotasNfeDestinadaRespostaParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static NfeDestinadaConsultaResult Parse(int httpStatusCode, string? rawBody)
    {
        if (httpStatusCode is < 200 or >= 300)
        {
            return new NfeDestinadaConsultaResult
            {
                Sucesso = false,
                HttpStatusCode = httpStatusCode,
                Mensagem = string.IsNullOrWhiteSpace(rawBody) ? "Erro na consulta de NF-e destinadas." : rawBody,
                RawResponse = rawBody
            };
        }

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return new NfeDestinadaConsultaResult
            {
                Sucesso = true,
                HttpStatusCode = httpStatusCode,
                RawResponse = rawBody
            };
        }

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var itens = ParseItens(doc.RootElement);
            return new NfeDestinadaConsultaResult
            {
                Sucesso = true,
                HttpStatusCode = httpStatusCode,
                Itens = itens,
                RawResponse = rawBody
            };
        }
        catch (JsonException ex)
        {
            return new NfeDestinadaConsultaResult
            {
                Sucesso = false,
                HttpStatusCode = httpStatusCode,
                Mensagem = $"Resposta inválida da API PlugNotas (destinadas): {ex.Message}",
                RawResponse = rawBody
            };
        }
    }

    private static IReadOnlyList<NfeDestinadaItem> ParseItens(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return ParseItemArray(root);

        foreach (var propName in new[] { "data", "notas", "documentos", "itens" })
        {
            if (root.TryGetProperty(propName, out var arr) && arr.ValueKind == JsonValueKind.Array)
                return ParseItemArray(arr);
        }

        return Array.Empty<NfeDestinadaItem>();
    }

    private static IReadOnlyList<NfeDestinadaItem> ParseItemArray(JsonElement array)
    {
        var list = new List<NfeDestinadaItem>();
        foreach (var el in array.EnumerateArray())
        {
            var item = ParseItem(el);
            if (item != null)
                list.Add(item);
        }

        return list;
    }

    private static NfeDestinadaItem? ParseItem(JsonElement el)
    {
        var chave = GetString(el, "chave", "chaveAcesso", "chave_acesso");
        if (string.IsNullOrWhiteSpace(chave))
            return null;

        return new NfeDestinadaItem
        {
            ChaveAcesso = chave,
            CnpjEmitente = FiscalDigitsHelper.DigitsOnlyOrNull(GetString(el, "emitente", "cnpjEmitente", "cnpj_emitente", "cpfCnpjEmitente")),
            CnpjDestinatario = FiscalDigitsHelper.DigitsOnlyOrNull(GetString(el, "destinatario", "cnpjDestinatario", "cnpj_destinatario", "cpfCnpjDestinatario")),
            DataEmissao = ParseDate(GetString(el, "dataEmissao", "data_emissao", "emissao")),
            NomeResumo = GetString(el, "nome", "nomeResumo", "razaoSocial", "razao_social"),
            ResumoItensNcm = GetString(el, "resumoNcm", "resumo_ncm", "resumoItensNcm", "ncm")
        };
    }

    private static string? GetString(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (!el.TryGetProperty(name, out var prop))
                continue;

            if (prop.ValueKind == JsonValueKind.String)
            {
                var s = prop.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }
            else if (prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetRawText();
            }
            else if (prop.ValueKind == JsonValueKind.Object)
            {
                var nested = GetString(prop, "cpfCnpj", "cnpj", "nome", "razaoSocial");
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        return null;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
            return dt;

        if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.AssumeLocal, out dt))
            return dt;

        return null;
    }
}

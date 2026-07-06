using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using ERP.Fiscal.Abstractions;

namespace ERP.Fiscal.PlugNotas.Parsers;

/// <summary>
/// Extrai campos do JSON de resposta do POST <c>/nfe</c> PlugNotas.
/// Formato típico: <c>{ "documents": [{ "id": "...", "idIntegracao": "..." }], "protocol": "guid-lote" }</c>.
/// O <c>protocol</c> da raiz é protocolo de lote (GUID), não protocolo SEFAZ.
/// </summary>
public static class PlugNotasNfeEmissaoRespostaParser
{
    public static PlugNotasNfeEmissaoCamposParseados? TryParse(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object && root.ValueKind != JsonValueKind.Array)
                return null;

            return new PlugNotasNfeEmissaoCamposParseados
            {
                IdDocumentoProvedor = TryReadIdDocumentoPlugNotas(root),
                ProtocoloAutorizacaoSefaz = TryReadProtocoloSefaz(root),
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Id MongoDB do documento no PlugNotas (~24 hex), usado em GET <c>/nfe/{id}/...</c>.</summary>
    public static bool LooksLikeIdDocumentoPlugNotas(string? value) =>
        NfeProvedorIdentificadorRules.LooksLikeIdDocumentoPlugNotas(value);

    /// <summary>Protocolo de autorização SEFAZ — numérico, tipicamente 15 dígitos.</summary>
    public static bool LooksLikeProtocoloSefaz(string? value) =>
        NfeProvedorIdentificadorRules.LooksLikeProtocoloSefaz(value);

    private static string? TryReadIdDocumentoPlugNotas(JsonElement root)
    {
        foreach (var obj in EnumerateCandidateObjects(root))
        {
            if (!TryGetPropertyIgnoreCase(obj, "id", out var idEl))
                continue;
            var id = ReadStringOrNumber(idEl);
            if (NfeProvedorIdentificadorRules.LooksLikeIdDocumentoPlugNotas(id))
                return id;
        }

        return null;
    }

    private static string? TryReadProtocoloSefaz(JsonElement root)
    {
        foreach (var obj in EnumerateCandidateObjects(root))
        {
            if (TryReadProtocoloSefazFromObject(obj, out var protocolo))
                return protocolo;
        }

        return null;
    }

    private static IEnumerable<JsonElement> EnumerateCandidateObjects(JsonElement root)
    {
        static IEnumerable<JsonElement> FromObject(JsonElement obj)
        {
            if (obj.ValueKind == JsonValueKind.Object)
                yield return obj;

            if (obj.ValueKind != JsonValueKind.Object)
                yield break;

            if (TryGetPropertyIgnoreCase(obj, "documents", out var documents) &&
                documents.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in documents.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                        yield return item;
                }
            }

            if (TryGetPropertyIgnoreCase(obj, "data", out var data))
            {
                if (data.ValueKind == JsonValueKind.Object)
                {
                    foreach (var nested in FromObject(data))
                        yield return nested;
                }
                else if (data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        foreach (var nested in FromObject(item))
                            yield return nested;
                    }
                }
            }
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                foreach (var obj in FromObject(item))
                    yield return obj;
            }

            yield break;
        }

        foreach (var obj in FromObject(root))
            yield return obj;
    }

    private static bool TryReadProtocoloSefazFromObject(JsonElement obj, out string? protocolo)
    {
        protocolo = null;
        if (obj.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var name in new[] { "protocolo", "nProt", "numeroProtocolo", "nrProtocolo" })
        {
            if (!TryGetPropertyIgnoreCase(obj, name, out var el))
                continue;
            var s = ReadStringOrNumber(el);
            if (string.IsNullOrWhiteSpace(s))
                continue;
            if (NfeProvedorIdentificadorRules.LooksLikeProtocoloSefaz(s))
            {
                protocolo = s.Trim();
                return true;
            }
        }

        return false;
    }

    private static string? ReadStringOrNumber(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.String => el.GetString()?.Trim(),
            JsonValueKind.Number => el.GetRawText(),
            _ => null,
        };

    private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
    {
        value = default;
        if (obj.ValueKind != JsonValueKind.Object)
            return false;
        foreach (var p in obj.EnumerateObject())
        {
            if (!string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;
            value = p.Value;
            return true;
        }

        return false;
    }

    /// <summary>Indica resposta de POST /nfe rejeitada porque o documento já foi enviado (mesmo idIntegração ou chave).</summary>
    public static bool IndicaDocumentoJaExistenteNoProvedor(bool sucesso, string? rawBody, string? errorMessage)
    {
        if (sucesso)
            return false;

        if (TryIndicaDocumentoDuplicadoNoJson(rawBody))
            return true;

        if (TextoIndicaDocumentoDuplicado(errorMessage))
            return true;

        return TextoIndicaDocumentoDuplicado(rawBody);
    }

    private static bool TryIndicaDocumentoDuplicadoNoJson(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            return ElementIndicaDocumentoDuplicado(doc.RootElement);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool ElementIndicaDocumentoDuplicado(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return TextoIndicaDocumentoDuplicado(element.GetString());

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in element.EnumerateObject())
            {
                if (ElementIndicaDocumentoDuplicado(p.Value))
                    return true;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (ElementIndicaDocumentoDuplicado(item))
                    return true;
            }
        }

        return false;
    }

    private static bool TextoIndicaDocumentoDuplicado(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return false;

        if (texto.Contains("já existe uma nfe", StringComparison.OrdinalIgnoreCase)
            || texto.Contains("ja existe uma nfe", StringComparison.OrdinalIgnoreCase))
            return true;

        // "already exists" só em contexto NF-e — evita falso positivo em outras mensagens do provedor.
        return texto.Contains("already exists", StringComparison.OrdinalIgnoreCase)
               && (texto.Contains("nfe", StringComparison.OrdinalIgnoreCase)
                   || texto.Contains("nf-e", StringComparison.OrdinalIgnoreCase)
                   || texto.Contains("nota fiscal", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Extrai <c>cStat</c> SEFAZ de respostas de erro do POST /nfe quando presente.</summary>
    public static string? TryExtrairCodigoRejeicao(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            return TryExtrairCodigoRejeicao(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryExtrairCodigoRejeicao(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (TryGetIntProperty(element, "cStat", "cstat", out var cStat))
                return cStat.ToString(CultureInfo.InvariantCulture);

            foreach (var p in element.EnumerateObject())
            {
                var nested = TryExtrairCodigoRejeicao(p.Value);
                if (nested != null)
                    return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = TryExtrairCodigoRejeicao(item);
                if (nested != null)
                    return nested;
            }
        }

        return null;
    }

    private static bool TryGetIntProperty(JsonElement obj, string a, string b, out int value)
    {
        value = 0;
        foreach (var n in new[] { a, b })
        {
            if (!TryGetPropertyIgnoreCase(obj, n, out var el))
                continue;
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out value))
                return true;
            if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out value))
                return true;
        }

        return false;
    }
}

public sealed class PlugNotasNfeEmissaoCamposParseados
{
    public string? IdDocumentoProvedor { get; init; }

    public string? ProtocoloAutorizacaoSefaz { get; init; }
}

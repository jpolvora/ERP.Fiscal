using System;
using System.Text.Json;
using ERP.Fiscal.Abstractions;

namespace ERP.Fiscal.PlugNotas.Parsers;

/// <summary>Extrai campos úteis do JSON de consulta NF-e PlugNotas (formato pode ser array na raiz, <c>data</c> objeto ou array).</summary>
public static class PlugNotasNfeConsultaRespostaParser
{
    public static PlugNotasNfeConsultaCamposParseados? TryParse(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                return Normalize(ParseObject(root[0]));

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            if (TryGetPropertyIgnoreCase(root, "data", out var data))
            {
                if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                    return Normalize(ParseObject(data[0]));
                if (data.ValueKind == JsonValueKind.Object)
                    return Normalize(ParseObject(data));
            }

            return Normalize(ParseObject(root));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static PlugNotasNfeConsultaCamposParseados ParseObject(JsonElement obj)
    {
        var dto = new PlugNotasNfeConsultaCamposParseados();
        if (obj.ValueKind != JsonValueKind.Object)
            return dto;

        if (TryGetStringIgnoreCase(obj, "status", out var st))
            dto.StatusPlugNotas = st;
        else if (TryGetStringIgnoreCase(obj, "situacao", out var situacao))
            dto.StatusPlugNotas = situacao;
        dto.ChaveAcesso = TryGetStringAny(obj, "chave", "chaveAcesso", "chave_acesso", "codigoVerificacao", "codigo_verificacao");
        var idBruto = TryGetStringAny(obj, "id");
        if (NfeProvedorIdentificadorRules.LooksLikeIdDocumentoPlugNotas(idBruto))
            dto.IdDocumentoProvedor = idBruto;
        dto.ProtocoloAutorizacao = TryGetStringAny(obj, "protocolo", "nProt", "numeroProtocolo");
        dto.MensagemSefaz = TryGetStringAny(obj, "mensagem", "message", "xMotivo");
        dto.NumeroNota = TryGetStringAny(obj, "numero", "nNF", "numeroNfse", "numeroNFe");
        dto.Serie = TryGetStringAny(obj, "serie", "serieNFe");

        if (TryGetIntProperty(obj, "cStat", "cstat", out var cStat))
            dto.CodigoStatusSefaz = cStat;

        return dto;
    }

    private static PlugNotasNfeConsultaCamposParseados Normalize(PlugNotasNfeConsultaCamposParseados dto)
    {
        var cStat = dto.CodigoStatusSefaz;
        var status = dto.StatusPlugNotas?.Trim();
        var msg = dto.MensagemSefaz ?? string.Empty;

        if (cStat == 100)
            dto.SituacaoResumida = NfeSituacao.Autorizada;
        else if (cStat is 110 or 205 or 301 or 302 or 401)
            dto.SituacaoResumida = NfeSituacao.Rejeitada;
        else if (cStat is 135 or 136 or 155)
            dto.SituacaoResumida = NfeSituacao.Cancelada;
        else if (!string.IsNullOrEmpty(status) &&
                 status.Equals("CONCLUIDO", StringComparison.OrdinalIgnoreCase))
            dto.SituacaoResumida = NfeSituacao.Autorizada;
        else if (msg.Contains("Autorizado", StringComparison.OrdinalIgnoreCase) &&
                 msg.Contains("NF", StringComparison.OrdinalIgnoreCase))
            dto.SituacaoResumida = NfeSituacao.Autorizada;
        else if (!string.IsNullOrEmpty(status) &&
                 (status.Contains("PROC", StringComparison.OrdinalIgnoreCase) ||
                  status.Contains("AGUARD", StringComparison.OrdinalIgnoreCase)))
            dto.SituacaoResumida = NfeSituacao.Processando;
        else if (!string.IsNullOrEmpty(status) &&
                 status.Contains("CANCEL", StringComparison.OrdinalIgnoreCase))
            dto.SituacaoResumida = NfeSituacao.Cancelada;
        else if (!string.IsNullOrEmpty(status) &&
                 status.Contains("REJEIT", StringComparison.OrdinalIgnoreCase))
            dto.SituacaoResumida = NfeSituacao.Rejeitada;
        else if (msg.Contains("Rejeicao", StringComparison.OrdinalIgnoreCase) ||
                 msg.Contains("Rejeição", StringComparison.OrdinalIgnoreCase))
            dto.SituacaoResumida = NfeSituacao.Rejeitada;
        else if (cStat is >= 200 and not (135 or 136 or 155))
            dto.SituacaoResumida = NfeSituacao.Rejeitada;
        else
            dto.SituacaoResumida = NfeSituacao.Desconhecido;

        return dto;
    }

    private static string? TryGetStringAny(JsonElement obj, params string[] names)
    {
        foreach (var n in names)
        {
            if (TryGetStringIgnoreCase(obj, n, out var s) && !string.IsNullOrWhiteSpace(s))
                return s.Trim();
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

    private static bool TryGetStringIgnoreCase(JsonElement obj, string name, out string? value)
    {
        value = null;
        if (!TryGetPropertyIgnoreCase(obj, name, out var el))
            return false;
        if (el.ValueKind == JsonValueKind.String)
        {
            value = el.GetString();
            return !string.IsNullOrWhiteSpace(value);
        }

        if (el.ValueKind == JsonValueKind.Number)
        {
            value = el.GetRawText();
            return true;
        }

        return false;
    }

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
}

/// <summary>Campos parseados da consulta NF-e, antes de virar <see cref="NfeConsultaResult"/> na fronteira do provider.</summary>
public sealed class PlugNotasNfeConsultaCamposParseados
{
    public string? StatusPlugNotas { get; set; }
    public string? ChaveAcesso { get; set; }
    public string? IdDocumentoProvedor { get; set; }
    public string? ProtocoloAutorizacao { get; set; }
    public string? MensagemSefaz { get; set; }
    public string? NumeroNota { get; set; }
    public string? Serie { get; set; }
    public int? CodigoStatusSefaz { get; set; }
    public string? SituacaoResumida { get; set; }
}

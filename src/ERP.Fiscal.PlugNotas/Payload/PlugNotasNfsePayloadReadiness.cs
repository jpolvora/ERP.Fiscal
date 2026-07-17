using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Contracts;

namespace ERP.Fiscal.PlugNotas.Payload;

/// <summary>
/// Validação mínima do payload NFS-e antes de liberar transmissão.
/// Opera apenas sobre contratos PlugNotas — sem vocabulário de domínio do ERP.
/// </summary>
public static class PlugNotasNfsePayloadReadiness
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static PlugNotasNfseDocumentPayload? TryParseDocumentoFromPostArray(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return null;

        try
        {
            var list = JsonSerializer.Deserialize<List<PlugNotasNfseDocumentPayload>>(payloadJson, JsonOptions);
            return list?.FirstOrDefault();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static (bool PodeTransmitir, IReadOnlyList<string> Pendencias) Avaliar(PlugNotasNfseDocumentPayload? doc)
    {
        var p = new List<string>();
        if (doc == null)
        {
            p.Add("Documento NFS-e não foi montado.");
            return (false, p);
        }

        if (string.IsNullOrWhiteSpace(doc.IdIntegracao))
            p.Add("Identificador de integração (idIntegracao) é obrigatório.");

        if (string.IsNullOrWhiteSpace(doc.Competencia))
            p.Add("Competência da NFS-e é obrigatória (yyyy-MM-dd).");

        var prestadorCnpj = FiscalDigitsHelper.DigitsOnly(doc.Prestador?.CpfCnpj);
        if (prestadorCnpj.Length != 14 && prestadorCnpj.Length != 11)
            p.Add("CNPJ ou CPF do prestador inválido ou ausente.");

        if (string.IsNullOrWhiteSpace(doc.Prestador?.InscricaoMunicipal))
            p.Add("Inscrição municipal do prestador é obrigatória.");

        if (doc.Servico == null || doc.Servico.Count == 0)
            p.Add("Pelo menos um serviço na NFS-e é obrigatório.");

        for (var i = 0; i < (doc.Servico?.Count ?? 0); i++)
        {
            var svc = doc.Servico![i];
            var prefixo = $"Serviço {i + 1}:";

            if (string.IsNullOrWhiteSpace(svc.Codigo))
                p.Add($"{prefixo} código do serviço (LC116) é obrigatório.");

            var ibge = FiscalDigitsHelper.DigitsOnly(svc.CodigoCidadeIncidencia);
            if (ibge.Length != 7)
                p.Add($"{prefixo} código IBGE da cidade de incidência é obrigatório (7 dígitos).");

            if (string.IsNullOrWhiteSpace(svc.Discriminacao))
                p.Add($"{prefixo} discriminação do serviço é obrigatória.");

            if (svc.Iss == null)
            {
                p.Add($"{prefixo} bloco ISS é obrigatório.");
            }
            else
            {
                if (svc.Iss.Exigibilidade is null or < 1 or > 7)
                    p.Add($"{prefixo} exigibilidade ISS deve ser 1–7 (schema PlugNotas).");
                if (svc.Iss.Aliquota is null)
                    p.Add($"{prefixo} alíquota ISS é obrigatória.");
            }

            if (svc.Valor == null)
            {
                p.Add($"{prefixo} bloco valor é obrigatório.");
            }
            else if (svc.Valor.Servico is null or <= 0)
            {
                p.Add($"{prefixo} valor do serviço deve ser maior que zero.");
            }
        }

        return (p.Count == 0, p);
    }
}

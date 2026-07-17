using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Contracts;

namespace ERP.Fiscal.PlugNotas.Payload;

/// <summary>
/// Validação mínima do payload NF-e antes de liberar transmissão.
/// Opera apenas sobre contratos PlugNotas — sem vocabulário de domínio do ERP.
/// </summary>
public static class PlugNotasNfePayloadReadiness
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static PlugNotasNfeDocumentPayload? TryParseDocumentoFromPostArray(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return null;

        try
        {
            var list = JsonSerializer.Deserialize<List<PlugNotasNfeDocumentPayload>>(payloadJson, JsonOptions);
            return list?.FirstOrDefault();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static (bool PodeTransmitir, IReadOnlyList<string> Pendencias) Avaliar(PlugNotasNfeDocumentPayload? doc)
    {
        var p = new List<string>();
        if (doc == null)
        {
            p.Add("Documento fiscal não foi montado.");
            return (false, p);
        }

        if (PlugNotasNfeNaturezaCamposHelper.FinalidadeInvalida(doc.Finalidade))
            p.Add("Finalidade da NF-e deve estar entre 1 (normal) e 6 (débito).");

        if (PlugNotasNfeNaturezaCamposHelper.CombinacaoInvalidaPlugNotas(doc.Presencial, doc.Finalidade))
        {
            p.Add(
                "Indicador de presença 'Não se aplica' (0) só é permitido pela PlugNotas quando a finalidade é complementar (2) ou ajuste (3).");
        }

        if (string.IsNullOrWhiteSpace(doc.IdIntegracao))
            p.Add("Identificador de integração (idIntegracao) é obrigatório.");

        if (string.IsNullOrWhiteSpace(doc.DataEmissao))
            p.Add("Data de emissão é obrigatória.");

        var emitCnpj = FiscalDigitsHelper.DigitsOnly(doc.Emitente?.CpfCnpj);
        if (emitCnpj.Length != 14 && emitCnpj.Length != 11)
            p.Add("CNPJ ou CPF do emitente inválido ou ausente.");

        if (emitCnpj.Length == 14 && string.IsNullOrWhiteSpace(doc.Emitente?.InscricaoEstadual))
            p.Add("Inscrição estadual do emitente é obrigatória (cadastro do emissor).");

        var destCnpj = FiscalDigitsHelper.DigitsOnly(doc.Destinatario?.CpfCnpj);
        if (destCnpj.Length != 14 && destCnpj.Length != 11)
            p.Add("CNPJ ou CPF do destinatário inválido ou ausente.");

        if (string.IsNullOrWhiteSpace(doc.Destinatario?.RazaoSocial))
            p.Add("Razão social do destinatário é obrigatória.");

        var ieDest = doc.Destinatario?.InscricaoEstadual?.Trim();
        var indIeDest = doc.Destinatario?.IndicadorInscricaoEstadual;
        if (!indIeDest.HasValue)
        {
            p.Add("Indicador da inscrição estadual do destinatário é obrigatório.");
        }
        else
        {
            switch (indIeDest.Value)
            {
                case 1:
                    if (string.IsNullOrWhiteSpace(ieDest) || string.Equals(ieDest, "ISENTO", StringComparison.OrdinalIgnoreCase))
                        p.Add("Indicador IE do destinatário = 1 exige inscrição estadual preenchida e diferente de ISENTO.");
                    break;
                case 2:
                    if (!string.Equals(ieDest, "ISENTO", StringComparison.OrdinalIgnoreCase))
                        p.Add("Indicador IE do destinatário = 2 exige inscrição estadual com literal ISENTO.");
                    break;
                case 9:
                    if (!string.IsNullOrWhiteSpace(ieDest))
                        p.Add("Indicador IE do destinatário = 9 exige inscrição estadual não informada.");
                    break;
                default:
                    p.Add("Indicador da inscrição estadual do destinatário deve ser 1, 2 ou 9.");
                    break;
            }
        }

        var end = doc.Destinatario?.Endereco;
        if (end == null)
        {
            p.Add("Endereço do destinatário é obrigatório.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(end.Logradouro))
                p.Add("Logradouro do destinatário é obrigatório.");
            if (string.IsNullOrWhiteSpace(end.Numero))
                p.Add("Número do endereço do destinatário é obrigatório.");
            if (string.IsNullOrWhiteSpace(end.Bairro))
                p.Add("Bairro do destinatário é obrigatório.");
            var cep = FiscalDigitsHelper.DigitsOnly(end.Cep);
            if (cep.Length != 8)
                p.Add("CEP do destinatário deve ter 8 dígitos.");
            var ibge = FiscalDigitsHelper.DigitsOnly(end.CodigoCidade);
            if (ibge.Length != 7)
                p.Add("Código IBGE do município do destinatário é obrigatório (7 dígitos).");
            if (string.IsNullOrWhiteSpace(end.Estado) || end.Estado.Trim().Length != 2)
                p.Add("UF do destinatário é obrigatória (2 letras).");
        }

        if (doc.Itens == null || doc.Itens.Count == 0)
            p.Add("Pelo menos um item na nota é obrigatório.");

        for (var i = 0; i < (doc.Itens?.Count ?? 0); i++)
        {
            var it = doc.Itens![i];
            var prefixo = $"Item {i + 1}:";
            if (string.IsNullOrWhiteSpace(it.Descricao))
                p.Add($"{prefixo} descrição é obrigatória.");
            var ncm = FiscalDigitsHelper.DigitsOnly(it.Ncm);
            if (ncm.Length != 8)
                p.Add($"{prefixo} NCM com 8 dígitos é obrigatório.");
            var cfop = FiscalDigitsHelper.DigitsOnly(it.Cfop);
            if (cfop.Length != 4)
                p.Add($"{prefixo} CFOP válido (4 dígitos) é obrigatório.");
            if (it.Quantidade?.Comercial is not > 0)
                p.Add($"{prefixo} quantidade comercial deve ser maior que zero.");
            var unidadeComercial = it.Unidade?.Comercial?.Trim();
            if (string.IsNullOrWhiteSpace(unidadeComercial) || unidadeComercial.Length > 6)
                p.Add($"{prefixo} unidade comercial é obrigatória (1 a 6 caracteres).");
            if (it.ValorUnitario == null || it.ValorUnitario.Comercial <= 0)
                p.Add($"{prefixo} valor unitário comercial é obrigatório.");
            if (it.Valor <= 0)
                p.Add($"{prefixo} valor total do item é obrigatório.");
            if (it.Tributos == null)
            {
                p.Add($"{prefixo} tributos (ICMS, PIS e COFINS) são obrigatórios.");
                continue;
            }

            if (it.Tributos.Icms == null || string.IsNullOrWhiteSpace(it.Tributos.Icms.Cst))
                p.Add($"{prefixo} ICMS (CST) é obrigatório.");
            if (it.Tributos.Pis == null || string.IsNullOrWhiteSpace(it.Tributos.Pis.Cst))
                p.Add($"{prefixo} PIS (CST) é obrigatório.");
            if (it.Tributos.Cofins == null || string.IsNullOrWhiteSpace(it.Tributos.Cofins.Cst))
                p.Add($"{prefixo} COFINS (CST) é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(doc.Serie))
            p.Add("Série da NF-e é obrigatória (configuração fiscal do emissor).");

        if (!doc.Numero.HasValue || doc.Numero.Value <= 0)
            p.Add("Número da NF-e é obrigatório (configuração fiscal do emissor).");

        var veiculo = doc.Transporte?.Veiculo;
        if (!string.IsNullOrWhiteSpace(veiculo?.Placa))
        {
            var ufV = NormalizeUf(veiculo!.Uf);
            if (ufV == null)
                p.Add("UF do veículo é obrigatória quando a placa é informada.");
        }

        return (p.Count == 0, p);
    }

    private static string? NormalizeUf(string? uf)
    {
        if (string.IsNullOrWhiteSpace(uf))
            return null;
        var t = uf.Trim();
        return t.Length == 2 ? t.ToUpperInvariant() : null;
    }
}

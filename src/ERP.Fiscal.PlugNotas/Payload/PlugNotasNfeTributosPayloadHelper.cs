using System;
using ERP.Fiscal.PlugNotas.Contracts;

namespace ERP.Fiscal.PlugNotas.Payload;

/// <summary>
/// Monta o bloco <c>itens[].tributos</c> do POST <c>/nfe</c> PlugNotas a partir de CST/alíquotas neutros
/// (sem dependência de entidades de domínio do ERP consumidor).
/// </summary>
public static class PlugNotasNfeTributosPayloadHelper
{
    /// <summary>Parâmetros tributários mínimos para Simples Nacional (CSOSN/CST + bases zeradas exigidas pela API).</summary>
    public sealed record SimplesNacionalEntrada(
        string IcmsCst,
        int IcmsModalidadeDeterminacao,
        string PisCst,
        string CofinsCst,
        string OrigemMercadoria = "0");

    /// <summary>Parâmetros tributários para regime normal (ICMS/PIS/COFINS com bases e alíquotas).</summary>
    public sealed record RegimeNormalEntrada(
        string IcmsCst,
        int IcmsModalidadeDeterminacao,
        decimal IcmsAliquotaPercentual,
        decimal IcmsBaseCalculoPercentual,
        string PisCst,
        decimal PisAliquotaPercentual,
        decimal PisBaseCalculoPercentual,
        string CofinsCst,
        decimal CofinsAliquotaPercentual,
        decimal CofinsBaseCalculoPercentual,
        decimal ValorTotalItem,
        decimal QuantidadeItem,
        string OrigemMercadoria = "0");

    /// <summary>
    /// Tributos para optante Simples Nacional: envia CST/CSOSN e preenche
    /// <c>baseCalculo</c>, <c>aliquota</c> e <c>valor</c> zerados exigidos pela validação PlugNotas.
    /// </summary>
    public static PlugNotasNfeTributosItemPayload BuildSimplesNacional(SimplesNacionalEntrada entrada) =>
        new()
        {
            Icms = new PlugNotasNfeTributoIcmsPayload
            {
                Origem = entrada.OrigemMercadoria,
                Cst = entrada.IcmsCst,
                BaseCalculo = new PlugNotasNfeBaseCalculoIcmsPayload
                {
                    ModalidadeDeterminacao = entrada.IcmsModalidadeDeterminacao,
                    Valor = 0
                },
                Aliquota = 0,
                Valor = 0
            },
            Pis = new PlugNotasNfeTributoPisPayload
            {
                Cst = entrada.PisCst,
                BaseCalculo = new PlugNotasNfeBaseCalculoPisPayload { Valor = 0, Quantidade = 0 },
                Aliquota = 0,
                Valor = 0
            },
            Cofins = new PlugNotasNfeTributoCofinsPayload
            {
                Cst = entrada.CofinsCst,
                BaseCalculo = new PlugNotasNfeBaseCalculoCofinsPayload { Valor = 0 },
                Aliquota = 0,
                Valor = 0
            }
        };

    /// <summary>Tributos para regime normal com bases e valores calculados a partir do valor do item.</summary>
    public static PlugNotasNfeTributosItemPayload BuildRegimeNormal(RegimeNormalEntrada entrada)
    {
        var baseIcms = RoundBaseCalculoFromPercentual(entrada.ValorTotalItem, entrada.IcmsBaseCalculoPercentual);
        var basePis = RoundBaseCalculoFromPercentual(entrada.ValorTotalItem, entrada.PisBaseCalculoPercentual);
        var baseCofins = RoundBaseCalculoFromPercentual(entrada.ValorTotalItem, entrada.CofinsBaseCalculoPercentual);

        return new PlugNotasNfeTributosItemPayload
        {
            Icms = new PlugNotasNfeTributoIcmsPayload
            {
                Origem = entrada.OrigemMercadoria,
                Cst = entrada.IcmsCst,
                BaseCalculo = new PlugNotasNfeBaseCalculoIcmsPayload
                {
                    ModalidadeDeterminacao = entrada.IcmsModalidadeDeterminacao,
                    Valor = baseIcms
                },
                Aliquota = entrada.IcmsAliquotaPercentual,
                Valor = RoundValorTributo(baseIcms, entrada.IcmsAliquotaPercentual)
            },
            Pis = new PlugNotasNfeTributoPisPayload
            {
                Cst = entrada.PisCst,
                BaseCalculo = new PlugNotasNfeBaseCalculoPisPayload
                {
                    Valor = basePis,
                    Quantidade = entrada.QuantidadeItem
                },
                Aliquota = entrada.PisAliquotaPercentual,
                Valor = RoundValorTributo(basePis, entrada.PisAliquotaPercentual)
            },
            Cofins = new PlugNotasNfeTributoCofinsPayload
            {
                Cst = entrada.CofinsCst,
                BaseCalculo = new PlugNotasNfeBaseCalculoCofinsPayload { Valor = baseCofins },
                Aliquota = entrada.CofinsAliquotaPercentual,
                Valor = RoundValorTributo(baseCofins, entrada.CofinsAliquotaPercentual)
            }
        };
    }

    internal static decimal RoundBaseCalculoFromPercentual(decimal valorTotal, decimal baseCalculoPercentual) =>
        Math.Round(valorTotal * baseCalculoPercentual / 100m, 2, MidpointRounding.AwayFromZero);

    internal static decimal RoundValorTributo(decimal baseCalculo, decimal aliquotaPercentual) =>
        Math.Round(baseCalculo * aliquotaPercentual / 100m, 2, MidpointRounding.AwayFromZero);
}

using System;
using System.Collections.Generic;
using ERP.Fiscal.PlugNotas.Contracts;

namespace ERP.Fiscal.PlugNotas.Payload;

/// <summary>
/// Normaliza <c>itens[].tributos.icms</c> conforme o CST/CSOSN aceito pela validação de esquema PlugNotas
/// (campos de outros grupos ICMS — ex.: <c>valor</c>/<c>aliquota</c> em CST 40 — geram rejeição JSON).
/// </summary>
public static class PlugNotasNfeIcmsCamposPorCstHelper
{
    /// <summary>
    /// CST/CSOSN em que <c>aliquota</c> e <c>valor</c> não pertencem ao grupo ICMS (omitir no POST).
    /// </summary>
    private static readonly HashSet<string> CstsSemValorAliquota = new(StringComparer.Ordinal)
    {
        "40",
        "41",
        "50",
        "103",
        "300",
        "400",
    };

    /// <summary>
    /// CST 60 usa grupo <c>substituicaoTributaria</c>; <c>baseCalculo</c>/<c>aliquota</c>/<c>valor</c> na raiz não são esperados.
    /// </summary>
    private static readonly HashSet<string> CstsSemBaseCalculoRaiz = new(StringComparer.Ordinal)
    {
        "60",
        "500",
    };

    public static void AplicarCamposPermitidos(PlugNotasNfeTributoIcmsPayload? icms)
    {
        if (icms == null || string.IsNullOrWhiteSpace(icms.Cst))
        {
            return;
        }

        var cst = icms.Cst.Trim();
        if (CstsSemValorAliquota.Contains(cst))
        {
            icms.Aliquota = null;
            icms.Valor = null;
        }

        if (CstsSemBaseCalculoRaiz.Contains(cst))
        {
            icms.BaseCalculo = null;
            icms.Aliquota = null;
            icms.Valor = null;
        }
    }
}

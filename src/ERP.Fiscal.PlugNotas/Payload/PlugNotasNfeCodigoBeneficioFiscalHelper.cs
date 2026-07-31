using System;
using System.Collections.Generic;

namespace ERP.Fiscal.PlugNotas.Payload;

/// <summary>Regras neutras de serialização do código de benefício fiscal no payload NF-e.</summary>
public static class PlugNotasNfeCodigoBeneficioFiscalHelper
{
    private static readonly HashSet<string> CstsComBeneficioFiscal = new(StringComparer.Ordinal)
    {
        "20", "30", "40", "41", "50", "51", "70", "90"
    };

    public static bool CstAceitaCodigoBeneficioFiscal(string? cst) =>
        !string.IsNullOrWhiteSpace(cst) && CstsComBeneficioFiscal.Contains(cst.Trim());

    public static string? ObterCodigoParaPayload(string? cst, string? codigo)
    {
        if (!CstAceitaCodigoBeneficioFiscal(cst))
            return null;

        return string.IsNullOrWhiteSpace(codigo) ? null : codigo.Trim();
    }
}

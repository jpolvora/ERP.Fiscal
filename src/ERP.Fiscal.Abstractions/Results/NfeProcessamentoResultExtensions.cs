using System;

namespace ERP.Fiscal.Abstractions;

/// <summary>Helpers neutros sobre <see cref="NfeProcessamentoResult"/>.</summary>
public static class NfeProcessamentoResultExtensions
{
    public static bool EstaProcessando(this NfeProcessamentoResult? result) =>
        result?.Sucesso == true
        && string.Equals(result.Situacao, NfeSituacao.Processando, StringComparison.OrdinalIgnoreCase);

    public static bool FoiAutorizada(this NfeProcessamentoResult? result) =>
        result?.Sucesso == true
        && string.Equals(result.Situacao, NfeSituacao.Autorizada, StringComparison.OrdinalIgnoreCase);

    public static bool FoiRejeitada(this NfeProcessamentoResult? result) =>
        result != null
        && (!result.Sucesso
            || string.Equals(result.Situacao, NfeSituacao.Rejeitada, StringComparison.OrdinalIgnoreCase));
}

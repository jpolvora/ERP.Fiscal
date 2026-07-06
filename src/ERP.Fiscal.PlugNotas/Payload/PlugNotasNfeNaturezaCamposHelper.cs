namespace ERP.Fiscal.PlugNotas.Payload;

/// <summary>Regras neutras de presencial/finalidade exigidas pela API PlugNotas.</summary>
public static class PlugNotasNfeNaturezaCamposHelper
{
    public const int PresencialNaoSeAplica = 0;
    public const int FinalidadeNfeComplementar = 2;
    public const int FinalidadeNfeAjuste = 3;

    public static bool CombinacaoInvalidaPlugNotas(string? presencial, int? finalidade)
    {
        if (!int.TryParse(presencial, out var pres) || pres != PresencialNaoSeAplica)
            return false;

        var fin = finalidade ?? 0;
        return fin is not FinalidadeNfeComplementar and not FinalidadeNfeAjuste;
    }

    public static bool FinalidadeInvalida(int? finalidade) =>
        !finalidade.HasValue || finalidade.Value <= 0;
}

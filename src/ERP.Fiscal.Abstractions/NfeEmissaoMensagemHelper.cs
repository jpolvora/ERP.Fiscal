using System.Text;

namespace ERP.Fiscal.Abstractions;

/// <summary>Monta mensagens de erro de transmissão NF-e sem depender de localização do ERP.</summary>
public static class NfeEmissaoMensagemHelper
{
    public static string MontarMensagemErro(
        string mensagemTransiente,
        string mensagemPermanente,
        string acaoTransiente,
        NfeEmissaoResult? result,
        string? detalheProvedorOuExcecao = null,
        int maxLength = 2000)
    {
        var detalhe = detalheProvedorOuExcecao
            ?? result?.Mensagem
            ?? result?.RawResponse;

        if (result?.IsTransientFailure == true)
        {
            var sb = new StringBuilder();
            sb.Append(mensagemTransiente);
            if (!string.IsNullOrWhiteSpace(detalhe))
                sb.Append(' ').Append(detalhe.Trim());
            if (!string.IsNullOrWhiteSpace(acaoTransiente))
                sb.Append(' ').Append(acaoTransiente.Trim());
            return Clip(sb.ToString(), maxLength);
        }

        var msg = mensagemPermanente;
        if (!string.IsNullOrWhiteSpace(detalhe))
            msg = $"{msg} {detalhe.Trim()}";
        return Clip(msg, maxLength);
    }

    public static bool EhFalhaTransiente(NfeEmissaoResult? result) =>
        result?.IsTransientFailure == true;

    public static bool EhFalhaTransiente(NfeProcessamentoResult? result) =>
        result?.IsTransientFailure == true;

    private static string Clip(string s, int maxLength) =>
        s.Length <= maxLength ? s : s[..maxLength];
}

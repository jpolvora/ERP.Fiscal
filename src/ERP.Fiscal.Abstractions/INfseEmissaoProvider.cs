using System.Threading;
using System.Threading.Tasks;

namespace ERP.Fiscal.Abstractions;

/// <summary>
/// Emissão, consulta, cancelamento e obtenção de artefatos (XML/PDF) de NFS-e junto ao provedor fiscal.
/// A implementação recebe o payload JSON já pronto — a montagem do payload a partir do domínio
/// (serviços, cliente, natureza de operação, tributos) é responsabilidade de cada ERP.
/// </summary>
public interface INfseEmissaoProvider
{
    Task<NfeEmissaoResult> EmitirAsync(string payloadJson, NfeAmbiente ambiente, CancellationToken cancellationToken = default);

    Task<NfeConsultaResult> ConsultarPorIdAsync(string idDocumentoProvedor, NfeAmbiente ambiente, CancellationToken cancellationToken = default);

    Task<NfeConsultaResult> ConsultarPorIdIntegracaoAsync(string cpfCnpjDigits, string idIntegracao, NfeAmbiente ambiente, CancellationToken cancellationToken = default);

    Task<NfeProcessamentoResult> EmitirCompletoAsync(
        string payloadJson,
        string? cpfCnpjDigits,
        string? idIntegracao,
        NfeAmbiente ambiente,
        CancellationToken cancellationToken = default);

    Task<NfeProcessamentoResult> ConsultarResultadoAsync(
        string identificador,
        NfeAmbiente ambiente,
        CancellationToken cancellationToken = default);

    Task<NfeCancelamentoResult> CancelarAsync(string idDocumentoProvedor, string justificativa, NfeAmbiente ambiente, CancellationToken cancellationToken = default);

    Task<NfeXmlResult> ObterXmlAsync(string idDocumentoProvedor, NfeAmbiente ambiente, CancellationToken cancellationToken = default);

    Task<NfePdfResult> ObterPdfAsync(string idDocumentoProvedor, NfeAmbiente ambiente, CancellationToken cancellationToken = default);
}

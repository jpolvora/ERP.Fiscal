using System.Threading;
using System.Threading.Tasks;

namespace ERP.Fiscal.Abstractions;

/// <summary>
/// Cadastro e integração de certificado digital e emissor/empresa junto ao provedor fiscal.
/// </summary>
public interface INfeIntegracaoProvider
{
    Task<NfeProviderResult> CadastrarCertificadoAsync(NfeCertificadoUpload data, NfeAmbiente ambiente, CancellationToken cancellationToken = default);

    Task<NfeProviderResult> ConsultarCertificadoAsync(string idCertificadoProvedor, NfeAmbiente ambiente, CancellationToken cancellationToken = default);

    Task<NfeProviderResult> CadastrarEmissorAsync(NfeEmissorData data, NfeAmbiente ambiente, CancellationToken cancellationToken = default);

    Task<NfeProviderResult> ConsultarEmissorAsync(string cpfCnpjDigits, NfeAmbiente ambiente, CancellationToken cancellationToken = default);

    Task<NfeProviderResult> SincronizarAmbienteEmissorAsync(string cpfCnpjDigits, bool producao, NfeAmbiente ambiente, CancellationToken cancellationToken = default);
}

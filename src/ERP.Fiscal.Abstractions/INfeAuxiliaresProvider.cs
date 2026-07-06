using System.Threading;
using System.Threading.Tasks;

namespace ERP.Fiscal.Abstractions;

/// <summary>Consultas auxiliares de cadastro (CNPJ/CEP) usadas no preenchimento de formulários de cliente/emissor.</summary>
public interface INfeAuxiliaresProvider
{
    Task<NfeConsultaCnpjResult> ConsultarCnpjAsync(string cnpjSomenteDigitos, CancellationToken cancellationToken = default);

    Task<NfeConsultaCepResult> ConsultarCepAsync(string cepSomenteDigitos, CancellationToken cancellationToken = default);
}

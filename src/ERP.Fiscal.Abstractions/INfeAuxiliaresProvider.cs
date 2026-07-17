using System.Threading;
using System.Threading.Tasks;

namespace ERP.Fiscal.Abstractions;

/// <summary>Consultas auxiliares de cadastro (CNPJ/CEP/municípios NFS-e) usadas no preenchimento de formulários.</summary>
public interface INfeAuxiliaresProvider
{
    Task<NfeConsultaCnpjResult> ConsultarCnpjAsync(string cnpjSomenteDigitos, CancellationToken cancellationToken = default);

    Task<NfeConsultaCepResult> ConsultarCepAsync(string cepSomenteDigitos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista municípios homologados PlugNotas (<c>GET /nfse/cidades</c>), com filtro opcional por nome/IBGE e UF.
    /// A implementação pode cachear a lista completa.
    /// </summary>
    Task<NfeConsultaMunicipiosResult> ConsultarMunicipiosAsync(
        string? filtroNomeOuIbge = null,
        string? uf = null,
        CancellationToken cancellationToken = default);

    /// <summary>Consulta um município pelo código IBGE (<c>GET /nfse/cidades/{codigoIbge}</c>).</summary>
    Task<NfeConsultaMunicipioResult> ConsultarMunicipioPorIbgeAsync(
        string codigoIbge,
        CancellationToken cancellationToken = default);
}

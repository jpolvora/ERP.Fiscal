using System.Threading;
using System.Threading.Tasks;

namespace ERP.Fiscal.Abstractions;

/// <summary>Consulta NF-e destinadas (DF-e) no provedor fiscal.</summary>
public interface INfeDestinadaProvider
{
    Task<NfeDestinadaConsultaResult> ListarDestinadasAsync(
        NfeDestinadaConsultaRequest request,
        NfeAmbiente ambiente,
        CancellationToken cancellationToken = default);
}

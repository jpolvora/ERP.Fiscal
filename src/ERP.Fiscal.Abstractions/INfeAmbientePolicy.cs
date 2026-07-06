using System.Threading.Tasks;

namespace ERP.Fiscal.Abstractions;

/// <summary>
/// Política de ambiente efetivo em runtime. Contrato fica na lib; a implementação
/// pertence a cada ERP porque depende de configuração local (<c>appsettings</c>, ABP Settings).
/// </summary>
public interface INfeAmbientePolicy
{
    /// <summary>
    /// Resolve o ambiente efetivo a partir do ambiente intencionado (configurado no cadastro do emissor).
    /// Tipicamente força <see cref="NfeAmbiente.Sandbox"/> quando uma flag local (ex.: <c>OnlySandbox</c>) está ativa.
    /// </summary>
    Task<NfeAmbiente> GetAmbienteEfetivoAsync(NfeAmbiente ambienteIntencionado);

    Task<bool> IsOnlySandboxAsync();
}

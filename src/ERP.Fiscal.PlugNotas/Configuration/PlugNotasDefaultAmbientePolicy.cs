using System.Threading.Tasks;
using ERP.Fiscal.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace ERP.Fiscal.PlugNotas.Configuration;

/// <summary>
/// Implementação padrão de <see cref="INfeAmbientePolicy"/> lendo <see cref="PlugNotasOptions.OnlySandbox"/>.
/// ERPs podem registrar diretamente ou compor com políticas locais de UI/domínio.
/// </summary>
public class PlugNotasDefaultAmbientePolicy : INfeAmbientePolicy, ITransientDependency
{
    private readonly IOptions<PlugNotasOptions> _plugNotasOptions;

    public PlugNotasDefaultAmbientePolicy(IOptions<PlugNotasOptions> plugNotasOptions)
    {
        _plugNotasOptions = plugNotasOptions;
    }

    public Task<bool> IsOnlySandboxAsync() =>
        Task.FromResult(_plugNotasOptions.Value.OnlySandbox);

    public async Task<NfeAmbiente> GetAmbienteEfetivoAsync(NfeAmbiente ambienteIntencionado)
    {
        if (await IsOnlySandboxAsync())
            return NfeAmbiente.Sandbox;

        return ambienteIntencionado;
    }
}

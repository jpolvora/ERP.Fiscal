using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Payload;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Configuration;

public class PlugNotasDefaultAmbientePolicyTests
{
    [Fact]
    public async Task GetAmbienteEfetivoAsync_deve_forcar_sandbox_quando_only_sandbox()
    {
        var policy = new PlugNotasDefaultAmbientePolicy(Options.Create(new PlugNotasOptions { OnlySandbox = true }));

        var ambiente = await policy.GetAmbienteEfetivoAsync(NfeAmbiente.Producao);

        ambiente.ShouldBe(NfeAmbiente.Sandbox);
    }

    [Fact]
    public async Task GetAmbienteEfetivoAsync_deve_preservar_intencao_quando_only_sandbox_false()
    {
        var policy = new PlugNotasDefaultAmbientePolicy(Options.Create(new PlugNotasOptions { OnlySandbox = false }));

        var ambiente = await policy.GetAmbienteEfetivoAsync(NfeAmbiente.Homologacao);

        ambiente.ShouldBe(NfeAmbiente.Homologacao);
    }
}

using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Configuration;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Configuration;

public class PlugNotasBaseUrlResolverTests
{
    [Fact]
    public void Resolve_deve_usar_url_sandbox_para_ambiente_sandbox()
    {
        PlugNotasBaseUrlResolver.Resolve(NfeAmbiente.Sandbox).ShouldBe(PlugNotasAmbienteConstants.SandboxBaseUrl);
    }

    [Theory]
    [InlineData(NfeAmbiente.Homologacao)]
    [InlineData(NfeAmbiente.Producao)]
    public void Resolve_deve_usar_url_oficial_para_homologacao_e_producao(NfeAmbiente ambiente)
    {
        PlugNotasBaseUrlResolver.Resolve(ambiente).ShouldBe(PlugNotasAmbienteConstants.ApiOficialBaseUrl);
    }
}

public class PlugNotasApiKeyResolverTests
{
    [Fact]
    public void Resolve_deve_usar_chave_publica_quando_sandbox_sem_chave_configurada()
    {
        var options = Options.Create(new PlugNotasOptions { SandboxApiKey = "" });

        var key = PlugNotasApiKeyResolver.Resolve(options, NfeAmbiente.Sandbox);

        key.ShouldBe(PlugNotasAmbienteConstants.PublicSandboxApiKey);
    }

    [Fact]
    public void Resolve_deve_usar_chave_sandbox_configurada_quando_presente()
    {
        var options = Options.Create(new PlugNotasOptions { SandboxApiKey = "minha-chave-sandbox" });

        PlugNotasApiKeyResolver.Resolve(options, NfeAmbiente.Sandbox).ShouldBe("minha-chave-sandbox");
    }

    [Fact]
    public void Resolve_deve_lancar_quando_producao_sem_chave_configurada()
    {
        var options = Options.Create(new PlugNotasOptions { ProductionApiKey = "" });

        Should.Throw<PlugNotasConfigurationException>(() => PlugNotasApiKeyResolver.Resolve(options, NfeAmbiente.Producao));
    }

    [Fact]
    public void Resolve_deve_usar_chave_producao_configurada_para_homologacao()
    {
        var options = Options.Create(new PlugNotasOptions { ProductionApiKey = "chave-producao" });

        PlugNotasApiKeyResolver.Resolve(options, NfeAmbiente.Homologacao).ShouldBe("chave-producao");
    }
}

using System.Net;
using System.Threading.Tasks;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Providers;

public class PlugNotasAuxiliaresProviderTests
{
    private static (PlugNotasAuxiliaresProvider Provider, FakeHttpMessageHandler Handler) CreateProvider(PlugNotasOptions? options = null)
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = FakeHttpMessageHandler.CreateClient(handler);
        var opts = Options.Create(options ?? new PlugNotasOptions());
        return (new PlugNotasAuxiliaresProvider(httpClient, NullLogger<PlugNotasAuxiliaresProvider>.Instance, opts), handler);
    }

    [Fact]
    public async Task ConsultarCnpjAsync_deve_mapear_dados_de_endereco()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """
            { "razao_social": "Empresa Teste LTDA", "nome": "Empresa Teste", "endereco": { "logradouro": "Rua A", "municipio": "São Paulo", "uf": "SP", "cep": "01000000" } }
            """);

        var result = await provider.ConsultarCnpjAsync("12345678000199");

        result.Sucesso.ShouldBeTrue();
        result.RazaoSocial.ShouldBe("Empresa Teste LTDA");
        result.Municipio.ShouldBe("São Paulo");
        result.Uf.ShouldBe("SP");
    }

    [Fact]
    public async Task ConsultarCnpjAsync_deve_usar_endpoint_sandbox_quando_sem_chave_producao()
    {
        var (provider, handler) = CreateProvider(new PlugNotasOptions());
        handler.Enqueue(HttpStatusCode.OK, "{}");

        await provider.ConsultarCnpjAsync("12345678000199");

        handler.Requests[0].RequestUri!.Host.ShouldBe("api.sandbox.plugnotas.com.br");
    }

    [Fact]
    public async Task ConsultarCnpjAsync_deve_usar_sandbox_quando_only_sandbox_mesmo_com_chave_producao()
    {
        var (provider, handler) = CreateProvider(new PlugNotasOptions
        {
            OnlySandbox = true,
            ProductionApiKey = "chave-producao-configurada"
        });
        handler.Enqueue(HttpStatusCode.OK, "{}");

        await provider.ConsultarCnpjAsync("12345678000199");

        handler.Requests[0].RequestUri!.Host.ShouldBe("api.sandbox.plugnotas.com.br");
        handler.Requests[0].Headers.GetValues("x-api-key").ShouldContain(PlugNotasAmbienteConstants.PublicSandboxApiKey);
    }

    [Fact]
    public async Task ConsultarCnpjAsync_deve_usar_producao_quando_only_sandbox_false_e_chave_producao_configurada()
    {
        var (provider, handler) = CreateProvider(new PlugNotasOptions
        {
            OnlySandbox = false,
            ProductionApiKey = "chave-producao-configurada"
        });
        handler.Enqueue(HttpStatusCode.OK, "{}");

        await provider.ConsultarCnpjAsync("12345678000199");

        handler.Requests[0].RequestUri!.Host.ShouldBe("api.plugnotas.com.br");
        handler.Requests[0].Headers.GetValues("x-api-key").ShouldContain("chave-producao-configurada");
    }

    [Fact]
    public async Task ConsultarCepAsync_deve_mapear_resultado_de_sucesso()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "cep": "01000000", "logradouro": "Praça da Sé", "uf": "SP" }""");

        var result = await provider.ConsultarCepAsync("01000000");

        result.Sucesso.ShouldBeTrue();
        result.Logradouro.ShouldBe("Praça da Sé");
    }

    [Theory]
    [InlineData("""{ "municipio": "Maringá", "ibge": "4115200" }""", "4115200")]
    [InlineData("""{ "municipio": "Maringá", "codigo_ibge": "4115201" }""", "4115201")]
    [InlineData("""{ "municipio": "Maringá", "codigo_municipio": "4115202" }""", "4115202")]
    [InlineData("""{ "municipio": "Maringá", "ibge": "4115200", "codigo_ibge": "9999999" }""", "4115200")]
    public async Task ConsultarCepAsync_deve_mapear_codigo_ibge_de_variantes_json(string json, string ibgeEsperado)
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, json);

        var result = await provider.ConsultarCepAsync("87111001");

        result.Sucesso.ShouldBeTrue();
        result.CodigoIbge.ShouldBe(ibgeEsperado);
    }

    [Fact]
    public async Task ConsultarCepAsync_deve_normalizar_codigo_ibge_com_mascara()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "municipio": "Maringá", "ibge": " 4115-200 ", "logradouro": "Rua A" }""");

        var result = await provider.ConsultarCepAsync("87111001");

        result.Sucesso.ShouldBeTrue();
        result.CodigoIbge.ShouldBe("4115200");
    }

    [Fact]
    public async Task ConsultarCepAsync_deve_retornar_falha_para_status_nao_2xx()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.NotFound, """{ "message": "CEP não encontrado" }""");

        var result = await provider.ConsultarCepAsync("00000000");

        result.Sucesso.ShouldBeFalse();
        result.Mensagem.ShouldBe("CEP não encontrado");
    }
}

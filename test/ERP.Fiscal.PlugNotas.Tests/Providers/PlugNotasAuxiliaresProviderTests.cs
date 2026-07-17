using System.Net;
using System.Threading.Tasks;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Providers;
using Microsoft.Extensions.Caching.Memory;
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
        var cache = new MemoryCache(new MemoryCacheOptions());
        return (new PlugNotasAuxiliaresProvider(httpClient, NullLogger<PlugNotasAuxiliaresProvider>.Instance, opts, cache), handler);
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

    [Fact]
    public async Task ConsultarMunicipiosAsync_deve_mapear_deduplicar_e_filtrar()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """
            [
              { "id": 4115200, "nome": "Maringá", "uf": "PR" },
              { "id": 4115200, "nome": "Maringá", "uf": "PR" },
              { "id": 3550308, "nome": "São Paulo", "uf": "SP" }
            ]
            """);

        var todos = await provider.ConsultarMunicipiosAsync();
        todos.Sucesso.ShouldBeTrue();
        todos.Itens.Count.ShouldBe(2);

        var filtrados = await provider.ConsultarMunicipiosAsync("maring", "PR");
        filtrados.Sucesso.ShouldBeTrue();
        filtrados.Itens.Count.ShouldBe(1);
        filtrados.Itens[0].CodigoIbge.ShouldBe("4115200");
        filtrados.Itens[0].Nome.ShouldBe("Maringá");
        filtrados.Itens[0].Uf.ShouldBe("PR");
    }

    [Fact]
    public async Task ConsultarMunicipiosAsync_deve_usar_cache_na_segunda_chamada()
    {
        var (provider, handler) = CreateProvider(new PlugNotasOptions { MunicipiosCacheMinutes = 60 });
        handler.Enqueue(HttpStatusCode.OK, """[{ "id": 4115200, "nome": "Maringá", "uf": "PR" }]""");

        await provider.ConsultarMunicipiosAsync();
        await provider.ConsultarMunicipiosAsync("maringa");

        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldContain("/nfse/cidades");
    }

    [Fact]
    public async Task ConsultarMunicipiosAsync_deve_retornar_falha_para_status_nao_2xx()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.BadRequest, """{ "error": { "message": "Motivo do erro" } }""");

        var result = await provider.ConsultarMunicipiosAsync();

        result.Sucesso.ShouldBeFalse();
        result.Mensagem.ShouldBe("Motivo do erro");
        result.Itens.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ConsultarMunicipiosAsync_deve_filtrar_por_codigo_ibge()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """
            [
              { "id": 4115200, "nome": "Maringá", "uf": "PR" },
              { "id": 3550308, "nome": "São Paulo", "uf": "SP" }
            ]
            """);

        var result = await provider.ConsultarMunicipiosAsync("4115200");

        result.Sucesso.ShouldBeTrue();
        result.Itens.Count.ShouldBe(1);
        result.Itens[0].Nome.ShouldBe("Maringá");
    }

    [Fact]
    public async Task ConsultarMunicipiosAsync_deve_filtrar_somente_por_uf()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """
            [
              { "id": 4115200, "nome": "Maringá", "uf": "PR" },
              { "id": 4106902, "nome": "Curitiba", "uf": "PR" },
              { "id": 3550308, "nome": "São Paulo", "uf": "SP" }
            ]
            """);

        var result = await provider.ConsultarMunicipiosAsync(null, "sp");

        result.Sucesso.ShouldBeTrue();
        result.Itens.Count.ShouldBe(1);
        result.Itens[0].CodigoIbge.ShouldBe("3550308");
    }

    [Fact]
    public async Task ConsultarMunicipiosAsync_deve_ignorar_itens_sem_id_ou_nome()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """
            [
              { "id": 4115200, "nome": "Maringá", "uf": "PR" },
              { "id": 9999999, "uf": "PR" },
              { "nome": "Sem IBGE", "uf": "PR" }
            ]
            """);

        var result = await provider.ConsultarMunicipiosAsync();

        result.Sucesso.ShouldBeTrue();
        result.Itens.Count.ShouldBe(1);
        result.Itens[0].CodigoIbge.ShouldBe("4115200");
    }

    [Fact]
    public async Task ConsultarMunicipiosAsync_deve_retornar_falha_em_erro_de_rede()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueNetworkFailure();

        var result = await provider.ConsultarMunicipiosAsync();

        result.Sucesso.ShouldBeFalse();
        result.HttpStatusCode.ShouldBe(0);
        result.Mensagem.ShouldNotBeNullOrWhiteSpace();
        result.Itens.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ConsultarMunicipioPorIbgeAsync_nao_deve_consumir_cache_da_lista()
    {
        var (provider, handler) = CreateProvider(new PlugNotasOptions { MunicipiosCacheMinutes = 60 });
        handler.Enqueue(HttpStatusCode.OK, """[{ "id": 4115200, "nome": "Maringá", "uf": "PR" }]""");
        handler.Enqueue(HttpStatusCode.OK, """{ "id": 3550308, "nome": "São Paulo", "uf": "SP" }""");

        await provider.ConsultarMunicipiosAsync();
        var porIbge = await provider.ConsultarMunicipioPorIbgeAsync("3550308");

        handler.Requests.Count.ShouldBe(2);
        porIbge.Sucesso.ShouldBeTrue();
        porIbge.Municipio!.CodigoIbge.ShouldBe("3550308");
        handler.Requests[1].RequestUri!.AbsolutePath.ShouldContain("/nfse/cidades/3550308");
    }

    [Fact]
    public async Task ConsultarMunicipiosAsync_deve_usar_cache_separado_por_ambiente()
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = FakeHttpMessageHandler.CreateClient(handler);
        var cache = new MemoryCache(new MemoryCacheOptions());

        var sandbox = new PlugNotasAuxiliaresProvider(
            httpClient,
            NullLogger<PlugNotasAuxiliaresProvider>.Instance,
            Options.Create(new PlugNotasOptions { OnlySandbox = true, MunicipiosCacheMinutes = 60 }),
            cache);
        var producao = new PlugNotasAuxiliaresProvider(
            httpClient,
            NullLogger<PlugNotasAuxiliaresProvider>.Instance,
            Options.Create(new PlugNotasOptions
            {
                OnlySandbox = false,
                ProductionApiKey = "chave-producao-configurada",
                MunicipiosCacheMinutes = 60
            }),
            cache);

        handler.Enqueue(HttpStatusCode.OK, """[{ "id": 4115200, "nome": "Maringá", "uf": "PR" }]""");
        handler.Enqueue(HttpStatusCode.OK, """[{ "id": 3550308, "nome": "São Paulo", "uf": "SP" }]""");

        var r1 = await sandbox.ConsultarMunicipiosAsync();
        var r2 = await producao.ConsultarMunicipiosAsync();

        handler.Requests.Count.ShouldBe(2);
        r1.Itens[0].CodigoIbge.ShouldBe("4115200");
        r2.Itens[0].CodigoIbge.ShouldBe("3550308");
        handler.Requests[0].RequestUri!.Host.ShouldBe("api.sandbox.plugnotas.com.br");
        handler.Requests[1].RequestUri!.Host.ShouldBe("api.plugnotas.com.br");
    }

    [Fact]
    public async Task ConsultarMunicipioPorIbgeAsync_deve_mapear_sucesso()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "id": 4115200, "nome": "Maringá", "uf": "PR" }""");

        var result = await provider.ConsultarMunicipioPorIbgeAsync("4115200");

        result.Sucesso.ShouldBeTrue();
        result.Municipio!.CodigoIbge.ShouldBe("4115200");
        result.Municipio.Nome.ShouldBe("Maringá");
        result.Municipio.Uf.ShouldBe("PR");
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldContain("/nfse/cidades/4115200");
    }

    [Fact]
    public async Task ConsultarMunicipioPorIbgeAsync_deve_retornar_falha_404()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.NotFound, """{ "error": { "message": "Nao localizamos qualquer Cidade" } }""");

        var result = await provider.ConsultarMunicipioPorIbgeAsync("411520");

        result.Sucesso.ShouldBeFalse();
        result.Municipio.ShouldBeNull();
        result.Mensagem.ShouldContain("Nao localizamos");
    }
}

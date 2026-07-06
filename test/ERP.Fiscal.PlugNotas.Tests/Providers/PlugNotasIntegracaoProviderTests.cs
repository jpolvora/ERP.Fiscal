using System.Net;
using System.Threading.Tasks;
using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Http;
using ERP.Fiscal.PlugNotas.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Providers;

public class PlugNotasIntegracaoProviderTests
{
    private static (PlugNotasIntegracaoProvider Provider, FakeHttpMessageHandler Handler) CreateProvider()
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = FakeHttpMessageHandler.CreateClient(handler);
        var opts = Options.Create(new PlugNotasOptions { ProductionApiKey = "chave-prod" });
        var innerClient = new PlugNotasHttpClient(httpClient, NullLogger<PlugNotasHttpClient>.Instance, opts);
        return (new PlugNotasIntegracaoProvider(innerClient, opts), handler);
    }

    [Fact]
    public async Task CadastrarCertificadoAsync_deve_retornar_id_do_provedor_em_sucesso()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "message": "ok", "id": "cert-123" }""");

        var result = await provider.CadastrarCertificadoAsync(
            new NfeCertificadoUpload { ArquivoBytes = new byte[] { 1 }, Senha = "123" },
            NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.IdProvedor.ShouldBe("cert-123");
    }

    [Fact]
    public async Task CadastrarEmissorAsync_deve_mapear_dados_para_payload_e_retornar_id()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "message": "ok", "data": { "cnpj": "12345678000199" } }""");

        var result = await provider.CadastrarEmissorAsync(
            new NfeEmissorData
            {
                CpfCnpj = "12345678000199",
                RazaoSocial = "Empresa Teste LTDA",
                IdCertificadoProvedor = "cert-123",
                Producao = true
            },
            NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.IdProvedor.ShouldBe("12345678000199");

        var body = handler.RequestBodies[0];
        body.ShouldContain("\"cpfCnpj\":\"12345678000199\"");
        body.ShouldContain("\"certificado\":\"cert-123\"");
    }

    [Fact]
    public async Task SincronizarAmbienteEmissorAsync_deve_enviar_patch_com_producao()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "message": "ok", "id": "12345678000199" }""");

        var result = await provider.SincronizarAmbienteEmissorAsync("12345678000199", true, NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        handler.Requests[0].Method.ShouldBe(HttpMethod.Patch);
    }

    [Fact]
    public async Task ConsultarEmissorAsync_deve_retornar_campos_estruturados()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(
            HttpStatusCode.OK,
            """{ "data": { "id": "pn-empresa-1", "cnpj": "12345678000199", "razaoSocial": "Empresa Teste", "email": "contato@empresa.com", "nfe": { "config": { "producao": true } } } }"""
        );

        var result = await provider.ConsultarEmissorAsync("12345678000199", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.IdProvedor.ShouldBe("pn-empresa-1");
        result.CpfCnpj.ShouldBe("12345678000199");
        result.Nome.ShouldBe("Empresa Teste");
        result.Email.ShouldBe("contato@empresa.com");
        result.Producao.ShouldBe(true);
    }

    [Fact]
    public async Task ConsultarCertificadoAsync_deve_retornar_metadados_estruturados()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(
            HttpStatusCode.OK,
            """{ "data": { "id": "cert-123", "nome": "Certificado A1", "validadeFinal": "2027-01-31T00:00:00Z" } }"""
        );

        var result = await provider.ConsultarCertificadoAsync("cert-123", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.IdProvedor.ShouldBe("cert-123");
        result.Nome.ShouldBe("Certificado A1");
        result.ValidadeFinal.ShouldBe(new System.DateTime(2027, 1, 31, 0, 0, 0, System.DateTimeKind.Utc));
    }
}

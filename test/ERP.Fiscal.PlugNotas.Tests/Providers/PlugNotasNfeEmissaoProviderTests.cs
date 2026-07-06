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

public class PlugNotasNfeEmissaoProviderTests
{
    private static (PlugNotasNfeEmissaoProvider Provider, FakeHttpMessageHandler Handler) CreateProvider(PlugNotasOptions? options = null)
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = FakeHttpMessageHandler.CreateClient(handler);
        var opts = Options.Create(options ?? new PlugNotasOptions { ProductionApiKey = "chave-prod" });
        var innerClient = new PlugNotasHttpClient(httpClient, NullLogger<PlugNotasHttpClient>.Instance, opts);
        return (new PlugNotasNfeEmissaoProvider(innerClient, opts, NullLogger<PlugNotasNfeEmissaoProvider>.Instance), handler);
    }

    [Fact]
    public async Task EmitirAsync_deve_mapear_resultado_neutro_de_sucesso()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "documents": [{ "id": "aabbccddeeff00112233ddee", "protocolo": "135240000123456" }] }""");

        var result = await provider.EmitirAsync("[]", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.IdDocumentoProvedor.ShouldBe("aabbccddeeff00112233ddee");
        result.Protocolo.ShouldBe("135240000123456");
        result.IsTransientFailure.ShouldBeFalse();
    }

    [Fact]
    public async Task EmitirAsync_deve_marcar_falha_transitoria()
    {
        var (provider, handler) = CreateProvider(new PlugNotasOptions
        {
            ProductionApiKey = "chave-prod",
            Retry = new PlugNotasRetryOptions { MaxAttempts = 1, BaseDelayMs = 1 }
        });
        handler.Enqueue(HttpStatusCode.ServiceUnavailable, "{}");

        var result = await provider.EmitirAsync("[]", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeFalse();
        result.IsTransientFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task EmitirAsync_deve_usar_endpoint_sandbox_quando_ambiente_sandbox()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, "{}");

        await provider.EmitirAsync("[]", NfeAmbiente.Sandbox);

        handler.Requests[0].RequestUri!.Host.ShouldBe("api.sandbox.plugnotas.com.br");
        handler.Requests[0].Headers.GetValues("x-api-key").ShouldContain(PlugNotasAmbienteConstants.PublicSandboxApiKey);
    }

    [Fact]
    public async Task ConsultarPorIdAsync_deve_mapear_situacao_autorizada()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "cStat": 100, "numero": "42" }""");

        var result = await provider.ConsultarPorIdAsync("aabbccddeeff00112233ddee", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.Situacao.ShouldBe(NfeSituacao.Autorizada);
        result.NumeroNota.ShouldBe("42");
    }

    [Fact]
    public async Task ObterPdfAsync_deve_retornar_bytes_quando_sucesso()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(_ => new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new System.Net.Http.ByteArrayContent(new byte[] { 1, 2, 3 })
        });

        var result = await provider.ObterPdfAsync("aabbccddeeff00112233ddee", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.PdfBytes.ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public async Task EmitirCompletoAsync_deve_consolidar_autorizacao_com_xml_e_pdf()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "documents": [{ "id": "aabbccddeeff00112233ddee" }] }""");
        handler.Enqueue(HttpStatusCode.OK, """{ "id": "aabbccddeeff00112233ddee", "cStat": 100, "chave": "35123456789012345678901234567890123456789012", "protocolo": "135240000123456" }""");
        handler.Enqueue(HttpStatusCode.OK, "<xml>ok</xml>");
        handler.Enqueue(_ => new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new System.Net.Http.ByteArrayContent(new byte[] { 9, 8, 7 })
        });

        var result = await provider.EmitirCompletoAsync("[]", "12345678000199", "doc-1", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.Situacao.ShouldBe(NfeSituacao.Autorizada);
        result.IdDocumentoProvedor.ShouldBe("aabbccddeeff00112233ddee");
        result.ChaveAcesso.ShouldBe("35123456789012345678901234567890123456789012");
        result.XmlContent.ShouldBe("<xml>ok</xml>");
        result.PdfBytes.ShouldBe(new byte[] { 9, 8, 7 });
    }

    [Fact]
    public async Task EmitirCompletoAsync_deve_retornar_processando_quando_consulta_nao_finaliza()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "documents": [{ "id": "aabbccddeeff00112233ddee" }] }""");
        for (var i = 0; i < 6; i++)
        {
            handler.Enqueue(HttpStatusCode.OK, """{ "id": "aabbccddeeff00112233ddee", "status": "PROCESSANDO" }""");
        }

        var result = await provider.EmitirCompletoAsync("[]", "12345678000199", "doc-1", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.Situacao.ShouldBe(NfeSituacao.Processando);
        result.CodigoRetorno.ShouldBe("PROCESSANDO");
    }
}

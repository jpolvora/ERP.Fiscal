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

public class PlugNotasNfseEmissaoProviderTests
{
    private static (PlugNotasNfseEmissaoProvider Provider, FakeHttpMessageHandler Handler) CreateProvider(PlugNotasOptions? options = null)
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = FakeHttpMessageHandler.CreateClient(handler);
        var opts = Options.Create(options ?? new PlugNotasOptions { ProductionApiKey = "chave-prod" });
        var innerClient = new PlugNotasHttpClient(httpClient, NullLogger<PlugNotasHttpClient>.Instance, opts);
        return (new PlugNotasNfseEmissaoProvider(innerClient, opts, NullLogger<PlugNotasNfseEmissaoProvider>.Instance), handler);
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
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldEndWith("/nfse");
    }

    [Fact]
    public async Task EmitirAsync_deve_marcar_falha_transitoria_em_5xx()
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
        handler.Requests[0].Method.ShouldBe(HttpMethod.Post);
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldEndWith("/nfse");
    }

    [Fact]
    public async Task EmitirAsync_deve_classificar_erro_4xx_como_nao_transitorio()
    {
        var (provider, handler) = CreateProvider(new PlugNotasOptions
        {
            ProductionApiKey = "chave-prod",
            Retry = new PlugNotasRetryOptions { MaxAttempts = 1, BaseDelayMs = 1 }
        });
        handler.Enqueue(HttpStatusCode.BadRequest, """{ "error": { "message": "Payload inválido" } }""");

        var result = await provider.EmitirAsync("[]", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeFalse();
        result.IsTransientFailure.ShouldBeFalse();
        result.HttpStatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EmitirAsync_deve_usar_endpoint_sandbox_quando_ambiente_sandbox()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, "{}");

        await provider.EmitirAsync("[]", NfeAmbiente.Sandbox);

        handler.Requests[0].RequestUri!.Host.ShouldBe("api.sandbox.plugnotas.com.br");
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldEndWith("/nfse");
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
    public async Task ConsultarPorIdAsync_deve_mapear_situacao_concluido_nfse()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """
            [{
              "id": "aabbccddeeff00112233ddee",
              "situacao": "CONCLUIDO",
              "numeroNfse": "13398",
              "codigoVerificacao": "5278FE6A7",
              "mensagem": "RPS Autorizada com sucesso"
            }]
            """);

        var result = await provider.ConsultarPorIdAsync("aabbccddeeff00112233ddee", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.Situacao.ShouldBe(NfeSituacao.Autorizada);
        result.NumeroNota.ShouldBe("13398");
        result.ChaveAcesso.ShouldBe("5278FE6A7");
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldEndWith("/nfse/consultar/aabbccddeeff00112233ddee");
    }

    [Fact]
    public async Task ObterPdfAsync_deve_usar_rota_nfse_pdf_por_id()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(_ => new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new System.Net.Http.ByteArrayContent(new byte[] { 1, 2, 3 })
        });

        var result = await provider.ObterPdfAsync("aabbccddeeff00112233ddee", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.PdfBytes.ShouldBe(new byte[] { 1, 2, 3 });
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldEndWith("/nfse/pdf/aabbccddeeff00112233ddee");
    }

    [Fact]
    public async Task EmitirCompletoAsync_deve_consolidar_autorizacao_com_xml_e_pdf()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "documents": [{ "id": "aabbccddeeff00112233ddee" }] }""");
        handler.Enqueue(HttpStatusCode.OK, """
            [{
              "id": "aabbccddeeff00112233ddee",
              "situacao": "CONCLUIDO",
              "numeroNfse": "13398",
              "codigoVerificacao": "5278FE6A7",
              "mensagem": "RPS Autorizada com sucesso"
            }]
            """);
        handler.Enqueue(HttpStatusCode.OK, "<xml>nfse</xml>");
        handler.Enqueue(_ => new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new System.Net.Http.ByteArrayContent(new byte[] { 9, 8, 7 })
        });

        var result = await provider.EmitirCompletoAsync("[]", "12345678000199", "doc-nfse-1", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.Situacao.ShouldBe(NfeSituacao.Autorizada);
        result.IdDocumentoProvedor.ShouldBe("aabbccddeeff00112233ddee");
        result.ChaveAcesso.ShouldBe("5278FE6A7");
        result.XmlContent.ShouldBe("<xml>nfse</xml>");
        result.PdfBytes.ShouldBe(new byte[] { 9, 8, 7 });
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldEndWith("/nfse");
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

        var result = await provider.EmitirCompletoAsync("[]", "12345678000199", "doc-nfse-1", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.Situacao.ShouldBe(NfeSituacao.Processando);
        result.CodigoRetorno.ShouldBe("PROCESSANDO");
    }

    [Fact]
    public async Task EmitirCompletoAsync_deve_mapear_rejeicao_municipal()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "documents": [{ "id": "aabbccddeeff00112233ddee" }] }""");
        handler.Enqueue(HttpStatusCode.OK, """
            [{
              "id": "aabbccddeeff00112233ddee",
              "situacao": "REJEITADO",
              "mensagem": "Serviço não autorizado no município"
            }]
            """);

        var result = await provider.EmitirCompletoAsync("[]", "12345678000199", "doc-nfse-1", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.Situacao.ShouldBe(NfeSituacao.Rejeitada);
    }

    [Fact]
    public async Task CancelarAsync_deve_usar_rota_cancelamento_nfse()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """{ "message": "Cancelamento solicitado" }""");

        var result = await provider.CancelarAsync("aabbccddeeff00112233ddee", "Erro de digitação", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        handler.Requests[0].Method.ShouldBe(HttpMethod.Post);
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldEndWith("/nfse/aabbccddeeff00112233ddee/cancelamento");
    }

    [Fact]
    public async Task ObterXmlAsync_deve_usar_rota_nfse_xml_por_id()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, "<nfse>ok</nfse>");

        var result = await provider.ObterXmlAsync("aabbccddeeff00112233ddee", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.XmlContent.ShouldBe("<nfse>ok</nfse>");
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldEndWith("/nfse/xml/aabbccddeeff00112233ddee");
    }

    [Fact]
    public async Task ConsultarPorIdIntegracaoAsync_deve_usar_rota_resumo_por_cnpj()
    {
        var (provider, handler) = CreateProvider();
        handler.Enqueue(HttpStatusCode.OK, """
            {
              "id": "aabbccddeeff00112233ddee",
              "situacao": "CONCLUIDO",
              "numeroNfse": "100",
              "codigoVerificacao": "ABC"
            }
            """);

        var result = await provider.ConsultarPorIdIntegracaoAsync("12345678000199", "doc-1", NfeAmbiente.Producao);

        result.Sucesso.ShouldBeTrue();
        result.Situacao.ShouldBe(NfeSituacao.Autorizada);
        result.NumeroNota.ShouldBe("100");
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldEndWith("/nfse/12345678000199/doc-1/resumo");
    }
}

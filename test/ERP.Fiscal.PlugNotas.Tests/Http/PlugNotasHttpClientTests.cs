using System.Net;
using System.Threading.Tasks;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Http;

public class PlugNotasHttpClientTests
{
    private static PlugNotasHttpClient CreateClient(FakeHttpMessageHandler handler, PlugNotasOptions? options = null)
    {
        var httpClient = FakeHttpMessageHandler.CreateClient(handler);
        httpClient.BaseAddress = null;
        return new PlugNotasHttpClient(httpClient, NullLogger<PlugNotasHttpClient>.Instance, Options.Create(options ?? new PlugNotasOptions()));
    }

    [Fact]
    public async Task EmitirNfeAsync_deve_retornar_sucesso_com_id_e_protocolo_no_primeiro_status_2xx()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, """{ "documents": [{ "id": "aabbccddeeff00112233ddee", "protocolo": "135240000123456" }] }""");
        var client = CreateClient(handler);

        var result = await client.EmitirNfeAsync("https://api.plugnotas.com.br", "token", "[]");

        result.Success.ShouldBeTrue();
        result.IdDocumento.ShouldBe("aabbccddeeff00112233ddee");
        result.Protocolo.ShouldBe("135240000123456");
        handler.Requests.ShouldHaveSingleItem();
        handler.Requests[0].Headers.GetValues("x-api-key").ShouldContain("token");
    }

    [Fact]
    public async Task EmitirNfeAsync_deve_repetir_em_falha_transitoria_e_ter_sucesso_na_segunda_tentativa()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.ServiceUnavailable, "{}")
            .Enqueue(HttpStatusCode.OK, """{ "documents": [{ "id": "aabbccddeeff00112233ddee" }] }""");
        var options = new PlugNotasOptions { Retry = new PlugNotasRetryOptions { MaxAttempts = 3, BaseDelayMs = 1 } };
        var client = CreateClient(handler, options);

        var result = await client.EmitirNfeAsync("https://api.plugnotas.com.br", "token", "[]");

        result.Success.ShouldBeTrue();
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task EmitirNfeAsync_nao_deve_repetir_em_falha_permanente()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.BadRequest, """{ "error": { "message": "CNPJ inválido" } }""");
        var options = new PlugNotasOptions { Retry = new PlugNotasRetryOptions { MaxAttempts = 3, BaseDelayMs = 1 } };
        var client = CreateClient(handler, options);

        var result = await client.EmitirNfeAsync("https://api.plugnotas.com.br", "token", "[]");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("CNPJ inválido");
        handler.Requests.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task EmitirNfeAsync_deve_esgotar_tentativas_e_retornar_ultima_falha_transitoria()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.ServiceUnavailable, "{}")
            .Enqueue(HttpStatusCode.ServiceUnavailable, "{}");
        var options = new PlugNotasOptions { Retry = new PlugNotasRetryOptions { MaxAttempts = 2, BaseDelayMs = 1 } };
        var client = CreateClient(handler, options);

        var result = await client.EmitirNfeAsync("https://api.plugnotas.com.br", "token", "[]");

        result.Success.ShouldBeFalse();
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ObterNfeResumoPorIdAsync_deve_chamar_rota_id_resumo()
    {
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, "[]");
        var client = CreateClient(handler);

        await client.ObterNfeResumoPorIdAsync("https://api.sandbox.plugnotas.com.br", "test-key", "66958a6505757b0e34f1344a");

        handler.Requests[0].RequestUri!.ToString().ShouldBe("https://api.sandbox.plugnotas.com.br/nfe/66958a6505757b0e34f1344a/resumo");
    }

    [Fact]
    public async Task ObterNfeResumoPorCnpjIdIntegracaoAsync_deve_chamar_rota_cnpj_idIntegracao_resumo()
    {
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, "[]");
        var client = CreateClient(handler);

        await client.ObterNfeResumoPorCnpjIdIntegracaoAsync(
            "https://api.sandbox.plugnotas.com.br", "test-key", "53.738.428/0001-00", "3a2051697c93d0aca55f0557987f77d8");

        handler.Requests[0].RequestUri!.ToString().ShouldBe(
            "https://api.sandbox.plugnotas.com.br/nfe/53738428000100/3a2051697c93d0aca55f0557987f77d8/resumo");
    }

    [Fact]
    public async Task CadastrarCertificadoAsync_deve_enviar_multipart_com_arquivo_e_senha()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, """{ "message": "ok", "id": "cert-123" }""");
        var client = CreateClient(handler);

        var result = await client.CadastrarCertificadoAsync(
            "https://api.plugnotas.com.br", "token", new byte[] { 1, 2, 3 }, "senha123", "contato@empresa.com");

        result.Success.ShouldBeTrue();
        result.Id.ShouldBe("cert-123");
        var body = handler.RequestBodies[0];
        body.ShouldContain("senha123");
        body.ShouldContain("contato@empresa.com");
    }

    [Fact]
    public async Task CadastrarCertificadoAsync_deve_retornar_falha_estruturada_quando_arquivo_for_nulo()
    {
        var handler = new FakeHttpMessageHandler();
        var client = CreateClient(handler);

        var result = await client.CadastrarCertificadoAsync(
            "https://api.plugnotas.com.br", "token", null!, "senha", null);

        result.Success.ShouldBeFalse();
        result.HttpStatusCode.ShouldBe(0);
        result.ErrorMessage.ShouldBe("Arquivo do certificado deve ser informado.");
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task CadastrarCertificadoAsync_deve_retornar_falha_estruturada_quando_arquivo_for_vazio()
    {
        var handler = new FakeHttpMessageHandler();
        var client = CreateClient(handler);

        var result = await client.CadastrarCertificadoAsync(
            "https://api.plugnotas.com.br", "token", [], "senha", null);

        result.Success.ShouldBeFalse();
        result.HttpStatusCode.ShouldBe(0);
        result.ErrorMessage.ShouldBe("Arquivo do certificado deve ser informado.");
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task CadastrarEmpresaAsync_deve_extrair_id_de_data_cnpj()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, """{ "message": "ok", "data": { "cnpj": "12345678000199" } }""");
        var client = CreateClient(handler);

        var payload = new ERP.Fiscal.PlugNotas.Contracts.PlugNotasEmpresaPayload { CpfCnpj = "12345678000199" };
        var result = await client.CadastrarEmpresaAsync("https://api.plugnotas.com.br", "token", payload);

        result.Success.ShouldBeTrue();
        result.Id.ShouldBe("12345678000199");
    }

    [Fact]
    public async Task CadastrarCertificadoAsync_deve_retornar_falha_estruturada_em_erro_de_rede()
    {
        var handler = new FakeHttpMessageHandler()
            .EnqueueNetworkFailure();
        var client = CreateClient(handler);

        var result = await client.CadastrarCertificadoAsync(
            "https://api.plugnotas.com.br", "token", new byte[] { 1 }, "senha", null);

        result.Success.ShouldBeFalse();
        result.HttpStatusCode.ShouldBe(0);
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("Falha de rede");
    }

    [Fact]
    public async Task AtualizarConfigEmpresaAsync_deve_normalizar_cnpj_mascarado_na_url()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, """{ "message": "ok", "data": { "cnpj": "12345678000199" } }""");
        var client = CreateClient(handler);

        var result = await client.AtualizarConfigEmpresaAsync(
            "https://api.plugnotas.com.br", "token", "12.345.678/0001-99", true);

        result.Success.ShouldBeTrue();
        handler.Requests[0].RequestUri!.ToString().ShouldBe(
            "https://api.plugnotas.com.br/empresa/12345678000199/config");
    }
}

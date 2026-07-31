using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Http;
using ERP.Fiscal.PlugNotas.Parsers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Fiscal.PlugNotas.Providers;

/// <summary>Implementação PlugNotas de <see cref="INfseEmissaoProvider"/>. Consumo apenas via a interface (DI).</summary>
internal class PlugNotasNfseEmissaoProvider : INfseEmissaoProvider
{
    private const int MaxConsultaPolls = 6;
    private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(2);

    private readonly PlugNotasHttpClient _http;
    private readonly IOptions<PlugNotasOptions> _options;
    private readonly ILogger<PlugNotasNfseEmissaoProvider> _logger;

    public PlugNotasNfseEmissaoProvider(
        PlugNotasHttpClient http,
        IOptions<PlugNotasOptions> options,
        ILogger<PlugNotasNfseEmissaoProvider> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<NfeEmissaoResult> EmitirAsync(string payloadJson, NfeAmbiente ambiente, CancellationToken cancellationToken = default)
    {
        var (baseUrl, apiToken) = ResolveEndpoint(ambiente);
        var raw = await _http.EmitirNfseAsync(baseUrl, apiToken, payloadJson, cancellationToken);
        return new NfeEmissaoResult
        {
            Sucesso = raw.Success,
            IdDocumentoProvedor = raw.IdDocumento,
            Protocolo = raw.Protocolo,
            Mensagem = raw.ErrorMessage,
            IsTransientFailure = raw.IsTransientFailure,
            HttpStatusCode = raw.HttpStatusCode,
            RawResponse = raw.RawBody
        };
    }

    public async Task<NfeConsultaResult> ConsultarPorIdAsync(string idDocumentoProvedor, NfeAmbiente ambiente, CancellationToken cancellationToken = default)
    {
        var (baseUrl, apiToken) = ResolveEndpoint(ambiente);
        var raw = await _http.ObterNfseResumoPorIdAsync(baseUrl, apiToken, idDocumentoProvedor, cancellationToken);
        return MapConsultaResult(raw.Success, raw.HttpStatusCode, raw.RawBody, raw.ErrorMessage);
    }

    public async Task<NfeConsultaResult> ConsultarPorIdIntegracaoAsync(string cpfCnpjDigits, string idIntegracao, NfeAmbiente ambiente, CancellationToken cancellationToken = default)
    {
        var (baseUrl, apiToken) = ResolveEndpoint(ambiente);
        var raw = await _http.ObterNfseResumoPorCnpjIdIntegracaoAsync(baseUrl, apiToken, cpfCnpjDigits, idIntegracao, cancellationToken);
        return MapConsultaResult(raw.Success, raw.HttpStatusCode, raw.RawBody, raw.ErrorMessage);
    }

    public async Task<NfeProcessamentoResult> EmitirCompletoAsync(
        string payloadJson,
        string? cpfCnpjDigits,
        string? idIntegracao,
        NfeAmbiente ambiente,
        CancellationToken cancellationToken = default)
    {
        var payloadEnviado = payloadJson;
        var emissao = await EmitirAsync(payloadEnviado, ambiente, cancellationToken);
        if (!emissao.Sucesso)
        {
            return MapEmissaoFalha(emissao, payloadEnviado, idIntegracao);
        }

        var cnpj = FiscalDigitsHelper.DigitsOnlyOrNull(cpfCnpjDigits);
        var consulta = await PollConsultaFinalAsync(
            emissao.IdDocumentoProvedor,
            cnpj,
            idIntegracao,
            ambiente,
            cancellationToken);

        if (consulta == null)
        {
            return new NfeProcessamentoResult
            {
                Sucesso = true,
                IdDocumentoProvedor = emissao.IdDocumentoProvedor,
                IdIntegracao = idIntegracao,
                Protocolo = emissao.Protocolo,
                Situacao = NfeSituacao.Processando,
                CodigoRetorno = "PROCESSANDO",
                Mensagem = "Documento enviado; aguardando retorno da prefeitura (consulte o status).",
                PayloadEnviado = payloadEnviado,
                RawResponse = emissao.RawResponse,
                IsTransientFailure = emissao.IsTransientFailure,
                HttpStatusCode = emissao.HttpStatusCode
            };
        }

        if (!consulta.Sucesso)
        {
            return MapConsultaFalha(consulta, emissao.IdDocumentoProvedor, idIntegracao, payloadEnviado);
        }

        return await BuildProcessamentoResultAsync(consulta, payloadEnviado, idIntegracao, ambiente, cancellationToken);
    }

    public async Task<NfeProcessamentoResult> ConsultarResultadoAsync(
        string identificador,
        NfeAmbiente ambiente,
        CancellationToken cancellationToken = default)
    {
        var consulta = await ConsultarPorIdAsync(identificador, ambiente, cancellationToken);
        if (!consulta.Sucesso)
        {
            return MapConsultaFalha(consulta, identificador, null, null);
        }

        return await BuildProcessamentoResultAsync(consulta, null, null, ambiente, cancellationToken);
    }

    public async Task<NfeCancelamentoResult> CancelarAsync(string idDocumentoProvedor, string justificativa, NfeAmbiente ambiente, CancellationToken cancellationToken = default)
    {
        var (baseUrl, apiToken) = ResolveEndpoint(ambiente);
        var raw = await _http.CancelarNfseAsync(baseUrl, apiToken, idDocumentoProvedor, justificativa, cancellationToken);
        return new NfeCancelamentoResult
        {
            Sucesso = raw.Success,
            Mensagem = raw.ErrorMessage,
            HttpStatusCode = raw.HttpStatusCode,
            RawResponse = raw.RawBody
        };
    }

    public async Task<NfeXmlResult> ObterXmlAsync(string idDocumentoProvedor, NfeAmbiente ambiente, CancellationToken cancellationToken = default)
    {
        var (baseUrl, apiToken) = ResolveEndpoint(ambiente);
        var raw = await _http.ObterXmlNfsePorIdAsync(baseUrl, apiToken, idDocumentoProvedor, cancellationToken);
        return new NfeXmlResult
        {
            Sucesso = raw.Success,
            XmlContent = raw.Success ? raw.RawBody : null,
            Mensagem = raw.ErrorMessage,
            HttpStatusCode = raw.HttpStatusCode
        };
    }

    public async Task<NfePdfResult> ObterPdfAsync(string idDocumentoProvedor, NfeAmbiente ambiente, CancellationToken cancellationToken = default)
    {
        var (baseUrl, apiToken) = ResolveEndpoint(ambiente);
        var raw = await _http.ObterPdfNfsePorIdAsync(baseUrl, apiToken, idDocumentoProvedor, cancellationToken);
        return new NfePdfResult
        {
            Sucesso = raw.Success,
            PdfBytes = raw.Content,
            ContentType = raw.ContentType,
            Mensagem = raw.ErrorMessage,
            HttpStatusCode = raw.HttpStatusCode
        };
    }

    private async Task<NfeConsultaResult?> PollConsultaFinalAsync(
        string? idDocumento,
        string? cpfCnpjDigits,
        string? idIntegracao,
        NfeAmbiente ambiente,
        CancellationToken cancellationToken)
    {
        NfeConsultaResult? ultima = null;
        for (var i = 0; i < MaxConsultaPolls; i++)
        {
            ultima = await ConsultarResumoAsync(
                idDocumento,
                cpfCnpjDigits,
                idIntegracao,
                ambiente,
                cancellationToken);

            if (!ultima.Sucesso)
            {
                if (i < MaxConsultaPolls - 1)
                {
                    await Task.Delay(PollDelay, cancellationToken);
                }

                continue;
            }

            idDocumento ??= ultima.IdDocumentoProvedor;
            if (ultima.Situacao is NfeSituacao.Autorizada or NfeSituacao.Rejeitada or NfeSituacao.Cancelada)
            {
                return ultima;
            }

            if (i < MaxConsultaPolls - 1)
            {
                await Task.Delay(PollDelay, cancellationToken);
            }
        }

        return ultima?.Situacao == NfeSituacao.Processando ? null : ultima;
    }

    private async Task<NfeConsultaResult> ConsultarResumoAsync(
        string? idDocumento,
        string? cpfCnpjDigits,
        string? idIntegracao,
        NfeAmbiente ambiente,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(idDocumento)
            && PlugNotasNfeEmissaoRespostaParser.LooksLikeIdDocumentoPlugNotas(idDocumento))
        {
            return await ConsultarPorIdAsync(idDocumento, ambiente, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(cpfCnpjDigits) && !string.IsNullOrWhiteSpace(idIntegracao))
        {
            return await ConsultarPorIdIntegracaoAsync(cpfCnpjDigits, idIntegracao, ambiente, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(idDocumento))
        {
            return await ConsultarPorIdAsync(idDocumento, ambiente, cancellationToken);
        }

        return new NfeConsultaResult
        {
            Sucesso = false,
            Mensagem = "Identificadores insuficientes para consulta NFS-e"
        };
    }

    private async Task<NfeProcessamentoResult> BuildProcessamentoResultAsync(
        NfeConsultaResult consulta,
        string? payloadEnviado,
        string? idIntegracao,
        NfeAmbiente ambiente,
        CancellationToken cancellationToken)
    {
        var idDocumento = consulta.IdDocumentoProvedor;
        var xml = consulta.XmlContent;
        byte[]? pdf = null;

        if (consulta.Situacao == NfeSituacao.Autorizada && !string.IsNullOrWhiteSpace(idDocumento))
        {
            var xmlResult = await ObterXmlAsync(idDocumento, ambiente, cancellationToken);
            if (xmlResult.Sucesso)
            {
                xml = xmlResult.XmlContent;
            }

            var pdfResult = await ObterPdfAsync(idDocumento, ambiente, cancellationToken);
            if (pdfResult.Sucesso)
            {
                pdf = pdfResult.PdfBytes;
            }
        }

        return new NfeProcessamentoResult
        {
            Sucesso = true,
            IdDocumentoProvedor = idDocumento,
            IdIntegracao = idIntegracao,
            ChaveAcesso = consulta.ChaveAcesso,
            Protocolo = consulta.Protocolo,
            Situacao = consulta.Situacao,
            CodigoRetorno = ResolveCodigoRetorno(consulta.CodigoStatusSefaz, consulta.RawResponse),
            Mensagem = consulta.Mensagem,
            PayloadEnviado = payloadEnviado,
            RawResponse = consulta.RawResponse,
            XmlContent = xml,
            PdfBytes = pdf,
            HttpStatusCode = consulta.HttpStatusCode
        };
    }

    private static NfeConsultaResult MapConsultaResult(bool sucesso, int httpStatusCode, string? rawBody, string? errorMessage)
    {
        var campos = sucesso ? PlugNotasNfeConsultaRespostaParser.TryParse(rawBody) : null;
        return new NfeConsultaResult
        {
            Sucesso = sucesso,
            IdDocumentoProvedor = campos?.IdDocumentoProvedor,
            ChaveAcesso = campos?.ChaveAcesso,
            Protocolo = campos?.ProtocoloAutorizacao,
            Situacao = campos?.SituacaoResumida,
            CodigoStatusSefaz = campos?.CodigoStatusSefaz,
            NumeroNota = campos?.NumeroNota,
            Serie = campos?.Serie,
            Mensagem = errorMessage ?? campos?.MensagemSefaz,
            XmlContent = null,
            HttpStatusCode = httpStatusCode,
            RawResponse = rawBody
        };
    }

    private static NfeProcessamentoResult MapEmissaoFalha(
        NfeEmissaoResult emissao,
        string payloadEnviado,
        string? idIntegracao)
    {
        return new NfeProcessamentoResult
        {
            Sucesso = false,
            IdDocumentoProvedor = emissao.IdDocumentoProvedor,
            IdIntegracao = idIntegracao,
            Protocolo = emissao.Protocolo,
            CodigoRetorno = ResolveCodigoRetorno(null, emissao.RawResponse) ?? ToNullableString(emissao.HttpStatusCode),
            Mensagem = emissao.Mensagem ?? "Falha na emissão PlugNotas (NFS-e)",
            PayloadEnviado = payloadEnviado,
            RawResponse = emissao.RawResponse,
            IsTransientFailure = emissao.IsTransientFailure,
            HttpStatusCode = emissao.HttpStatusCode
        };
    }

    private static NfeProcessamentoResult MapConsultaFalha(
        NfeConsultaResult consulta,
        string? identificador,
        string? idIntegracao,
        string? payloadEnviado)
    {
        return new NfeProcessamentoResult
        {
            Sucesso = false,
            IdDocumentoProvedor = identificador,
            IdIntegracao = idIntegracao,
            CodigoRetorno = ResolveCodigoRetorno(consulta.CodigoStatusSefaz, consulta.RawResponse)
                ?? ToNullableString(consulta.HttpStatusCode),
            Mensagem = consulta.Mensagem,
            PayloadEnviado = payloadEnviado,
            RawResponse = consulta.RawResponse,
            HttpStatusCode = consulta.HttpStatusCode
        };
    }

    private static string? ResolveCodigoRetorno(int? codigoStatusSefaz, string? rawBody) =>
        codigoStatusSefaz?.ToString()
        ?? PlugNotasNfeEmissaoRespostaParser.TryExtrairCodigoRejeicao(rawBody);

    private static string? ToNullableString(int value) => value > 0 ? value.ToString() : null;

    private (string BaseUrl, string ApiToken) ResolveEndpoint(NfeAmbiente ambiente)
    {
        var baseUrl = PlugNotasBaseUrlResolver.Resolve(ambiente);
        var apiToken = PlugNotasApiKeyResolver.Resolve(_options, ambiente);
        return (baseUrl, apiToken);
    }
}

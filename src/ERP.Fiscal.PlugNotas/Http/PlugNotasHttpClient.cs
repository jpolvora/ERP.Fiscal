using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Contracts;
using ERP.Fiscal.PlugNotas.Parsers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Fiscal.PlugNotas.Http;

/// <summary>
/// Cliente HTTP interno unificado para a API PlugNotas (NF-e, certificado e empresa).
/// Não é exposto fora da lib — os providers (<c>PlugNotasNfeEmissaoProvider</c>,
/// <c>PlugNotasIntegracaoProvider</c>) são a superfície pública, retornando DTOs neutros de <c>ERP.Fiscal.Abstractions</c>.
/// Registrado explicitamente como <c>HttpClient</c> tipado em <c>PlugNotasFiscalModule</c> (não usa <c>ITransientDependency</c>
/// para evitar registro concorrente que quebraria a injeção do <see cref="HttpClient"/> tipado).
/// </summary>
internal class PlugNotasHttpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<PlugNotasHttpClient> _logger;
    private readonly IOptions<PlugNotasOptions> _plugNotasOptions;

    public PlugNotasHttpClient(
        HttpClient httpClient,
        ILogger<PlugNotasHttpClient> logger,
        IOptions<PlugNotasOptions> plugNotasOptions)
    {
        _httpClient = httpClient;
        _logger = logger;
        _plugNotasOptions = plugNotasOptions;
    }

    // ----------------------------------------------------------------
    // NF-e — emissão, consulta, cancelamento, XML/PDF
    // ----------------------------------------------------------------

    public async Task<PlugNotasNfeEmissaoRawResult> EmitirNfeAsync(
        string baseUrl, string apiToken, string payloadJson, CancellationToken cancellationToken = default)
    {
        var url = Combine(baseUrl, "nfe");
        var options = _plugNotasOptions.Value;
        var maxAttempts = options.GetEffectiveMaxAttempts();
        var baseDelayMs = options.GetEffectiveBaseDelayMs();

        PlugNotasNfeEmissaoRawResult? lastResult = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            lastResult = await EmitirNfeOnceAsync(url, apiToken, payloadJson, cancellationToken);
            if (lastResult.Success || !lastResult.IsTransientFailure || attempt >= maxAttempts)
                return lastResult;

            var delayMs = baseDelayMs * (1 << (attempt - 1));
            _logger.LogWarning(
                "PlugNotas POST nfe tentativa {Attempt}/{MaxAttempts} falhou (HTTP {Status}); retry em {DelayMs}ms",
                attempt, maxAttempts, lastResult.HttpStatusCode, delayMs);
            await Task.Delay(delayMs, cancellationToken);
        }

        return lastResult!;
    }

    private async Task<PlugNotasNfeEmissaoRawResult> EmitirNfeOnceAsync(
        string url, string apiToken, string payloadJson, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("x-api-key", apiToken);
        request.Content = new StringContent(payloadJson ?? "[]", Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("PlugNotas POST nfe HTTP {Status}", (int)response.StatusCode);
            return ParseNfeEmissaoResponse((int)response.StatusCode, raw);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PlugNotasNfeEmissaoRawResult { HttpStatusCode = 0, ErrorMessage = "Tempo esgotado ao contatar a API PlugNotas (NF-e)." };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "PlugNotas POST nfe falhou (rede/SSL): {Url}", url);
            return new PlugNotasNfeEmissaoRawResult { HttpStatusCode = 0, ErrorMessage = $"Falha de rede ao contatar a PlugNotas: {ex.Message}" };
        }
    }

    public Task<PlugNotasNfeGetRawResult> ObterNfeResumoPorIdAsync(
        string baseUrl, string apiToken, string idNotaOrChaveOrProtocol, CancellationToken cancellationToken = default)
    {
        var id = NormalizePlugNotasNfePathId(idNotaOrChaveOrProtocol);
        var url = Combine(baseUrl, $"nfe/{Uri.EscapeDataString(id)}/resumo");
        return SendNfeGetAsync(url, apiToken, cancellationToken);
    }

    public Task<PlugNotasNfeGetRawResult> ObterNfeResumoPorCnpjIdIntegracaoAsync(
        string baseUrl, string apiToken, string cpfCnpjDigits, string idIntegracao, CancellationToken cancellationToken = default)
    {
        var cnpj = new string((cpfCnpjDigits ?? string.Empty).Where(char.IsDigit).ToArray());
        var id = NormalizePlugNotasNfePathId(idIntegracao);
        var url = Combine(baseUrl, $"nfe/{Uri.EscapeDataString(cnpj)}/{Uri.EscapeDataString(id)}/resumo");
        return SendNfeGetAsync(url, apiToken, cancellationToken);
    }

    public Task<PlugNotasNfeGetRawResult> ObterXmlNfePorIdAsync(
        string baseUrl, string apiToken, string idDocumento, CancellationToken cancellationToken = default)
    {
        var id = NormalizePlugNotasNfePathId(idDocumento);
        var url = Combine(baseUrl, $"nfe/{Uri.EscapeDataString(id)}/xml");
        return SendNfeGetAsync(url, apiToken, cancellationToken);
    }

    public async Task<PlugNotasBinaryRawResult> ObterPdfNfePorIdAsync(
        string baseUrl, string apiToken, string idDocumento, CancellationToken cancellationToken = default)
    {
        var id = NormalizePlugNotasNfePathId(idDocumento);
        var url = Combine(baseUrl, $"nfe/{Uri.EscapeDataString(id)}/pdf");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("x-api-key", apiToken);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var status = (int)response.StatusCode;
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (status is >= 200 and < 300)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                if (bytes.Length == 0)
                    return new PlugNotasBinaryRawResult { HttpStatusCode = status, ErrorMessage = "Resposta PDF vazia." };

                return new PlugNotasBinaryRawResult { HttpStatusCode = status, Content = bytes, ContentType = contentType };
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            return new PlugNotasBinaryRawResult { HttpStatusCode = status, ErrorMessage = FormatPlugNotasErro(raw) };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PlugNotasBinaryRawResult { HttpStatusCode = 0, ErrorMessage = "Tempo esgotado ao contatar a API PlugNotas (PDF NF-e)." };
        }
        catch (HttpRequestException ex)
        {
            return new PlugNotasBinaryRawResult { HttpStatusCode = 0, ErrorMessage = $"Falha de rede ao contatar a PlugNotas: {ex.Message}" };
        }
    }

    public async Task<PlugNotasNfeCancelamentoRawResult> CancelarNfeAsync(
        string baseUrl, string apiToken, string idDocumento, string justificativa, CancellationToken cancellationToken = default)
    {
        var id = NormalizePlugNotasNfePathId(idDocumento);
        var url = Combine(baseUrl, $"nfe/{Uri.EscapeDataString(id)}/cancelamento");
        var bodyObj = new Dictionary<string, string> { ["justificativa"] = justificativa ?? string.Empty };
        var body = JsonSerializer.Serialize(bodyObj);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("x-api-key", apiToken);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("PlugNotas POST nfe/cancelamento HTTP {Status}", (int)response.StatusCode);
            return ParseNfeCancelamentoResponse((int)response.StatusCode, raw);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PlugNotasNfeCancelamentoRawResult { HttpStatusCode = 0, ErrorMessage = "Tempo esgotado ao contatar a API PlugNotas (cancelamento NF-e)." };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "PlugNotas POST nfe/cancelamento falhou (rede/SSL): {Url}", url);
            return new PlugNotasNfeCancelamentoRawResult { HttpStatusCode = 0, ErrorMessage = $"Falha de rede ao contatar a PlugNotas: {ex.Message}" };
        }
    }

    private async Task<PlugNotasNfeGetRawResult> SendNfeGetAsync(string url, string apiToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("x-api-key", apiToken);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var status = (int)response.StatusCode;
            if (status is >= 200 and < 300)
                return new PlugNotasNfeGetRawResult { HttpStatusCode = status, RawBody = raw };

            return new PlugNotasNfeGetRawResult { HttpStatusCode = status, RawBody = raw, ErrorMessage = FormatPlugNotasErro(raw) };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PlugNotasNfeGetRawResult { HttpStatusCode = 0, ErrorMessage = "Tempo esgotado ao contatar a API PlugNotas (NF-e)." };
        }
        catch (HttpRequestException ex)
        {
            return new PlugNotasNfeGetRawResult { HttpStatusCode = 0, ErrorMessage = $"Falha de rede ao contatar a PlugNotas: {ex.Message}" };
        }
    }

    private static PlugNotasNfeCancelamentoRawResult ParseNfeCancelamentoResponse(int httpStatusCode, string raw)
    {
        var result = new PlugNotasNfeCancelamentoRawResult { HttpStatusCode = httpStatusCode, RawBody = raw };
        if (httpStatusCode is >= 200 and < 300)
            return result;

        result.ErrorMessage = FormatPlugNotasErro(raw);
        return result;
    }

    private static PlugNotasNfeEmissaoRawResult ParseNfeEmissaoResponse(int httpStatusCode, string raw)
    {
        var result = new PlugNotasNfeEmissaoRawResult { HttpStatusCode = httpStatusCode, RawBody = raw };
        if (httpStatusCode is >= 200 and < 300)
        {
            var campos = PlugNotasNfeEmissaoRespostaParser.TryParse(raw);
            result.IdDocumento = campos?.IdDocumentoProvedor;
            result.Protocolo = campos?.ProtocoloAutorizacaoSefaz;
            return result;
        }

        result.ErrorMessage = FormatPlugNotasErro(raw);
        return result;
    }

    // ----------------------------------------------------------------
    // Cadastro — certificado e empresa
    // ----------------------------------------------------------------

    public async Task<PlugNotasCertificadoRawResult> CadastrarCertificadoAsync(
        string baseUrl, string apiToken, byte[] arquivoBytes, string senha, string? email, CancellationToken cancellationToken = default)
    {
        if (arquivoBytes is null || arquivoBytes.Length == 0)
            return new PlugNotasCertificadoRawResult { HttpStatusCode = 0, ErrorMessage = "Arquivo do certificado deve ser informado." };

        var url = Combine(baseUrl, "certificado");

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("x-api-key", apiToken);
        var form = new MultipartFormDataContent();
        var cert = new ByteArrayContent(arquivoBytes);
        cert.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-pkcs12");
        form.Add(cert, "arquivo", "certificado.pfx");
        form.Add(new StringContent(senha ?? string.Empty, Encoding.UTF8), "senha");
        if (!string.IsNullOrWhiteSpace(email))
            form.Add(new StringContent(email.Trim(), Encoding.UTF8), "email");
        request.Content = form;

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("PlugNotas POST certificado HTTP {Status}", (int)response.StatusCode);

            return ParseCertificadoResponse((int)response.StatusCode, raw);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PlugNotasCertificadoRawResult { HttpStatusCode = 0, ErrorMessage = "Tempo esgotado ao contatar a API PlugNotas (certificado)." };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "PlugNotas POST certificado falhou (rede/SSL): {Url}", url);
            return new PlugNotasCertificadoRawResult { HttpStatusCode = 0, ErrorMessage = $"Falha de rede ao contatar a PlugNotas: {ex.Message}" };
        }
    }

    public async Task<PlugNotasConsultaRawResult> ObterCertificadoAsync(
        string baseUrl, string apiToken, string idCertificado, CancellationToken cancellationToken = default)
    {
        var id = (idCertificado ?? string.Empty).Trim();
        var url = Combine(baseUrl, $"certificado/{Uri.EscapeDataString(id)}");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("x-api-key", apiToken);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseConsultaResponse((int)response.StatusCode, raw);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PlugNotasConsultaRawResult { HttpStatusCode = 0, ErrorMessage = "Tempo esgotado ao contatar a API PlugNotas (certificado)." };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "PlugNotas GET certificado falhou (rede/SSL): {Url}", url);
            return new PlugNotasConsultaRawResult { HttpStatusCode = 0, ErrorMessage = $"Falha de rede ao contatar a PlugNotas: {ex.Message}" };
        }
    }

    public async Task<PlugNotasEmpresaRawResult> CadastrarEmpresaAsync(
        string baseUrl, string apiToken, PlugNotasEmpresaPayload payload, CancellationToken cancellationToken = default)
    {
        var url = Combine(baseUrl, "empresa");

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var node = JsonNode.Parse(json)!.AsObject();
        node["nfce"] = new JsonObject();
        node["mdfe"] = new JsonObject();
        node["nfcom"] = new JsonObject();

        if (node["nfe"] is JsonObject nfeNode && nfeNode["tipoContrato"] == null)
            nfeNode["tipoContrato"] = PlugNotasOptions.NormalizeTipoContrato(_plugNotasOptions.Value.TipoContrato);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("x-api-key", apiToken);
        request.Content = new StringContent(node.ToJsonString(), Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("PlugNotas POST empresa HTTP {Status}", (int)response.StatusCode);

            return ParseEmpresaResponse((int)response.StatusCode, raw);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PlugNotasEmpresaRawResult { HttpStatusCode = 0, ErrorMessage = "Tempo esgotado ao contatar a API PlugNotas (empresa)." };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "PlugNotas POST empresa falhou (rede/SSL): {Url}", url);
            return new PlugNotasEmpresaRawResult { HttpStatusCode = 0, ErrorMessage = $"Falha de rede ao contatar a PlugNotas: {ex.Message}" };
        }
    }

    public async Task<PlugNotasEmpresaRawResult> AtualizarConfigEmpresaAsync(
        string baseUrl, string apiToken, string cpfCnpj, bool producao, CancellationToken cancellationToken = default)
    {
        var cnpj = FiscalDigitsHelper.DigitsOnly(cpfCnpj);
        var url = Combine(baseUrl, $"empresa/{Uri.EscapeDataString(cnpj)}/config");
        var body = new JsonObject { ["nfe"] = new JsonObject { ["config"] = new JsonObject { ["producao"] = producao } } };

        using var request = new HttpRequestMessage(HttpMethod.Patch, url);
        request.Headers.TryAddWithoutValidation("x-api-key", apiToken);
        request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("PlugNotas PATCH empresa/config HTTP {Status}", (int)response.StatusCode);

            return ParseEmpresaResponse((int)response.StatusCode, raw);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PlugNotasEmpresaRawResult { HttpStatusCode = 0, ErrorMessage = "Tempo esgotado ao contatar a API PlugNotas (config empresa)." };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "PlugNotas PATCH empresa/config falhou (rede/SSL): {Url}", url);
            return new PlugNotasEmpresaRawResult { HttpStatusCode = 0, ErrorMessage = $"Falha de rede ao contatar a PlugNotas: {ex.Message}" };
        }
    }

    public async Task<PlugNotasConsultaRawResult> ObterEmpresaAsync(
        string baseUrl, string apiToken, string cpfCnpjDigits, CancellationToken cancellationToken = default)
    {
        var cnpj = (cpfCnpjDigits ?? string.Empty).Trim();
        var url = Combine(baseUrl, $"empresa/{Uri.EscapeDataString(cnpj)}");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("x-api-key", apiToken);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseConsultaResponse((int)response.StatusCode, raw);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PlugNotasConsultaRawResult { HttpStatusCode = 0, ErrorMessage = "Tempo esgotado ao contatar a API PlugNotas (empresa)." };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "PlugNotas GET empresa falhou (rede/SSL): {Url}", url);
            return new PlugNotasConsultaRawResult { HttpStatusCode = 0, ErrorMessage = $"Falha de rede ao contatar a PlugNotas: {ex.Message}" };
        }
    }

    private static PlugNotasConsultaRawResult ParseConsultaResponse(int httpStatusCode, string raw)
    {
        var result = new PlugNotasConsultaRawResult { HttpStatusCode = httpStatusCode, RawBody = raw };
        if (httpStatusCode is >= 200 and < 300)
            return result;

        result.ErrorMessage = FormatPlugNotasErro(raw);
        return result;
    }

    private static PlugNotasCertificadoRawResult ParseCertificadoResponse(int httpStatusCode, string raw)
    {
        var result = new PlugNotasCertificadoRawResult { HttpStatusCode = httpStatusCode, RawBody = raw };
        if (httpStatusCode is >= 200 and < 300)
        {
            TryReadMessage(raw, out var message);
            TryReadId(raw, out var id);
            result.SuccessMessage = message;
            result.Id = id;
            if (string.IsNullOrWhiteSpace(result.Id))
                result.ErrorMessage = string.IsNullOrWhiteSpace(raw)
                    ? "Resposta de sucesso sem identificador (data.id)."
                    : raw;
        }
        else
        {
            result.ErrorMessage = FormatPlugNotasErro(raw);
        }

        return result;
    }

    private static PlugNotasEmpresaRawResult ParseEmpresaResponse(int httpStatusCode, string raw)
    {
        var result = new PlugNotasEmpresaRawResult { HttpStatusCode = httpStatusCode, RawBody = raw };
        if (httpStatusCode is >= 200 and < 300)
        {
            TryReadEmpresaSucesso(raw, out var id, out var message);
            result.Id = id;
            result.SuccessMessage = message;
            if (string.IsNullOrWhiteSpace(result.Id))
                result.ErrorMessage = string.IsNullOrWhiteSpace(raw)
                    ? "Resposta de sucesso sem identificador (data.cnpj / data.id)."
                    : raw;
        }
        else
        {
            result.ErrorMessage = FormatPlugNotasErro(raw);
        }

        return result;
    }

    /// <summary>PlugNotas retorna <c>data.cnpj</c> no sucesso (doc oficial); alguns ambientes podem enviar <c>data.id</c>.</summary>
    private static void TryReadEmpresaSucesso(string raw, out string? id, out string? message)
    {
        id = null;
        message = null;
        if (string.IsNullOrWhiteSpace(raw))
            return;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("message", out var msgEl))
                message = msgEl.GetString();

            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                if (data.TryGetProperty("cnpj", out var cnpj) && cnpj.ValueKind == JsonValueKind.String)
                {
                    id = cnpj.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(id))
                        return;
                }

                if (data.TryGetProperty("id", out var idEl))
                {
                    id = ReadJsonIdValue(idEl);
                    if (!string.IsNullOrWhiteSpace(id))
                        return;
                }
            }

            TryReadId(raw, out id);
        }
        catch
        {
            /* ignore */
        }
    }

    private static bool TryReadId(string raw, out string? id)
    {
        id = null;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("id", out var idEl))
            {
                id = ReadJsonIdValue(idEl);
                return !string.IsNullOrWhiteSpace(id);
            }

            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("id", out var id2))
            {
                id = ReadJsonIdValue(id2);
                return !string.IsNullOrWhiteSpace(id);
            }
        }
        catch
        {
            /* ignore */
        }

        return false;
    }

    private static string? ReadJsonIdValue(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.String => el.GetString()?.Trim(),
            JsonValueKind.Number => el.GetRawText().Trim(),
            _ => null
        };

    private static void TryReadMessage(string raw, out string? message)
    {
        message = null;
        if (string.IsNullOrWhiteSpace(raw))
            return;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                message = msg.GetString();
        }
        catch
        {
            /* ignore */
        }
    }

    private static string FormatPlugNotasErro(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "Resposta vazia do provedor.";
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var msgEl))
                {
                    var m = msgEl.GetString();
                    if (!string.IsNullOrWhiteSpace(m))
                    {
                        if (error.TryGetProperty("data", out var data))
                            return $"{m.Trim()} {data.GetRawText()}".Trim();
                        return m.Trim();
                    }
                }
                else if (error.ValueKind == JsonValueKind.String)
                {
                    var e = error.GetString();
                    if (root.TryGetProperty("message", out var msgRoot))
                    {
                        var mr = msgRoot.GetString();
                        if (!string.IsNullOrWhiteSpace(mr))
                            return string.IsNullOrWhiteSpace(e) ? mr!.Trim() : $"{e}: {mr}".Trim();
                    }

                    return string.IsNullOrWhiteSpace(e) ? raw : e!;
                }
            }

            if (root.TryGetProperty("message", out var flatMsg))
            {
                var m = flatMsg.GetString();
                if (!string.IsNullOrWhiteSpace(m))
                    return m.Trim();
            }
        }
        catch
        {
            /* ignore */
        }

        return raw;
    }

    private static string Combine(string baseUrl, string path)
    {
        var b = (baseUrl ?? string.Empty).TrimEnd('/');
        return $"{b}/{path.TrimStart('/')}";
    }

    private static string NormalizePlugNotasNfePathId(string? idDocumento)
    {
        var t = (idDocumento ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(t))
            return string.Empty;
        if (Guid.TryParse(t, out var g))
            return g.ToString("N");
        return t;
    }
}

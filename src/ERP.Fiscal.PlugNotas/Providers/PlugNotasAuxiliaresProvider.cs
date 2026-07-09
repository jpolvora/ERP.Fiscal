using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Fiscal.PlugNotas.Providers;

/// <summary>
/// Consultas auxiliares PlugNotas (CNPJ, CEP, municípios NFS-e) — <see cref="INfeAuxiliaresProvider"/>.
/// Registrado explicitamente como <c>HttpClient</c> tipado em <c>PlugNotasFiscalModule</c> (não usa
/// <c>ITransientDependency</c> para evitar registro concorrente que quebraria a injeção do <see cref="HttpClient"/> tipado).
/// Consumo apenas via a interface (DI).
/// </summary>
internal sealed class PlugNotasAuxiliaresProvider : INfeAuxiliaresProvider
{
    private static readonly JsonSerializerOptions JsonDeserializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<PlugNotasAuxiliaresProvider> _logger;
    private readonly IOptions<PlugNotasOptions> _options;
    private readonly IMemoryCache _cache;

    public PlugNotasAuxiliaresProvider(
        HttpClient httpClient,
        ILogger<PlugNotasAuxiliaresProvider> logger,
        IOptions<PlugNotasOptions> options,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;
        _cache = cache;
    }

    public async Task<NfeConsultaCnpjResult> ConsultarCnpjAsync(string cnpjSomenteDigitos, CancellationToken cancellationToken = default)
    {
        var digits = FiscalDigitsHelper.DigitsOnly(cnpjSomenteDigitos);
        var (status, raw, err, dados) = await GetConsultaAsync(
            $"cnpj/{Uri.EscapeDataString(digits)}",
            r => JsonSerializer.Deserialize<PlugNotasConsultaCnpjDados>(r, JsonDeserializeOptions),
            cancellationToken);

        return new NfeConsultaCnpjResult
        {
            Sucesso = status is >= 200 and < 300,
            RazaoSocial = dados?.RazaoSocial ?? dados?.Nome,
            NomeFantasia = dados?.Nome,
            Logradouro = dados?.Endereco?.Logradouro,
            Numero = dados?.Endereco?.Numero,
            Complemento = dados?.Endereco?.Complemento,
            Bairro = dados?.Endereco?.Bairro,
            Municipio = dados?.Endereco?.Municipio,
            Uf = dados?.Endereco?.Uf,
            Cep = FiscalDigitsHelper.DigitsOnlyOrNull(dados?.Endereco?.Cep),
            Telefone = dados?.Telefone,
            Email = dados?.Email,
            Mensagem = err,
            HttpStatusCode = status,
            RawResponse = raw
        };
    }

    public async Task<NfeConsultaCepResult> ConsultarCepAsync(string cepSomenteDigitos, CancellationToken cancellationToken = default)
    {
        var digits = FiscalDigitsHelper.DigitsOnly(cepSomenteDigitos);
        var (status, raw, err, dados) = await GetConsultaAsync(
            $"cep/{Uri.EscapeDataString(digits)}",
            r => JsonSerializer.Deserialize<PlugNotasConsultaCepDados>(r, JsonDeserializeOptions),
            cancellationToken);

        return new NfeConsultaCepResult
        {
            Sucesso = status is >= 200 and < 300,
            Logradouro = dados?.Logradouro,
            Bairro = dados?.Bairro,
            Municipio = dados?.Municipio,
            Uf = dados?.Uf,
            Cep = FiscalDigitsHelper.DigitsOnlyOrNull(dados?.Cep),
            CodigoIbge = FiscalDigitsHelper.DigitsOnlyOrNull(
                dados?.Ibge ?? dados?.CodigoIbge ?? dados?.CodigoMunicipio),
            Mensagem = err,
            HttpStatusCode = status,
            RawResponse = raw
        };
    }

    public async Task<NfeConsultaMunicipiosResult> ConsultarMunicipiosAsync(
        string? filtroNomeOuIbge = null,
        string? uf = null,
        CancellationToken cancellationToken = default)
    {
        var (status, raw, err, lista) = await GetMunicipiosListaCachedAsync(cancellationToken);
        if (status is < 200 or >= 300 || lista is null)
        {
            return new NfeConsultaMunicipiosResult
            {
                Sucesso = false,
                Mensagem = err,
                HttpStatusCode = status,
                RawResponse = raw
            };
        }

        var filtrados = FiltrarMunicipios(lista, filtroNomeOuIbge, uf);
        return new NfeConsultaMunicipiosResult
        {
            Sucesso = true,
            Itens = filtrados,
            HttpStatusCode = status,
            RawResponse = raw
        };
    }

    public async Task<NfeConsultaMunicipioResult> ConsultarMunicipioPorIbgeAsync(
        string codigoIbge,
        CancellationToken cancellationToken = default)
    {
        var digits = FiscalDigitsHelper.DigitsOnly(codigoIbge);
        var (status, raw, err, dados) = await GetConsultaAsync(
            $"nfse/cidades/{Uri.EscapeDataString(digits)}",
            r => JsonSerializer.Deserialize<PlugNotasNfseCidadeDados>(r, JsonDeserializeOptions),
            cancellationToken);

        if (status is < 200 or >= 300 || dados is null)
        {
            return new NfeConsultaMunicipioResult
            {
                Sucesso = false,
                Mensagem = err,
                HttpStatusCode = status,
                RawResponse = raw
            };
        }

        var item = MapCidade(dados);
        if (item is null)
        {
            return new NfeConsultaMunicipioResult
            {
                Sucesso = false,
                Mensagem = "Resposta de município sem código IBGE/nome reconhecíveis.",
                HttpStatusCode = status,
                RawResponse = raw
            };
        }

        return new NfeConsultaMunicipioResult
        {
            Sucesso = true,
            Municipio = item,
            HttpStatusCode = status,
            RawResponse = raw
        };
    }

    private async Task<(int Status, string? Raw, string? Error, IReadOnlyList<NfeMunicipioItem>? Lista)> GetMunicipiosListaCachedAsync(
        CancellationToken cancellationToken)
    {
        var (baseUrl, _) = ResolveBaseUrlAndApiKey();
        var cacheKey = $"plugnotas:nfse:cidades:{baseUrl}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<NfeMunicipioItem>? cached) && cached is not null)
        {
            return (200, null, null, cached);
        }

        var (status, raw, err, dados) = await GetConsultaAsync(
            "nfse/cidades",
            r => JsonSerializer.Deserialize<List<PlugNotasNfseCidadeDados>>(r, JsonDeserializeOptions),
            cancellationToken);

        if (status is < 200 or >= 300 || dados is null)
        {
            return (status, raw, err, null);
        }

        var lista = DedupMunicipios(dados);
        var ttl = TimeSpan.FromMinutes(_options.Value.GetEffectiveMunicipiosCacheMinutes());
        _cache.Set(cacheKey, lista, ttl);
        return (status, raw, err, lista);
    }

    private static IReadOnlyList<NfeMunicipioItem> DedupMunicipios(IEnumerable<PlugNotasNfseCidadeDados> dados)
    {
        var map = new Dictionary<string, NfeMunicipioItem>(StringComparer.Ordinal);
        foreach (var d in dados)
        {
            var item = MapCidade(d);
            if (item is null)
                continue;
            map[item.CodigoIbge] = item;
        }

        return map.Values
            .OrderBy(x => x.Uf, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Nome, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static NfeMunicipioItem? MapCidade(PlugNotasNfseCidadeDados? dados)
    {
        if (dados is null)
            return null;

        var ibge = FiscalDigitsHelper.DigitsOnlyOrNull(dados.Id?.ToString(CultureInfo.InvariantCulture));
        if (string.IsNullOrWhiteSpace(ibge) || string.IsNullOrWhiteSpace(dados.Nome))
            return null;

        return new NfeMunicipioItem
        {
            CodigoIbge = ibge,
            Nome = dados.Nome.Trim(),
            Uf = (dados.Uf ?? string.Empty).Trim().ToUpperInvariant()
        };
    }

    private static IReadOnlyList<NfeMunicipioItem> FiltrarMunicipios(
        IReadOnlyList<NfeMunicipioItem> lista,
        string? filtroNomeOuIbge,
        string? uf)
    {
        IEnumerable<NfeMunicipioItem> query = lista;

        var ufNorm = uf?.Trim();
        if (!string.IsNullOrEmpty(ufNorm))
        {
            var ufUpper = ufNorm.ToUpperInvariant();
            query = query.Where(x => string.Equals(x.Uf, ufUpper, StringComparison.OrdinalIgnoreCase));
        }

        var q = filtroNomeOuIbge?.Trim();
        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(x =>
                x.Nome.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                x.CodigoIbge.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }

    private async Task<(int Status, string? Raw, string? Error, TDados? Dados)> GetConsultaAsync<TDados>(
        string relativePath, Func<string, TDados?> tryDeserialize, CancellationToken cancellationToken)
        where TDados : class
    {
        var (baseUrl, apiKey) = ResolveBaseUrlAndApiKey();
        var url = Combine(baseUrl, relativePath);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("x-api-key", apiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var status = (int)response.StatusCode;
            _logger.LogDebug("PlugNotas auxiliares GET {Path} HTTP {Status}", relativePath, status);

            TDados? dados = null;
            string? err = null;

            if (status is >= 200 and < 300 && !string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    dados = tryDeserialize(raw);
                }
                catch (JsonException)
                {
                    // mantém dados nulo; RawBody disponível
                }
            }
            else
            {
                err = FormatPlugNotasAuxiliarErro(raw);
            }

            return (status, raw, err, dados);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (0, null, "Tempo esgotado ao contatar a API PlugNotas (consulta auxiliar).", default);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "PlugNotas auxiliares GET falhou (rede/SSL): {Url}", url);
            return (0, null, $"Falha de rede ao contatar a PlugNotas: {ex.Message}", default);
        }
    }

    private (string BaseUrl, string ApiKey) ResolveBaseUrlAndApiKey()
    {
        if (_options.Value.OnlySandbox)
        {
            var sandboxKey = _options.Value.SandboxApiKey?.Trim();
            if (string.IsNullOrEmpty(sandboxKey))
                sandboxKey = PlugNotasAmbienteConstants.PublicSandboxApiKey;

            return (PlugNotasAmbienteConstants.SandboxBaseUrl, sandboxKey);
        }

        var productionKey = _options.Value.ProductionApiKey?.Trim();
        if (!string.IsNullOrEmpty(productionKey))
            return (PlugNotasAmbienteConstants.ApiOficialBaseUrl, productionKey);

        var fallbackSandboxKey = _options.Value.SandboxApiKey?.Trim();
        if (string.IsNullOrEmpty(fallbackSandboxKey))
            fallbackSandboxKey = PlugNotasAmbienteConstants.PublicSandboxApiKey;

        return (PlugNotasAmbienteConstants.SandboxBaseUrl, fallbackSandboxKey);
    }

    private static string Combine(string baseUrl, string path)
    {
        var b = (baseUrl ?? string.Empty).TrimEnd('/');
        return $"{b}/{path.TrimStart('/')}";
    }

    private static string FormatPlugNotasAuxiliarErro(string raw)
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
                var fm = flatMsg.GetString();
                if (!string.IsNullOrWhiteSpace(fm))
                    return fm.Trim();
            }

            return raw;
        }
        catch
        {
            return raw;
        }
    }
}

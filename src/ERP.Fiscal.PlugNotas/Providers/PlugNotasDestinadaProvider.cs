using System.Threading;
using System.Threading.Tasks;
using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Http;
using ERP.Fiscal.PlugNotas.Parsers;
using Microsoft.Extensions.Options;

namespace ERP.Fiscal.PlugNotas.Providers;

internal sealed class PlugNotasDestinadaProvider : INfeDestinadaProvider
{
    private readonly PlugNotasHttpClient _http;
    private readonly IOptions<PlugNotasOptions> _options;

    public PlugNotasDestinadaProvider(PlugNotasHttpClient http, IOptions<PlugNotasOptions> options)
    {
        _http = http;
        _options = options;
    }

    public async Task<NfeDestinadaConsultaResult> ListarDestinadasAsync(
        NfeDestinadaConsultaRequest request,
        NfeAmbiente ambiente,
        CancellationToken cancellationToken = default)
    {
        var (baseUrl, apiToken) = ResolveEndpoint(ambiente);
        var raw = await _http.ListarNfeDestinadaAsync(
            baseUrl,
            apiToken,
            request.CpfCnpjDigits ?? string.Empty,
            request.Limite,
            request.Manifestada,
            cancellationToken);

        return PlugNotasNfeDestinadaRespostaParser.Parse(raw.HttpStatusCode, raw.RawBody ?? raw.ErrorMessage);
    }

    private (string BaseUrl, string ApiToken) ResolveEndpoint(NfeAmbiente ambiente)
    {
        var baseUrl = PlugNotasBaseUrlResolver.Resolve(ambiente);
        var apiToken = PlugNotasApiKeyResolver.Resolve(_options, ambiente);
        return (baseUrl, apiToken);
    }
}

using System.Threading;
using System.Threading.Tasks;
using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Contracts;
using ERP.Fiscal.PlugNotas.Http;
using ERP.Fiscal.PlugNotas.Parsers;
using Microsoft.Extensions.Options;

namespace ERP.Fiscal.PlugNotas.Providers;

/// <summary>
/// Implementação PlugNotas de <see cref="INfeIntegracaoProvider"/>. Consumo apenas via a interface (DI).
/// </summary>
internal class PlugNotasIntegracaoProvider : INfeIntegracaoProvider
{
    private readonly PlugNotasHttpClient _http;
    private readonly IOptions<PlugNotasOptions> _options;

    public PlugNotasIntegracaoProvider(PlugNotasHttpClient http, IOptions<PlugNotasOptions> options)
    {
        _http = http;
        _options = options;
    }

    public async Task<NfeProviderResult> CadastrarCertificadoAsync(NfeCertificadoUpload data, NfeAmbiente ambiente, CancellationToken cancellationToken = default)
    {
        var (baseUrl, apiToken) = ResolveEndpoint(ambiente);
        var raw = await _http.CadastrarCertificadoAsync(baseUrl, apiToken, data.ArquivoBytes, data.Senha, data.EmailNotificacao, cancellationToken);
        return new NfeProviderResult
        {
            Sucesso = raw.Success,
            IdProvedor = raw.Id,
            Mensagem = raw.Success ? raw.SuccessMessage : raw.ErrorMessage,
            HttpStatusCode = raw.HttpStatusCode,
            RawResponse = raw.RawBody
        };
    }

    public async Task<NfeProviderResult> ConsultarCertificadoAsync(string idCertificadoProvedor, NfeAmbiente ambiente, CancellationToken cancellationToken = default)
    {
        var (baseUrl, apiToken) = ResolveEndpoint(ambiente);
        var raw = await _http.ObterCertificadoAsync(baseUrl, apiToken, idCertificadoProvedor, cancellationToken);
        var campos = raw.Success ? PlugNotasIntegracaoConsultaRespostaParser.TryParse(raw.RawBody) : null;
        return new NfeProviderResult
        {
            Sucesso = raw.Success,
            IdProvedor = raw.Success ? campos?.IdProvedor ?? idCertificadoProvedor : null,
            CpfCnpj = campos?.CpfCnpj,
            Nome = campos?.Nome,
            Email = campos?.Email,
            ValidadeInicial = campos?.ValidadeInicial,
            ValidadeFinal = campos?.ValidadeFinal,
            Producao = campos?.Producao,
            Mensagem = raw.ErrorMessage,
            HttpStatusCode = raw.HttpStatusCode,
            RawResponse = raw.RawBody
        };
    }

    public async Task<NfeProviderResult> CadastrarEmissorAsync(NfeEmissorData data, NfeAmbiente ambiente, CancellationToken cancellationToken = default)
    {
        var (baseUrl, apiToken) = ResolveEndpoint(ambiente);
        var payload = MapEmpresaPayload(data);
        var raw = await _http.CadastrarEmpresaAsync(baseUrl, apiToken, payload, cancellationToken);
        var campos = raw.Success ? PlugNotasIntegracaoConsultaRespostaParser.TryParse(raw.RawBody) : null;
        return new NfeProviderResult
        {
            Sucesso = raw.Success,
            IdProvedor = raw.Id ?? campos?.IdProvedor ?? campos?.CpfCnpj,
            CpfCnpj = campos?.CpfCnpj ?? data.CpfCnpj,
            Nome = campos?.Nome ?? data.RazaoSocial,
            Email = campos?.Email ?? data.Email,
            Producao = campos?.Producao ?? data.Producao,
            Mensagem = raw.Success ? raw.SuccessMessage : raw.ErrorMessage,
            HttpStatusCode = raw.HttpStatusCode,
            RawResponse = raw.RawBody
        };
    }

    public async Task<NfeProviderResult> ConsultarEmissorAsync(string cpfCnpjDigits, NfeAmbiente ambiente, CancellationToken cancellationToken = default)
    {
        var (baseUrl, apiToken) = ResolveEndpoint(ambiente);
        var raw = await _http.ObterEmpresaAsync(baseUrl, apiToken, cpfCnpjDigits, cancellationToken);
        var campos = raw.Success ? PlugNotasIntegracaoConsultaRespostaParser.TryParse(raw.RawBody) : null;
        return new NfeProviderResult
        {
            Sucesso = raw.Success,
            IdProvedor = raw.Success ? campos?.IdProvedor ?? campos?.CpfCnpj ?? cpfCnpjDigits : null,
            CpfCnpj = campos?.CpfCnpj ?? cpfCnpjDigits,
            Nome = campos?.Nome,
            Email = campos?.Email,
            Producao = campos?.Producao,
            Mensagem = raw.ErrorMessage,
            HttpStatusCode = raw.HttpStatusCode,
            RawResponse = raw.RawBody
        };
    }

    public async Task<NfeProviderResult> SincronizarAmbienteEmissorAsync(string cpfCnpjDigits, bool producao, NfeAmbiente ambiente, CancellationToken cancellationToken = default)
    {
        var (baseUrl, apiToken) = ResolveEndpoint(ambiente);
        var raw = await _http.AtualizarConfigEmpresaAsync(baseUrl, apiToken, cpfCnpjDigits, producao, cancellationToken);
        var campos = raw.Success ? PlugNotasIntegracaoConsultaRespostaParser.TryParse(raw.RawBody) : null;
        return new NfeProviderResult
        {
            Sucesso = raw.Success,
            IdProvedor = raw.Id ?? campos?.IdProvedor ?? campos?.CpfCnpj,
            CpfCnpj = campos?.CpfCnpj ?? cpfCnpjDigits,
            Nome = campos?.Nome,
            Email = campos?.Email,
            Producao = campos?.Producao ?? producao,
            Mensagem = raw.Success ? raw.SuccessMessage : raw.ErrorMessage,
            HttpStatusCode = raw.HttpStatusCode,
            RawResponse = raw.RawBody
        };
    }

    private PlugNotasEmpresaPayload MapEmpresaPayload(NfeEmissorData data) => new()
    {
        CpfCnpj = data.CpfCnpj,
        RazaoSocial = data.RazaoSocial,
        NomeFantasia = data.NomeFantasia,
        InscricaoEstadual = data.InscricaoEstadual,
        InscricaoMunicipal = data.InscricaoMunicipal,
        Certificado = data.IdCertificadoProvedor,
        SimplesNacional = data.SimplesNacional,
        RegimeTributario = data.RegimeTributario,
        RegimeTributarioEspecial = data.RegimeTributarioEspecial,
        IncentivoFiscal = data.IncentivoFiscal,
        IncentivadorCultural = data.IncentivadorCultural,
        Email = data.Email,
        Endereco = data.Endereco is null
            ? null
            : new PlugNotasEnderecoPayload
            {
                Logradouro = data.Endereco.Logradouro,
                TipoLogradouro = data.Endereco.TipoLogradouro,
                Numero = data.Endereco.Numero,
                Bairro = data.Endereco.Bairro,
                Cep = data.Endereco.Cep,
                CodigoCidade = data.Endereco.CodigoCidade,
                Estado = data.Endereco.Estado,
                Complemento = data.Endereco.Complemento
            },
        Telefone = data.Telefone is null
            ? null
            : new PlugNotasTelefonePayload { Ddd = data.Telefone.Ddd, Numero = data.Telefone.Numero },
        Nfe = new PlugNotasNfeConfigPayload
        {
            TipoContrato = PlugNotasOptions.NormalizeTipoContrato(_options.Value.TipoContrato),
            Config = new PlugNotasNfeInnerConfigPayload
            {
                Producao = data.Producao,
                TipoContrato = PlugNotasOptions.NormalizeTipoContrato(_options.Value.TipoContrato),
                NumeracaoAutomatica = false,
                Numeracao = BuildNumeracao(data)
            }
        }
    };

    private static List<PlugNotasNumeracaoPayload>? BuildNumeracao(NfeEmissorData data)
    {
        if (string.IsNullOrWhiteSpace(data.SerieNfe))
        {
            return null;
        }

        var numero = string.IsNullOrWhiteSpace(data.NumeroInicialNfe) ? "1" : data.NumeroInicialNfe.Trim();
        return
        [
            new PlugNotasNumeracaoPayload { Numero = numero, Serie = data.SerieNfe.Trim() }
        ];
    }

    private (string BaseUrl, string ApiToken) ResolveEndpoint(NfeAmbiente ambiente)
    {
        var baseUrl = PlugNotasBaseUrlResolver.Resolve(ambiente);
        var apiToken = PlugNotasApiKeyResolver.Resolve(_options, ambiente);
        return (baseUrl, apiToken);
    }
}

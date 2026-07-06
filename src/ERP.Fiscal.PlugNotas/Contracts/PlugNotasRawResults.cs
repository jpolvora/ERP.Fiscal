using ERP.Fiscal.PlugNotas.Parsers;

namespace ERP.Fiscal.PlugNotas.Contracts;

/// <summary>Resposta crua do POST <c>/nfe</c> (emissão) na API PlugNotas.</summary>
internal class PlugNotasNfeEmissaoRawResult
{
    public string? IdDocumento { get; set; }
    public string? Protocolo { get; set; }
    public int HttpStatusCode { get; set; }
    public string? RawBody { get; set; }
    public string? ErrorMessage { get; set; }

    public bool Success => HttpStatusCode is >= 200 and < 300 && string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsTransientFailure => !Success && PlugNotasHttpErrorClassifier.IsTransient(HttpStatusCode);
}

/// <summary>Resposta crua de GET <c>/nfe/{id}</c>, <c>/resumo</c> ou <c>/xml</c> na API PlugNotas.</summary>
internal class PlugNotasNfeGetRawResult
{
    public int HttpStatusCode { get; set; }
    public string? RawBody { get; set; }
    public string? ErrorMessage { get; set; }

    public bool Success => HttpStatusCode is >= 200 and < 300 && string.IsNullOrWhiteSpace(ErrorMessage);
}

/// <summary>Resposta crua do POST <c>/nfe/{id}/cancelamento</c> na API PlugNotas.</summary>
internal class PlugNotasNfeCancelamentoRawResult
{
    public int HttpStatusCode { get; set; }
    public string? RawBody { get; set; }
    public string? ErrorMessage { get; set; }

    public bool Success => HttpStatusCode is >= 200 and < 300 && string.IsNullOrWhiteSpace(ErrorMessage);
}

/// <summary>Resposta binária crua (ex.: GET <c>/nfe/{id}/pdf</c> — DANFE).</summary>
internal class PlugNotasBinaryRawResult
{
    public int HttpStatusCode { get; set; }
    public byte[]? Content { get; set; }
    public string? ContentType { get; set; }
    public string? ErrorMessage { get; set; }

    public bool Success => HttpStatusCode is >= 200 and < 300 && Content is { Length: > 0 } && string.IsNullOrWhiteSpace(ErrorMessage);
}

/// <summary>Resposta crua do cadastro/consulta de certificado na API PlugNotas.</summary>
internal class PlugNotasCertificadoRawResult
{
    public string? Id { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawBody { get; set; }
    public int HttpStatusCode { get; set; }

    public bool Success => HttpStatusCode is >= 200 and < 300 && !string.IsNullOrWhiteSpace(Id);
}

/// <summary>Resposta crua de consulta (GET) de certificado — não exige <c>Id</c> (usada só para checar existência).</summary>
internal class PlugNotasConsultaRawResult
{
    public int HttpStatusCode { get; set; }
    public string? RawBody { get; set; }
    public string? ErrorMessage { get; set; }

    public bool Success => HttpStatusCode is >= 200 and < 300;
}

/// <summary>Resposta crua do cadastro/atualização de empresa na API PlugNotas.</summary>
internal class PlugNotasEmpresaRawResult
{
    public string? Id { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawBody { get; set; }
    public int HttpStatusCode { get; set; }

    public bool Success => HttpStatusCode is >= 200 and < 300 && !string.IsNullOrWhiteSpace(Id);
}

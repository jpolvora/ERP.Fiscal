using System;

namespace ERP.Fiscal.Abstractions;

/// <summary>Resultado neutro do POST de emissão (ex.: <c>POST /nfe</c>) — independe do provedor concreto.</summary>
public class NfeEmissaoResult
{
    public bool Sucesso { get; init; }

    /// <summary>Identificador do documento no provedor (chave técnica, não é a chave de acesso SEFAZ).</summary>
    public string? IdDocumentoProvedor { get; init; }

    /// <summary>Número de protocolo de autorização SEFAZ, quando disponível na resposta síncrona.</summary>
    public string? Protocolo { get; init; }

    public string? Mensagem { get; init; }

    /// <summary>Indica falha de comunicação transitória (rede, timeout, rate limit, 5xx de gateway) — permite retry.</summary>
    public bool IsTransientFailure { get; init; }

    public int HttpStatusCode { get; init; }

    public string? RawResponse { get; init; }
}

/// <summary>Resultado neutro de consulta pós-emissão (status, protocolo, XML/dados do documento).</summary>
public class NfeConsultaResult
{
    public bool Sucesso { get; init; }
    public string? IdDocumentoProvedor { get; init; }
    public string? ChaveAcesso { get; init; }
    public string? Protocolo { get; init; }

    /// <summary>Situação normalizada: <see cref="NfeSituacao"/>.</summary>
    public string? Situacao { get; init; }

    public int? CodigoStatusSefaz { get; init; }
    public string? NumeroNota { get; init; }
    public string? Serie { get; init; }
    public string? Mensagem { get; init; }
    public string? XmlContent { get; init; }
    public int HttpStatusCode { get; init; }
    public string? RawResponse { get; init; }
}

/// <summary>Situações normalizadas de consulta de NF-e, independentes de provedor.</summary>
public static class NfeSituacao
{
    public const string Autorizada = "Autorizada";
    public const string Rejeitada = "Rejeitada";
    public const string Cancelada = "Cancelada";
    public const string Processando = "Processando";
    public const string Desconhecido = "Desconhecido";
}

/// <summary>Resultado neutro de cancelamento de NF-e.</summary>
public class NfeCancelamentoResult
{
    public bool Sucesso { get; init; }
    public string? Mensagem { get; init; }
    public int HttpStatusCode { get; init; }
    public string? RawResponse { get; init; }
}

/// <summary>Resultado neutro de obtenção de XML autorizado.</summary>
public class NfeXmlResult
{
    public bool Sucesso { get; init; }
    public string? XmlContent { get; init; }
    public string? Mensagem { get; init; }
    public int HttpStatusCode { get; init; }
}

/// <summary>Resultado neutro de obtenção de PDF (DANFE).</summary>
public class NfePdfResult
{
    public bool Sucesso { get; init; }
    public byte[]? PdfBytes { get; init; }
    public string? ContentType { get; init; }
    public string? Mensagem { get; init; }
    public int HttpStatusCode { get; init; }
}

/// <summary>Resultado neutro de operações de cadastro/integração no provedor (certificado, emissor/empresa).</summary>
public class NfeProviderResult
{
    public bool Sucesso { get; init; }

    /// <summary>Identificador atribuído pelo provedor (ex.: id do certificado ou da empresa cadastrada).</summary>
    public string? IdProvedor { get; init; }

    public string? CpfCnpj { get; init; }

    public string? Nome { get; init; }

    public string? Email { get; init; }

    public DateTime? ValidadeInicial { get; init; }

    public DateTime? ValidadeFinal { get; init; }

    public bool? Producao { get; init; }

    public string? Mensagem { get; init; }
    public int HttpStatusCode { get; init; }
    public string? RawResponse { get; init; }
}

/// <summary>Resultado neutro consolidado do workflow técnico de emissão/consulta NF-e.</summary>
public class NfeProcessamentoResult
{
    public bool Sucesso { get; init; }
    public string? IdDocumentoProvedor { get; init; }
    public string? IdIntegracao { get; init; }
    public string? ChaveAcesso { get; init; }
    public string? Protocolo { get; init; }
    public string? Situacao { get; init; }
    public string? CodigoRetorno { get; init; }
    public string? Mensagem { get; init; }
    public string? PayloadEnviado { get; init; }
    public string? RawResponse { get; init; }
    public string? XmlContent { get; init; }
    public byte[]? PdfBytes { get; init; }
    public bool IsTransientFailure { get; init; }
    public int HttpStatusCode { get; init; }
}

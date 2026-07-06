using System;
using System.Collections.Generic;

namespace ERP.Fiscal.Abstractions;

/// <summary>Parâmetros neutros para listagem de NF-e destinadas.</summary>
public class NfeDestinadaConsultaRequest
{
    public string? CpfCnpjDigits { get; init; }

    public int Limite { get; init; } = 20;

    public bool? Manifestada { get; init; }
}

/// <summary>Item resumido de NF-e destinada.</summary>
public class NfeDestinadaItem
{
    public string? ChaveAcesso { get; init; }

    public string? CnpjEmitente { get; init; }

    public string? CnpjDestinatario { get; init; }

    public DateTime? DataEmissao { get; init; }

    public string? NomeResumo { get; init; }

    public string? ResumoItensNcm { get; init; }
}

/// <summary>Resultado neutro da consulta de NF-e destinadas.</summary>
public class NfeDestinadaConsultaResult
{
    public bool Sucesso { get; init; }

    public IReadOnlyList<NfeDestinadaItem> Itens { get; init; } = Array.Empty<NfeDestinadaItem>();

    public string? Mensagem { get; init; }

    public int HttpStatusCode { get; init; }

    public string? RawResponse { get; init; }
}

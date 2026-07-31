using System.Collections.Generic;

namespace ERP.Fiscal.PlugNotas.Contracts;

/// <summary>
/// Contratos JSON para NFS-e na API PlugNotas (POST <c>/nfse</c>, array na raiz).
/// Espelham o schema <c>dadosNfse</c> — sem vocabulário de domínio dos ERPs.
/// </summary>
public class PlugNotasNfseDocumentPayload
{
    public string? IdIntegracao { get; set; }
    public string? Competencia { get; set; }
    public PlugNotasNfsePrestadorPayload? Prestador { get; set; }
    public PlugNotasNfseTomadorPayload? Tomador { get; set; }
    public List<PlugNotasNfseServicoPayload>? Servico { get; set; }
    public PlugNotasNfseRpsPayload? Rps { get; set; }
    public string? InformacoesComplementares { get; set; }
}

public class PlugNotasNfsePrestadorPayload
{
    public string? CpfCnpj { get; set; }
    public string? InscricaoMunicipal { get; set; }
}

public class PlugNotasNfseTomadorPayload
{
    public string? CpfCnpj { get; set; }
    public string? RazaoSocial { get; set; }
    public string? Email { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public PlugNotasNfseEnderecoPayload? Endereco { get; set; }
}

public class PlugNotasNfseEnderecoPayload
{
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cep { get; set; }
    public string? CodigoCidade { get; set; }
    public string? DescricaoCidade { get; set; }
    public string? Estado { get; set; }
}

public class PlugNotasNfseServicoPayload
{
    public string? Codigo { get; set; }
    public string? CodigoTributacao { get; set; }
    public string? Cnae { get; set; }
    public string? CodigoCidadeIncidencia { get; set; }
    public string? DescricaoCidadeIncidencia { get; set; }
    public string? Discriminacao { get; set; }
    public PlugNotasNfseIssPayload? Iss { get; set; }
    public PlugNotasNfseValorPayload? Valor { get; set; }
    public PlugNotasNfseRetencaoPayload? Retencao { get; set; }
    public string? ResponsavelRetencao { get; set; }
}

public class PlugNotasNfseIssPayload
{
    public int? Exigibilidade { get; set; }
    public bool Retido { get; set; }
    public int? TipoRetencao { get; set; }
    public decimal? Aliquota { get; set; }
}

public class PlugNotasNfseValorPayload
{
    public decimal? Servico { get; set; }
    public decimal? BaseCalculo { get; set; }
}

public class PlugNotasNfseRetencaoPayload
{
    public PlugNotasNfseRetencaoItemPayload? Pis { get; set; }
    public PlugNotasNfseRetencaoItemPayload? Cofins { get; set; }
    public PlugNotasNfseRetencaoCsllPayload? Csll { get; set; }
    public PlugNotasNfseRetencaoItemPayload? Irrf { get; set; }
    public PlugNotasNfseRetencaoItemPayload? Inss { get; set; }
}

public class PlugNotasNfseRetencaoItemPayload
{
    public bool Retido { get; set; }
    public decimal? Aliquota { get; set; }
}

public class PlugNotasNfseRetencaoCsllPayload
{
    public decimal? Aliquota { get; set; }
    public decimal? Valor { get; set; }
}

public class PlugNotasNfseRpsPayload
{
    public string? Serie { get; set; }
    public string? DataEmissao { get; set; }
}

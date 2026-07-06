namespace ERP.Fiscal.Abstractions;

/// <summary>Dados de upload de certificado digital A1 para cadastro no provedor (PFX/P12).</summary>
public class NfeCertificadoUpload
{
    public byte[] ArquivoBytes { get; init; } = System.Array.Empty<byte>();
    public string Senha { get; init; } = string.Empty;
    public string? EmailNotificacao { get; init; }
}

/// <summary>
/// Dados neutros de emissor/empresa para cadastro no provedor. Montagem a partir das entidades
/// de domínio (<c>Empresa</c>, <c>Emissor</c>) é responsabilidade de cada ERP; a lib apenas serializa e transmite.
/// </summary>
public class NfeEmissorData
{
    public string CpfCnpj { get; init; } = string.Empty;
    public string RazaoSocial { get; init; } = string.Empty;
    public string? NomeFantasia { get; init; }
    public string? InscricaoEstadual { get; init; }
    public string? InscricaoMunicipal { get; init; }

    /// <summary>Id do certificado já cadastrado no provedor (retornado por <c>CadastrarCertificadoAsync</c>).</summary>
    public string? IdCertificadoProvedor { get; init; }

    public bool SimplesNacional { get; init; }
    public int RegimeTributario { get; init; }
    public int RegimeTributarioEspecial { get; init; }
    public bool IncentivoFiscal { get; init; }
    public bool IncentivadorCultural { get; init; }
    public string? Email { get; init; }

    public NfeEnderecoData? Endereco { get; init; }
    public NfeTelefoneData? Telefone { get; init; }

    /// <summary><c>true</c> para ambiente de produção efetivo (<c>nfe.config.producao</c>).</summary>
    public bool Producao { get; init; }

    /// <summary>Série NF-e para numeração inicial no cadastro PlugNotas (<c>nfe.config.numeracao</c>).</summary>
    public string? SerieNfe { get; init; }

    /// <summary>Número inicial NF-e (string conforme API PlugNotas).</summary>
    public string? NumeroInicialNfe { get; init; }
}

public class NfeEnderecoData
{
    public string? Logradouro { get; init; }
    public string? TipoLogradouro { get; init; }
    public string? Numero { get; init; }
    public string? Bairro { get; init; }
    public string? Cep { get; init; }
    public string? CodigoCidade { get; init; }
    public string? Estado { get; init; }
    public string? Complemento { get; init; }
}

public class NfeTelefoneData
{
    public string? Ddd { get; init; }
    public string? Numero { get; init; }
}

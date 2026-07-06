using System.Collections.Generic;

namespace ERP.Fiscal.PlugNotas.Contracts;

/// <summary>Valores de <c>tipoContrato</c> (modelo de faturamento) no POST /empresa PlugNotas.</summary>
internal static class PlugNotasNfeTipoContrato
{
    public const int Bilhetagem = 0;
    public const int Ilimitado = 1;
}

/// <summary>Payload JSON para POST /empresa (PlugNotas). Objetos vazios <c>nfce</c>, <c>mdfe</c> e <c>nfcom</c>
/// são acrescentados pelo client HTTP conforme exigência da API.</summary>
internal class PlugNotasEmpresaPayload
{
    public string? CpfCnpj { get; set; }
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string? Certificado { get; set; }
    public bool SimplesNacional { get; set; }
    public int RegimeTributario { get; set; }
    public int RegimeTributarioEspecial { get; set; }
    public bool IncentivoFiscal { get; set; }
    public bool IncentivadorCultural { get; set; }
    public string? Email { get; set; }
    public PlugNotasEnderecoPayload? Endereco { get; set; }
    public PlugNotasTelefonePayload? Telefone { get; set; }
    public PlugNotasNfeConfigPayload? Nfe { get; set; }
}

internal class PlugNotasEnderecoPayload
{
    public string? Logradouro { get; set; }
    public string? TipoLogradouro { get; set; }
    public string? Numero { get; set; }
    public string? Bairro { get; set; }
    public string? Cep { get; set; }
    public string? CodigoCidade { get; set; }
    public string? Estado { get; set; }
    public string? Complemento { get; set; }
}

internal class PlugNotasTelefonePayload
{
    public string? Ddd { get; set; }
    public string? Numero { get; set; }
}

internal class PlugNotasNfeConfigPayload
{
    public bool Ativo { get; set; } = true;

    /// <summary>Obrigatório na validação PlugNotas (<c>nfe.tipoContrato</c>).</summary>
    public int TipoContrato { get; set; } = PlugNotasNfeTipoContrato.Ilimitado;

    public PlugNotasNfeInnerConfigPayload? Config { get; set; }
}

internal class PlugNotasNfeInnerConfigPayload
{
    public bool Producao { get; set; }
    public int TipoContrato { get; set; } = PlugNotasNfeTipoContrato.Ilimitado;
    public List<PlugNotasNumeracaoPayload>? Numeracao { get; set; }
    public bool NumeracaoAutomatica { get; set; } = true;
}

internal class PlugNotasNumeracaoPayload
{
    public string? Numero { get; set; }
    public string? Serie { get; set; }
}
